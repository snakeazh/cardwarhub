using System.Collections.Generic;
using Config;
using UnityEngine;
using Unity.Profiling;

namespace FusionContinent.Target
{
    /// <summary>
    /// 非 Boss、非战士专属规则时的索敌回退方式。
    /// </summary>
    public enum CombatTargetFallback
    {
        /// <summary>攻击范围内最近（普攻默认）</summary>
        NearestInRange,

        /// <summary>攻击圆内方向密度选敌（主动技能默认）</summary>
        FanCrowd,
    }

    /// <summary>
    /// 英灵侧敌人索敌与目标坐标解析。
    /// 敌人列表唯一数据源：<see cref="MonsterManager.enemies"/>。
    /// </summary>
    public static class EnemyTargetFinder
    {
        /// <summary>扇形综合选敌：角度优先级权重</summary>
        public const float FanAnglePriorityWeight = 0.45f;

        /// <summary>扇形综合选敌：距离优先级权重</summary>
        public const float FanDistancePriorityWeight = 0.45f;

        /// <summary>扇形索敌默认张角（度）</summary>
        public const float DefaultFanAngleDegrees = 30f;

        /// <summary>主动技能密度窗口张角（度）：以候选为中心的滑动窗口</summary>
        public const float SkillFanAngleDegrees = 15f;

        /// <summary>
        /// MostCrowded 邻域统计半径（世界单位）。
        /// 以候选怪为圆心统计该半径内存活敌人数量；configAttackRange&gt;0 时改用其世界半径。
        /// </summary>
        public const float DefaultCrowdRadius = 1.5f;

        private struct FanCandidate
        {
            public MonsterCard Enemy;
            /// <summary>相对扇形中轴的有符号夹角（度，左负右正）</summary>
            public float SignedAngleDegrees;
            public float Distance;
        }

        /// <summary>
        /// 将配置表攻击距离转为世界空间检测半径（与 SoldierAtkRange 原逻辑一致：除以 250）。
        /// </summary>
        public static float ToWorldRange(float configAttackRange) => configAttackRange / 250f;

        /// <summary>敌人是否可作为索敌候选（存活、激活、非 die 状态）</summary>
        public static bool IsValidEnemy(MonsterCard enemy)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (enemy.hp <= 0 || enemy.monsterData.monsterState == MonsterState.die)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 快照中的怪物引用是否仍是「同一只存活敌人」。
        /// 对象池复用后 uniqueId 会变，此时视为原目标已失效，避免跟到出生点。
        /// </summary>
        public static bool IsLiveSnapshotEnemy(BattleTargetInfo info)
        {
            if (!info.isEnemy || !IsValidEnemy(info.target))
            {
                return false;
            }

            if (string.IsNullOrEmpty(info.targetUniqueId))
            {
                return false;
            }

            return info.target.uniqueId == info.targetUniqueId;
        }

        /// <summary>敌人是否在配置攻击距离内（使用世界半径 range/250）</summary>
        public static bool IsInRange(Vector3 from, MonsterCard enemy, float configAttackRange)
        {
            if (!IsValidEnemy(enemy))
            {
                return false;
            }

            var worldRange = ToWorldRange(configAttackRange);
            var distance = Vector2.Distance(from, enemy.transform.position);
            return distance <= worldRange;
        }

        /// <summary>主动技能前方扇形扫描角（朝来敌方向的半圆）</summary>
        public const float SkillForwardScanAngleDegrees = 180f;

        /// <summary>是否为精英或 Boss（配置 <see cref="MonsterQuality.Elite"/> / <see cref="MonsterQuality.Boss"/>）</summary>
        public static bool IsEliteOrBossEnemy(MonsterCard enemy)
        {
            if (enemy == null || enemy._monsterConfig == null)
            {
                return false;
            }

            var quality = enemy._monsterConfig.MonsterQuality;
            return quality == MonsterQuality.Elite || quality == MonsterQuality.Boss;
        }

        /// <summary>
        /// 统一战斗索敌优先级：
        /// 1) 精英/Boss 优先（技能仅前方扇形内；普攻全圆）；
        /// 2) 战士 → 最前方敌人；
        /// 3) 否则按 <paramref name="fallback"/>（技能为前方扇形密度选敌）。
        /// </summary>
        public static bool TrySelectCombatTarget(
            Vector3 fromPosition,
            float configAttackRange,
            CareerType career,
            float moveDirectionY,
            CombatTargetFallback fallback,
            out MonsterCard selected,
            HashSet<string> excludeIds = null,
            Vector3 fanForwardCenter = default)
        {
            var isSkillFan = fallback == CombatTargetFallback.FanCrowd;
            var fanCenter = fanForwardCenter != default
                ? fanForwardCenter
                : fromPosition + Vector3.up;

            if (TrySelectEliteOrBossInRange(
                    fromPosition,
                    configAttackRange,
                    career,
                    moveDirectionY,
                    excludeIds,
                    out selected,
                    isSkillFan,
                    fanCenter,
                    SkillForwardScanAngleDegrees))
            {
                return true;
            }

            if (career == CareerType.Warrior)
            {
                return TrySelectFrontmostInRange(
                    fromPosition,
                    configAttackRange,
                    moveDirectionY,
                    excludeIds,
                    out selected,
                    isSkillFan,
                    fanCenter,
                    SkillForwardScanAngleDegrees);
            }

            if (isSkillFan)
            {
                return TrySelectFanCrowdTarget(
                    fromPosition,
                    fanCenter,
                    ToWorldRange(configAttackRange),
                    out selected,
                    scanAngleDegrees: SkillForwardScanAngleDegrees);
            }

            return TrySelectEnemy(
                fromPosition,
                configAttackRange,
                EnemySelectionRule.NearestInRange,
                excludeIds,
                out selected);
        }

        /// <summary>
        /// 范围内精英/Boss 优先；多只时战士取最前方，其它职业取最近。
        /// <paramref name="restrictToForwardFan"/> 为 true 时仅统计前方扇形内目标（主动技能用）。
        /// </summary>
        public static bool TrySelectEliteOrBossInRange(
            Vector3 fromPosition,
            float configAttackRange,
            CareerType career,
            float moveDirectionY,
            HashSet<string> excludeIds,
            out MonsterCard selected,
            bool restrictToForwardFan = false,
            Vector3 fanForwardCenter = default,
            float forwardScanAngleDegrees = SkillForwardScanAngleDegrees)
        {
            selected = null;
            if (MonsterManager.Instance == null
                || MonsterManager.Instance.enemies == null
                || MonsterManager.Instance.enemies.Count == 0)
            {
                return false;
            }

            var preferFront = career == CareerType.Warrior;
            var bestProgress = float.MinValue;
            var nearestDistanceSqr = float.MaxValue;
            var bestAbsAngle = float.MaxValue;
            var enemies = MonsterManager.Instance.enemies;
            var fanCenter = fanForwardCenter != default
                ? fanForwardCenter
                : fromPosition + Vector3.up;
            Vector2 fanForward = Vector2.up;
            var hasFanForward = !restrictToForwardFan
                || TryBuildFanForward(fromPosition, fanCenter, out fanForward);

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!IsEliteOrBossEnemy(enemy) || !IsCandidate(enemy, excludeIds))
                {
                    continue;
                }

                if (!IsInRange(fromPosition, enemy, configAttackRange))
                {
                    continue;
                }

                if (restrictToForwardFan)
                {
                    if (!hasFanForward
                        || !IsInForwardScan(fromPosition, fanCenter, enemy.transform.position, forwardScanAngleDegrees))
                    {
                        continue;
                    }
                }

                if (preferFront)
                {
                    var progress = GetFrontProgress(enemy.transform.position.y, moveDirectionY);
                    if (progress > bestProgress)
                    {
                        bestProgress = progress;
                        selected = enemy;
                    }
                }
                else if (restrictToForwardFan)
                {
                    // 技能扇形内的精英/Boss：优先更靠近扇形中轴，避免边角精英把穿刺拉歪
                    var offset = ToVector2(enemy.transform.position) - ToVector2(fromPosition);
                    var absAngle = offset.sqrMagnitude <= Mathf.Epsilon
                        ? 0f
                        : Mathf.Abs(Vector2.SignedAngle(fanForward, offset));
                    var enemyPosition = enemy.transform.position + GlobalConst.monsterPosOffset;
                    var distanceSqr = (enemyPosition - fromPosition).sqrMagnitude;
                    if (absAngle < bestAbsAngle - Mathf.Epsilon
                        || (Mathf.Abs(absAngle - bestAbsAngle) <= Mathf.Epsilon && distanceSqr < nearestDistanceSqr))
                    {
                        bestAbsAngle = absAngle;
                        nearestDistanceSqr = distanceSqr;
                        selected = enemy;
                    }
                }
                else
                {
                    var enemyPosition = enemy.transform.position + GlobalConst.monsterPosOffset;
                    var distanceSqr = (enemyPosition - fromPosition).sqrMagnitude;
                    if (distanceSqr < nearestDistanceSqr)
                    {
                        nearestDistanceSqr = distanceSqr;
                        selected = enemy;
                    }
                }
            }

            return selected != null;
        }

        /// <summary>
        /// 攻击范围内最前方的敌人：沿车道 <paramref name="moveDirectionY"/> 行进进度最大者。
        /// </summary>
        public static bool TrySelectFrontmostInRange(
            Vector3 fromPosition,
            float configAttackRange,
            float moveDirectionY,
            HashSet<string> excludeIds,
            out MonsterCard selected,
            bool restrictToForwardFan = false,
            Vector3 fanForwardCenter = default,
            float forwardScanAngleDegrees = SkillForwardScanAngleDegrees)
        {
            selected = null;
            if (MonsterManager.Instance == null
                || MonsterManager.Instance.enemies == null
                || MonsterManager.Instance.enemies.Count == 0)
            {
                return false;
            }

            var bestProgress = float.MinValue;
            var enemies = MonsterManager.Instance.enemies;
            var fanCenter = fanForwardCenter != default
                ? fanForwardCenter
                : fromPosition + Vector3.up;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!IsCandidate(enemy, excludeIds))
                {
                    continue;
                }

                if (!IsInRange(fromPosition, enemy, configAttackRange))
                {
                    continue;
                }

                if (restrictToForwardFan
                    && !IsInForwardScan(fromPosition, fanCenter, enemy.transform.position, forwardScanAngleDegrees))
                {
                    continue;
                }

                var progress = GetFrontProgress(enemy.transform.position.y, moveDirectionY);
                if (progress > bestProgress)
                {
                    bestProgress = progress;
                    selected = enemy;
                }
            }

            return selected != null;
        }

        /// <summary>目标是否落在 origin→forwardCenter 的前方扫描扇形内（有符号夹角）</summary>
        private static bool IsInForwardScan(
            Vector3 origin,
            Vector3 forwardCenter,
            Vector3 enemyPosition,
            float scanAngleDegrees)
        {
            if (!TryBuildFanForward(origin, forwardCenter, out var forward))
            {
                return false;
            }

            var offset = ToVector2(enemyPosition) - ToVector2(origin);
            if (offset.sqrMagnitude <= Mathf.Epsilon)
            {
                return true;
            }

            var halfScan = scanAngleDegrees * 0.5f;
            return Mathf.Abs(Vector2.SignedAngle(forward, offset)) <= halfScan + Mathf.Epsilon;
        }

        /// <summary>
        /// 行进方向上的「前方」进度：怪物沿 moveDirectionY 移动时，该值越大越靠前。
        /// </summary>
        private static float GetFrontProgress(float enemyY, float moveDirectionY)
        {
            if (Mathf.Abs(moveDirectionY) <= Mathf.Epsilon)
            {
                // 无明确方向时与历史 LowestY 一致：Y 越小越靠前
                return -enemyY;
            }

            return enemyY * moveDirectionY;
        }

        private static bool IsCandidate(MonsterCard enemy, HashSet<string> excludeIds)
        {
            if (!IsValidEnemy(enemy))
            {
                return false;
            }

            if (excludeIds != null
                && !string.IsNullOrEmpty(enemy.uniqueId)
                && excludeIds.Contains(enemy.uniqueId))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 按规则选取单个敌人。
        /// </summary>
        /// <param name="fromPosition">索敌起点（通常为英灵 AttackRange 节点位置）</param>
        /// <param name="configAttackRange">
        /// 配置攻击距离。
        /// <see cref="EnemySelectionRule.Nearest"/> 可传 0 忽略距离；
        /// <see cref="EnemySelectionRule.MostCrowded"/> 传 0 表示全场候选，邻域用 <see cref="DefaultCrowdRadius"/>。
        /// </param>
        /// <param name="rule">选敌规则</param>
        /// <param name="excludeIds">排除的怪物 uniqueId，可为 null</param>
        /// <param name="selected">选中的怪物，失败时为 null</param>
        public static bool TrySelectEnemy(
            Vector3 fromPosition,
            float configAttackRange,
            EnemySelectionRule rule,
            HashSet<string> excludeIds,
            out MonsterCard selected)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.TargetSearch))
            {
                return TrySelectEnemyInternal(fromPosition, configAttackRange, rule, excludeIds, out selected);
            }
        }

        private static bool TrySelectEnemyInternal(
            Vector3 fromPosition,
            float configAttackRange,
            EnemySelectionRule rule,
            HashSet<string> excludeIds,
            out MonsterCard selected)
        {
            selected = null;

            if (MonsterManager.Instance == null
                || MonsterManager.Instance.enemies == null
                || MonsterManager.Instance.enemies.Count == 0)
            {
                return false;
            }

            switch (rule)
            {
                case EnemySelectionRule.LowestYInRange:
                    return TrySelectLowestYInRange(fromPosition, configAttackRange, excludeIds, out selected);
                case EnemySelectionRule.NearestInRange:
                    return TrySelectNearestInRange(fromPosition, configAttackRange, excludeIds, out selected);
                case EnemySelectionRule.Nearest:
                    return TrySelectNearest(fromPosition, excludeIds, out selected);
                case EnemySelectionRule.MostCrowded:
                    return TrySelectMostCrowded(fromPosition, configAttackRange, excludeIds, out selected);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 在扇形区域内按规则选取单个敌人。
        /// </summary>
        /// <param name="fan">扇形参数：Origin 为顶点，CenterPoint 与 Origin 确定中心轴朝向</param>
        /// <param name="rule">
        /// <see cref="EnemySelectionRule.FanSmallestAngle"/> /
        /// <see cref="EnemySelectionRule.FanNearest"/> /
        /// <see cref="EnemySelectionRule.FanAngleDistanceWeighted"/>
        /// </param>
        /// <param name="excludeIds">排除的怪物 uniqueId，可为 null</param>
        /// <param name="selected">选中的怪物</param>
        public static bool TrySelectEnemyInFan(
            FanSectorParams fan,
            EnemySelectionRule rule,
            HashSet<string> excludeIds,
            out MonsterCard selected)
        {
            selected = null;

            if (!IsFanSelectionRule(rule))
            {
                return false;
            }

            if (MonsterManager.Instance == null
                || MonsterManager.Instance.enemies == null
                || MonsterManager.Instance.enemies.Count == 0)
            {
                return false;
            }

            if (!TryBuildFanForward(fan.Origin, fan.CenterPoint, out var forward))
            {
                return false;
            }

            var halfAngle = fan.AngleDegrees > 0f ? fan.AngleDegrees * 0.5f : DefaultFanAngleDegrees * 0.5f;
            var candidates = CollectFanCandidates(fan, forward, halfAngle, excludeIds);
            if (candidates.Count == 0)
            {
                return false;
            }

            switch (rule)
            {
                case EnemySelectionRule.FanSmallestAngle:
                    return TrySelectFanBySmallestAngle(candidates, out selected);
                case EnemySelectionRule.FanNearest:
                    return TrySelectFanByNearest(candidates, out selected);
                case EnemySelectionRule.FanAngleDistanceWeighted:
                    return TrySelectFanByWeightedScore(candidates, out selected);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 扇形索敌便捷重载：传入顶点与中点，其余使用默认张角 30°。
        /// </summary>
        public static bool TrySelectEnemyInFan(
            Vector3 fanOrigin,
            Vector3 fanCenterPoint,
            EnemySelectionRule rule,
            HashSet<string> excludeIds,
            out MonsterCard selected,
            float fanAngleDegrees = DefaultFanAngleDegrees,
            float maxDistance = 0f)
        {
            var fan = new FanSectorParams(fanOrigin, fanCenterPoint, fanAngleDegrees, maxDistance);
            return TrySelectEnemyInFan(fan, rule, excludeIds, out selected);
        }

        /// <summary>判断敌人是否在扇形内（用于调试或多目标扩展）</summary>
        public static bool IsEnemyInFan(MonsterCard enemy, FanSectorParams fan)
        {
            if (!IsValidEnemy(enemy))
            {
                return false;
            }

            if (!TryBuildFanForward(fan.Origin, fan.CenterPoint, out var forward))
            {
                return false;
            }

            var halfAngle = fan.AngleDegrees > 0f ? fan.AngleDegrees * 0.5f : DefaultFanAngleDegrees * 0.5f;
            var origin = ToVector2(fan.Origin);
            var target = ToVector2(enemy.transform.position + GlobalConst.monsterPosOffset);
            return IsInsideFanSector(origin, forward, halfAngle, fan.MaxDistance, target);
        }

        /// <summary>
        /// 扇形方向密度选敌：前方扫描角内用滑动角度窗口统计密度（默认半圆 + 15° 窗口），
        /// 取最密方向，再在窗口内选最靠近该方向、距离更近的目标。
        /// 角度使用有符号夹角，避免左右对称被当成同一方向。
        /// </summary>
        /// <param name="origin">扇形顶点（应与普攻 AttackRange 原点一致）</param>
        /// <param name="forwardCenter">0° 参考方向上的点（与 origin 确定中轴朝向）</param>
        /// <param name="maxDistance">最远检测距离（世界单位，0 = 不限距离）</param>
        /// <param name="selected">选中的敌人</param>
        /// <param name="scanAngleDegrees">扫描总角度，默认前方半圆 <see cref="SkillForwardScanAngleDegrees"/></param>
        /// <param name="windowAngleDegrees">密度滑动窗口张角，默认 <see cref="SkillFanAngleDegrees"/></param>
        public static bool TrySelectFanCrowdTarget(
            Vector3 origin,
            Vector3 forwardCenter,
            float maxDistance,
            out MonsterCard selected,
            float scanAngleDegrees = SkillForwardScanAngleDegrees,
            float windowAngleDegrees = SkillFanAngleDegrees)
        {
            selected = null;

            if (MonsterManager.Instance == null
                || MonsterManager.Instance.enemies == null
                || MonsterManager.Instance.enemies.Count == 0)
            {
                return false;
            }

            if (!TryBuildFanForward(origin, forwardCenter, out var forward))
            {
                return false;
            }

            var origin2 = ToVector2(origin);
            var halfScan = scanAngleDegrees * 0.5f;
            var halfWindow = windowAngleDegrees * 0.5f;
            var enemies = MonsterManager.Instance.enemies;

            // 1) 收集前方半圆候选（有符号角度）
            var candidates = new List<FanCandidate>();
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!IsValidEnemy(enemy))
                {
                    continue;
                }

                var enemyPos2 = ToVector2(enemy.transform.position);
                var rangeDistance = Vector2.Distance(origin2, enemyPos2);
                if (maxDistance > 0f && rangeDistance > maxDistance)
                {
                    continue;
                }

                var aimPos2 = ToVector2(enemy.transform.position + GlobalConst.monsterPosOffset);
                var offset = aimPos2 - origin2;
                if (offset.sqrMagnitude <= Mathf.Epsilon)
                {
                    candidates.Add(new FanCandidate
                    {
                        Enemy = enemy,
                        SignedAngleDegrees = 0f,
                        Distance = rangeDistance,
                    });
                    continue;
                }

                var signedAngle = Vector2.SignedAngle(forward, offset);
                if (Mathf.Abs(signedAngle) > halfScan)
                {
                    continue;
                }

                candidates.Add(new FanCandidate
                {
                    Enemy = enemy,
                    SignedAngleDegrees = signedAngle,
                    Distance = rangeDistance,
                });
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            // 2) 以每个候选为中心开滑动窗口统计密度；平手优先更靠近正前方
            var bestCount = -1;
            var bestAngle = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                var count = 0;
                for (int j = 0; j < candidates.Count; j++)
                {
                    if (Mathf.Abs(candidates[i].SignedAngleDegrees - candidates[j].SignedAngleDegrees)
                        <= halfWindow)
                    {
                        count++;
                    }
                }

                if (count > bestCount
                    || (count == bestCount
                        && Mathf.Abs(candidates[i].SignedAngleDegrees) < Mathf.Abs(bestAngle)))
                {
                    bestCount = count;
                    bestAngle = candidates[i].SignedAngleDegrees;
                }
            }

            // 3) 最密窗口内：优先靠近窗口中心角，并列取更近者（贴近前排）
            var bestAngleDelta = float.MaxValue;
            var bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                var angleDelta = Mathf.Abs(candidates[i].SignedAngleDegrees - bestAngle);
                if (angleDelta > halfWindow)
                {
                    continue;
                }

                if (angleDelta < bestAngleDelta - Mathf.Epsilon
                    || (Mathf.Abs(angleDelta - bestAngleDelta) <= Mathf.Epsilon
                        && candidates[i].Distance < bestDistance))
                {
                    bestAngleDelta = angleDelta;
                    bestDistance = candidates[i].Distance;
                    selected = candidates[i].Enemy;
                }
            }

            return selected != null;
        }

        /// <summary>
        /// 选取最多 count 个敌人：优先 primaryTarget，其余按距离从近到远且不重复。
        /// 供多目标弹道等技能节点使用。
        /// </summary>
        public static List<MonsterCard> SelectEnemies(
            Vector3 fromPosition,
            int count,
            MonsterCard primaryTarget = null,
            HashSet<string> allocatedIds = null)
        {
            var result = new List<MonsterCard>();
            if (count <= 0)
            {
                return result;
            }

            var allocated = allocatedIds ?? new HashSet<string>();

            if (primaryTarget != null
                && IsValidEnemy(primaryTarget)
                && !string.IsNullOrEmpty(primaryTarget.uniqueId))
            {
                result.Add(primaryTarget);
                allocated.Add(primaryTarget.uniqueId);
            }

            while (result.Count < count)
            {
                if (!TrySelectNearest(fromPosition, allocated, out var enemy))
                {
                    break;
                }

                result.Add(enemy);
                allocated.Add(enemy.uniqueId);
            }

            return result;
        }

        /// <summary>从怪物构建目标快照（含偏移后的 targetPosition）</summary>
        public static BattleTargetInfo FromMonster(MonsterCard enemy)
        {
            return new BattleTargetInfo
            {
                isEnemy = true,
                targetPosition = enemy.transform.position + GlobalConst.monsterPosOffset,
                target = enemy,
                targetUniqueId = enemy.uniqueId,
            };
        }

        /// <summary>从世界坐标构建非敌人目标（如手动指定落点）</summary>
        public static BattleTargetInfo FromPosition(Vector3 worldPosition)
        {
            return new BattleTargetInfo
            {
                isEnemy = false,
                targetPosition = worldPosition + GlobalConst.monsterPosOffset,
                target = null,
                targetUniqueId = null,
            };
        }

        /// <summary>
        /// 解析本次攻击/技能的实际落点。
        /// 敌人已死亡或对象池复用（uniqueId 变化）时回退到快照中的 targetPosition，避免弹道/朝向跟到出生点。
        /// </summary>
        public static Vector3 ResolveAttackPosition(BattleTargetInfo info)
        {
            if (!info.isEnemy)
            {
                return info.targetPosition;
            }

            if (info.target == null)
            {
                return info.targetPosition;
            }

            if (info.target.monsterData.monsterState == MonsterState.die)
            {
                return info.targetPosition;
            }

            if (!string.IsNullOrEmpty(info.targetUniqueId)
                && info.target.uniqueId != info.targetUniqueId)
            {
                return info.targetPosition;
            }

            return info.target.transform.position + GlobalConst.monsterPosOffset;
        }

        private static bool IsFanSelectionRule(EnemySelectionRule rule)
        {
            return rule == EnemySelectionRule.FanSmallestAngle
                   || rule == EnemySelectionRule.FanNearest
                   || rule == EnemySelectionRule.FanAngleDistanceWeighted;
        }

        /// <summary>由顶点→中点构建扇形中心轴单位向量（XY 平面）</summary>
        private static bool TryBuildFanForward(Vector3 origin, Vector3 centerPoint, out Vector2 forward)
        {
            forward = ToVector2(centerPoint) - ToVector2(origin);
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                LogTool.LogWarning("[EnemyTargetFinder] 扇形 Origin 与 CenterPoint 重合，使用默认朝向 Vector2.right");
                forward = Vector2.right;
                return true;
            }

            forward.Normalize();
            return true;
        }

        private static List<FanCandidate> CollectFanCandidates(
            FanSectorParams fan,
            Vector2 forward,
            float halfAngleDegrees,
            HashSet<string> excludeIds)
        {
            var candidates = new List<FanCandidate>();
            var origin = ToVector2(fan.Origin);
            var enemies = MonsterManager.Instance.enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!IsValidEnemy(enemy))
                {
                    continue;
                }

                if (excludeIds != null
                    && !string.IsNullOrEmpty(enemy.uniqueId)
                    && excludeIds.Contains(enemy.uniqueId))
                {
                    continue;
                }

                var target = ToVector2(enemy.transform.position + GlobalConst.monsterPosOffset);
                if (!IsInsideFanSector(origin, forward, halfAngleDegrees, fan.MaxDistance, target))
                {
                    continue;
                }

                var offset = target - origin;
                var signedAngle = offset.sqrMagnitude <= Mathf.Epsilon
                    ? 0f
                    : Vector2.SignedAngle(forward, offset);
                var distance = offset.magnitude;
                candidates.Add(new FanCandidate
                {
                    Enemy = enemy,
                    SignedAngleDegrees = signedAngle,
                    Distance = distance,
                });
            }

            return candidates;
        }

        /// <summary>点是否在扇形内：距离上限 + 与中心轴夹角 ≤ 半张角</summary>
        private static bool IsInsideFanSector(
            Vector2 origin,
            Vector2 forward,
            float halfAngleDegrees,
            float maxDistance,
            Vector2 target)
        {
            var offset = target - origin;
            var distance = offset.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return true;
            }

            if (maxDistance > 0f && distance > maxDistance)
            {
                return false;
            }

            var angleDegrees = Mathf.Abs(Vector2.SignedAngle(forward, offset));
            return angleDegrees <= halfAngleDegrees + Mathf.Epsilon;
        }

        private static bool TrySelectFanBySmallestAngle(List<FanCandidate> candidates, out MonsterCard selected)
        {
            selected = null;
            var minAngle = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                var absAngle = Mathf.Abs(candidates[i].SignedAngleDegrees);
                if (absAngle < minAngle)
                {
                    minAngle = absAngle;
                    selected = candidates[i].Enemy;
                }
            }

            return selected != null;
        }

        private static bool TrySelectFanByNearest(List<FanCandidate> candidates, out MonsterCard selected)
        {
            selected = null;
            var minDistance = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Distance < minDistance)
                {
                    minDistance = candidates[i].Distance;
                    selected = candidates[i].Enemy;
                }
            }

            return selected != null;
        }

        /// <summary>
        /// 在扇形候选内归一化角度与距离后加权：0.45×角度 + 0.45×距离，得分最低者胜出。
        /// </summary>
        private static bool TrySelectFanByWeightedScore(List<FanCandidate> candidates, out MonsterCard selected)
        {
            selected = null;
            var maxAngle = 0f;
            var maxDistance = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                var absAngle = Mathf.Abs(candidates[i].SignedAngleDegrees);
                if (absAngle > maxAngle)
                {
                    maxAngle = absAngle;
                }

                if (candidates[i].Distance > maxDistance)
                {
                    maxDistance = candidates[i].Distance;
                }
            }

            var bestScore = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                var absAngle = Mathf.Abs(candidates[i].SignedAngleDegrees);
                var angleNorm = maxAngle > Mathf.Epsilon ? absAngle / maxAngle : 0f;
                var distanceNorm = maxDistance > Mathf.Epsilon ? candidates[i].Distance / maxDistance : 0f;
                var score = FanAnglePriorityWeight * angleNorm + FanDistancePriorityWeight * distanceNorm;

                if (score < bestScore)
                {
                    bestScore = score;
                    selected = candidates[i].Enemy;
                }
            }

            return selected != null;
        }

        /// <summary>范围内 Y 最小（最靠前）的敌人，原 SoldierAtkRange 选敌逻辑</summary>
        private static bool TrySelectLowestYInRange(
            Vector3 fromPosition,
            float configAttackRange,
            HashSet<string> excludeIds,
            out MonsterCard selected)
        {
            selected = null;
            var minY = float.MaxValue;
            var enemies = MonsterManager.Instance.enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!IsValidEnemy(enemy))
                {
                    continue;
                }

                if (excludeIds != null
                    && !string.IsNullOrEmpty(enemy.uniqueId)
                    && excludeIds.Contains(enemy.uniqueId))
                {
                    continue;
                }

                if (!IsInRange(fromPosition, enemy, configAttackRange))
                {
                    continue;
                }

                var y = enemy.transform.position.y;
                if (y < minY)
                {
                    minY = y;
                    selected = enemy;
                }
            }

            return selected != null;
        }

        /// <summary>攻击范围内距离最近的敌人（含 monsterPosOffset）</summary>
        private static bool TrySelectNearestInRange(
            Vector3 fromPosition,
            float configAttackRange,
            HashSet<string> excludeIds,
            out MonsterCard selected)
        {
            selected = null;
            var nearestDistanceSqr = float.MaxValue;
            var enemies = MonsterManager.Instance.enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!IsValidEnemy(enemy))
                {
                    continue;
                }

                if (excludeIds != null
                    && !string.IsNullOrEmpty(enemy.uniqueId)
                    && excludeIds.Contains(enemy.uniqueId))
                {
                    continue;
                }

                if (!IsInRange(fromPosition, enemy, configAttackRange))
                {
                    continue;
                }

                var enemyPosition = enemy.transform.position + GlobalConst.monsterPosOffset;
                var distanceSqr = (enemyPosition - fromPosition).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    selected = enemy;
                }
            }

            return selected != null;
        }

        /// <summary>
        /// 邻域内敌人最多的目标；平手时取距 fromPosition 更近者。
        /// configAttackRange&gt;0：只在攻击范围内选候选，邻域半径用该世界半径；
        /// 否则全场候选，邻域半径用 <see cref="DefaultCrowdRadius"/>。
        /// </summary>
        private static bool TrySelectMostCrowded(
            Vector3 fromPosition,
            float configAttackRange,
            HashSet<string> excludeIds,
            out MonsterCard selected)
        {
            selected = null;
            var enemies = MonsterManager.Instance.enemies;
            var limitByRange = configAttackRange > 0f;
            var crowdRadius = limitByRange ? ToWorldRange(configAttackRange) : DefaultCrowdRadius;
            var crowdRadiusSqr = crowdRadius * crowdRadius;

            var bestCrowdCount = -1;
            var bestDistanceSqr = float.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                var candidate = enemies[i];
                if (!IsValidEnemy(candidate))
                {
                    continue;
                }

                if (excludeIds != null
                    && !string.IsNullOrEmpty(candidate.uniqueId)
                    && excludeIds.Contains(candidate.uniqueId))
                {
                    continue;
                }

                if (limitByRange && !IsInRange(fromPosition, candidate, configAttackRange))
                {
                    continue;
                }

                var candidatePos = candidate.transform.position + GlobalConst.monsterPosOffset;
                var crowdCount = 0;
                for (int j = 0; j < enemies.Count; j++)
                {
                    var neighbor = enemies[j];
                    if (!IsValidEnemy(neighbor))
                    {
                        continue;
                    }

                    var neighborPos = neighbor.transform.position + GlobalConst.monsterPosOffset;
                    if ((neighborPos - candidatePos).sqrMagnitude <= crowdRadiusSqr)
                    {
                        crowdCount++;
                    }
                }

                var distanceSqr = (candidatePos - fromPosition).sqrMagnitude;
                if (crowdCount > bestCrowdCount
                    || (crowdCount == bestCrowdCount && distanceSqr < bestDistanceSqr))
                {
                    bestCrowdCount = crowdCount;
                    bestDistanceSqr = distanceSqr;
                    selected = candidate;
                }
            }

            return selected != null;
        }

        /// <summary>全场距离最近的敌人（含 monsterPosOffset，无距离上限）</summary>
        private static bool TrySelectNearest(
            Vector3 fromPosition,
            HashSet<string> excludeIds,
            out MonsterCard selected)
        {
            selected = null;
            var nearestDistanceSqr = float.MaxValue;
            var enemies = MonsterManager.Instance.enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!IsValidEnemy(enemy))
                {
                    continue;
                }

                if (excludeIds != null
                    && !string.IsNullOrEmpty(enemy.uniqueId)
                    && excludeIds.Contains(enemy.uniqueId))
                {
                    continue;
                }

                var enemyPosition = enemy.transform.position + GlobalConst.monsterPosOffset;
                var distanceSqr = (enemyPosition - fromPosition).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    selected = enemy;
                }
            }

            return selected != null;
        }

        private static Vector2 ToVector2(Vector3 v) => new Vector2(v.x, v.y);
    }
}
