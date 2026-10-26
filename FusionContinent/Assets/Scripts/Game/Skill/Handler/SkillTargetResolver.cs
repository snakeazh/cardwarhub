using System.Collections.Generic;
using FusionContinent.Buff;
using FusionContinent.Framework.Graph;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 技能图目标解析：供 SelectTarget、ApplyBuff 等节点共用。
    /// </summary>
    public static class SkillTargetResolver
    {
        public const string ModeSelected = "Selected";

        public static List<IBuffable> Resolve(string target, SkillContext context, bool excludePreviousSelected = false)
        {
            var buffTargets = new List<IBuffable>();

            switch (target)
            {
                case "Target":
                    if (context.Target is MonsterCard targetMonster)
                    {
                        if (SkillHitTargetGuard.CanApplyBuffToMonster(targetMonster, context))
                            buffTargets.Add(targetMonster);
                    }
                    else if (context.Target is IBuffable targetBuffable)
                    {
                        buffTargets.Add(targetBuffable);
                    }
                    break;

                case "Self":
                    if (context.Caster is IBuffable selfBuffable)
                        buffTargets.Add(selfBuffable);
                    break;

                case "EnemyGroup":
                    buffTargets.AddRange(GetEnemyGroup(context));
                    break;

                case "AllyGroup":
                    buffTargets.AddRange(GetAllyGroup(context));
                    break;

                case "RandomAlly":
                    AddRandomAllyTarget(buffTargets, context, includeSelf: true, excludePreviousSelected);
                    break;

                case "RandomAllyExcludeSelf":
                    AddRandomAllyTarget(buffTargets, context, includeSelf: false, excludePreviousSelected);
                    break;

                case "RandomEnemy":
                    AddRandomEnemyTarget(buffTargets, context, excludePreviousSelected);
                    break;

                case "Hit":
                    foreach (var monster in context.HitMonsters)
                    {
                        if (SkillHitTargetGuard.CanApplyBuffToMonster(monster, context))
                            buffTargets.Add(monster);
                    }
                    break;

                case ModeSelected:
                    buffTargets.AddRange(ResolveFromSelected(context));
                    break;
            }

            return buffTargets;
        }

        public static void WriteSelectedTargets(SkillContext context, List<IBuffable> targets, bool clearBefore)
        {
            if (clearBefore)
                context.SelectedTargets.Clear();

            foreach (var target in targets)
            {
                if (target is not Component component)
                    continue;

                if (!context.SelectedTargets.Contains(component))
                    context.SelectedTargets.Add(component);

                if (target is MonsterCard monster)
                    SkillHitTargetGuard.SnapshotMonster(context, monster);
            }
        }

        /// <summary>
        /// 将最新选取目标同步到 context.Target / TargetPosition，供默认瞄准的下游节点复用。
        /// </summary>
        public static void ApplySelectionToContext(SkillContext context)
        {
            if (context.SelectedTargets.Count == 0)
                return;

            var latest = context.SelectedTargets[context.SelectedTargets.Count - 1];
            context.Target = latest;
            context.TargetPosition = latest.transform.position;
        }

        public static void SortLoopBodyEdgesSelectTargetFirst(SkillGraphConfig config, List<EdgeConfig> bodyEdges)
        {
            bodyEdges.Sort((a, b) =>
            {
                bool aIsSelectTarget = config.GetNode(a.ToNodeId)?.NodeType == "SelectTarget";
                bool bIsSelectTarget = config.GetNode(b.ToNodeId)?.NodeType == "SelectTarget";
                if (aIsSelectTarget == bIsSelectTarget)
                    return 0;
                return aIsSelectTarget ? -1 : 1;
            });
        }

        public static List<IBuffable> ResolveFromSelected(SkillContext context)
        {
            var list = new List<IBuffable>();
            foreach (var component in context.SelectedTargets)
            {
                if (component is MonsterCard monster)
                {
                    if (SkillHitTargetGuard.CanApplyBuffToMonster(monster, context))
                        list.Add(monster);
                }
                else if (component is IBuffable buffable)
                {
                    list.Add(buffable);
                }
            }
            return list;
        }

        public static bool TryGetFirstSelectedPosition(SkillContext context, out Vector3 position)
        {
            return TryGetLatestSelectedPosition(context, out position);
        }

        public static bool TryGetLatestSelectedPosition(SkillContext context, out Vector3 position)
        {
            if (context.SelectedTargets.Count == 0)
            {
                position = Vector3.zero;
                return false;
            }

            position = context.SelectedTargets[context.SelectedTargets.Count - 1].transform.position;
            return true;
        }

        public static bool TryGetLatestSelectedTransform(SkillContext context, out Transform transform)
        {
            transform = null;
            if (context.SelectedTargets.Count == 0)
                return false;

            transform = context.SelectedTargets[context.SelectedTargets.Count - 1].transform;
            return true;
        }

        /// <summary>
        /// 解析投射物瞄准点（Projectile / FanProjectile 的 aim_target）。
        /// </summary>
        public static bool TryResolveProjectileAim(
            SkillContext context,
            string aimTarget,
            string logTag,
            out Vector3 aimPosition,
            out Transform homingTransform)
        {
            aimPosition = Vector3.zero;
            homingTransform = null;

            if (aimTarget == ModeSelected)
            {
                if (!TryGetLatestSelectedTransform(context, out homingTransform))
                {
                    LogTool.LogWarning($"[{logTag}] aim_target=Selected 但无已选取目标");
                    return false;
                }

                aimPosition = homingTransform.position;
                return true;
            }

            if (context.Target != null)
            {
                aimPosition = context.Target.transform.position;
                homingTransform = context.Target.transform;
                return true;
            }

            aimPosition = context.TargetPosition;
            return true;
        }

        /// <summary>
        /// 解析 Effect / AnimatedEffect 播放位置（含随机偏移缓存）。
        /// </summary>
        public static bool TryResolveEffectPosition(
            SkillContext context,
            string position,
            float randomOffsetRadius,
            string logTag,
            out Vector3 targetPos)
        {
            if (!context.HasEffectOffset && randomOffsetRadius > 0f)
            {
                context.EffectOffset = Random.insideUnitCircle * randomOffsetRadius;
                context.HasEffectOffset = true;
            }

            var offset = new Vector3(context.EffectOffset.x, context.EffectOffset.y, 0);
            targetPos = Vector3.zero;

            switch (position)
            {
                case "Target":
                    targetPos = context.TargetPosition + offset;
                    return true;
                case "Caster":
                    targetPos = context.Caster.transform.position + offset;
                    return true;
                case "Enemy":
                    targetPos = context.Target.transform.position + offset;
                    return true;
                case ModeSelected:
                    if (!TryGetFirstSelectedPosition(context, out targetPos))
                    {
                        LogTool.LogWarning($"[{logTag}] position=Selected 但无已选取目标，跳过特效");
                        return false;
                    }
                    targetPos += offset;
                    return true;
                default:
                    targetPos = context.TargetPosition + offset;
                    return true;
            }
        }

        private static List<IBuffable> GetEnemyGroup(SkillContext context)
        {
            var targets = new List<IBuffable>();
            bool casterIsAlly = IsAlly(context.Caster);

            if (casterIsAlly)
            {
                if (MonsterManager.Instance != null)
                {
                    foreach (var monster in MonsterManager.Instance.enemies)
                    {
                        if (monster != null)
                            targets.Add(monster);
                    }
                }
            }
            else if (GamePlayManager.Default != null)
            {
                foreach (var soldier in GamePlayManager.Default.villageGameCards)
                {
                    if (soldier != null && !soldier.isDie)
                        targets.Add(soldier);
                }
            }

            return targets;
        }

        private static List<IBuffable> GetAllyGroup(SkillContext context)
        {
            var targets = new List<IBuffable>();
            bool casterIsAlly = IsAlly(context.Caster);

            if (casterIsAlly)
            {
                if (GamePlayManager.Default != null)
                {
                    foreach (var soldier in GamePlayManager.Default.villageGameCards)
                    {
                        if (soldier != null && !soldier.isDie)
                            targets.Add(soldier);
                    }
                }
            }
            else if (MonsterManager.Instance != null)
            {
                foreach (var monster in MonsterManager.Instance.enemies)
                {
                    if (monster != null)
                        targets.Add(monster);
                }
            }

            if (context.Caster is IBuffable caster && !targets.Contains(caster))
                targets.Add(caster);

            return targets;
        }

        private static void AddRandomAllyTarget(List<IBuffable> buffTargets, SkillContext context, bool includeSelf, bool excludePreviousSelected)
        {
            List<IBuffable> candidates = BuildRandomCandidates(GetAllyGroup(context), includeSelf, context, excludePreviousSelected);
            if (candidates.Count == 0)
                return;

            var picked = candidates[Random.Range(0, candidates.Count)];
            buffTargets.Add(picked);
            RecordSelectTargetHistory(context, picked);
        }

        private static void AddRandomEnemyTarget(List<IBuffable> buffTargets, SkillContext context, bool excludePreviousSelected)
        {
            List<IBuffable> candidates = BuildRandomCandidates(GetEnemyGroup(context), includeSelf: true, context, excludePreviousSelected);
            if (candidates.Count == 0)
                return;

            var picked = candidates[Random.Range(0, candidates.Count)];
            buffTargets.Add(picked);
            RecordSelectTargetHistory(context, picked);
        }

        private static List<IBuffable> BuildRandomCandidates(
            List<IBuffable> source,
            bool includeSelf,
            SkillContext context,
            bool excludePreviousSelected)
        {
            var candidates = new List<IBuffable>(source);

            RemoveCasterFromCandidates(candidates, context, includeSelf);

            if (excludePreviousSelected && context.SelectTargetHistory.Count > 0)
            {
                candidates.RemoveAll(candidate =>
                    candidate is Component component && context.SelectTargetHistory.Contains(component));
            }

            if (candidates.Count == 0 && excludePreviousSelected && source.Count > 0)
            {
                LogTool.Log("[SkillTargetResolver] 随机候选已用尽，重新允许已选目标");
                candidates = new List<IBuffable>(source);
                RemoveCasterFromCandidates(candidates, context, includeSelf);
            }

            return candidates;
        }

        private static void RemoveCasterFromCandidates(List<IBuffable> candidates, SkillContext context, bool includeSelf)
        {
            if (includeSelf || context.Caster is not IBuffable caster)
                return;

            candidates.Remove(caster);
        }

        private static void RecordSelectTargetHistory(SkillContext context, IBuffable picked)
        {
            if (picked is Component component)
                context.SelectTargetHistory.Add(component);
        }

        private static bool IsAlly(Component target)
        {
            return target is SoldierCard;
        }
    }
}
