using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using QFramework;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 分裂命中模块 - 负责处理子弹命中后的分裂逻辑
/// 需要配合其他命中模块使用，分裂模块不执行伤害，只在命中后触发分裂
/// </summary>
public class SplitHitModule : IProjectileHitModule
{
    // ==================== 配置属性（由 ProjectileHandler 设置）====================
    /// <summary>
    /// 分裂子弹数量
    /// </summary>
    public int SplitsNum { get; set; } = 2;

    /// <summary>
    /// 分裂伤害继承比例
    /// </summary>
    public float SplitsDamage { get; set; } = 1f;

    /// <summary>
    /// 再次分裂伤害继承比例
    /// </summary>
    public float SplitsDamageAgain { get; set; } = 1f;

    /// <summary>
    /// 分裂次数（0=不分裂，1=可以再分裂1次，2=可以再分裂2次）
    /// </summary>
    public int SplitsAgainCount { get; set; } = 0;

    /// <summary>
    /// 母弹飞行速度（用于分裂子弹统一按母弹速度 / 2，避免二次分裂速度被连续减半）
    /// </summary>
    public float OriginalSpeed { get; set; } = 0f;

    // ==================== 运行时数据 ====================
    /// <summary>
    /// 当前分裂代数，用于记录分裂到第几层
    /// </summary>
    public int CurrentSplitGeneration { get; set; } = 0;

    /// <summary>
    /// 父子弹命中的目标ID集合（用于继承排除目标）
    /// </summary>
    public HashSet<string> ParentExcludedTargetIds { get; set; } = new HashSet<string>();

    private Vector3 _direction;
    private ISkill _skillCard;
    private SoldierCard _parentCard;
    private string _projectileName;

    /// <summary>
    /// 收集命中结果 - 分裂模块不检测碰撞，返回 false
    /// </summary>
    public bool CollectHits(Projectile_new p, HitContext context)
    {
        // 分裂模块不参与碰撞检测
        return false;
    }

    /// <summary>
    /// 处理命中 - 执行分裂逻辑
    /// </summary>
    public void OnHit(Projectile_new p, HitContext context)
    {
        // 获取分裂参数
        _skillCard = p.skillCard;
        _direction = p.Direction;
        _parentCard = p.parentCard;
        _projectileName = _skillCard?.Config?.ProjectileName ?? "ArcherProjectile";

        if (OriginalSpeed <= 0f)
        {
            OriginalSpeed = p.Config.speed;
        }

        // 初始化分裂参数（优先使用配置属性，否则从技能配置读取）
        InitSplitParams();

        // 如果当前代数 < 总分裂次数，执行分裂
        if (SplitsNum > 0 && CurrentSplitGeneration < SplitsAgainCount)
        {
            // 分裂箭特效：首次分裂 / 再次分裂使用不同预制体
            _projectileName = CurrentSplitGeneration == 0
                ? "ArcherProjectileSplits"
                : "ArcherProjectileSplitsAgain";

            // 获取命中位置
            Vector3 hitTargetPos = p.LastHitPosition;
            if (hitTargetPos == Vector3.zero && context.Hits != null && context.Hits.Count > 0)
            {
                hitTargetPos = context.Hits[0].hitPoint;
            }

            // 执行分裂
            // 首次分裂：原伤害 * SplitsDamage
            // 再次分裂：原伤害 * SplitsDamageAgain
            float damageForThisSplit = CurrentSplitGeneration == 0
                ? SplitsDamage
                : SplitsDamageAgain;
            SplitProjectile(hitTargetPos, damageForThisSplit, p, context);
        }
    }

    /// <summary>
    /// 初始化分裂参数 - 优先使用配置属性，否则从技能配置读取
    /// </summary>
    private void InitSplitParams()
    {
        // 如果配置属性已设置，优先使用配置属性
        if (SplitsNum <= 0)
        {
            // 配置属性未设置，从技能配置读取
            var splitSkill = _skillCard?.Parent?.GetSkillByType(SpecialEffectType.FenLie);
            if (splitSkill != null)
            {
                SplitsNum = (int)splitSkill.Attributes.GetFinal(AttributeType.SkillSplitsNum);
                SplitsDamage = splitSkill.Attributes.GetFinal(AttributeType.SkillSplitsDamage);
                SplitsAgainCount = (int)_skillCard.Attributes.GetFinal(AttributeType.SkillSplitsNumAgain);
                SplitsDamageAgain = _skillCard.Attributes.GetFinal(AttributeType.SkillSplitsDamageAgain);
            }
        }
    }

    /// <summary>
    /// 分裂子弹 - 首次分裂按 SplitsNum 数量；再次分裂固定只生成 1 颗
    /// </summary>
    private void SplitProjectile(Vector3 hitTargetPos, float damageInheritance, Projectile_new parentProjectile, HitContext context)
    {
        var baseExcludedTargets = BuildBaseExcludedTargets(parentProjectile, context);
        var allocatedTargetIdsInBurst = new HashSet<string>();
        int splitCount = CurrentSplitGeneration == 0 ? SplitsNum : 1;

        for (int i = 0; i < splitCount; i++)
        {
            float angleOffset = CalculateAngleOffset(i, splitCount);
            Vector3 finalDirection = Quaternion.Euler(0, 0, z: angleOffset) * _direction;

            // 计算子子弹的分裂代数
            int childGeneration = CurrentSplitGeneration + 1;
            // 仅首次分裂的子弹可再分裂一次；再次分裂的子弹不再分裂
            bool canChildSplitAgain = CurrentSplitGeneration == 0 && childGeneration < SplitsAgainCount;

            CreateSplitProjectile(
                hitTargetPos,
                damageInheritance,
                childGeneration,
                canChildSplitAgain,
                baseExcludedTargets,
                allocatedTargetIdsInBurst);
        }

        // 分裂后，当前子弹的分裂代数递增（防止重复分裂）
        CurrentSplitGeneration++;
    }

    /// <summary>
    /// 计算角度偏移 - 保持对称分布
    /// </summary>
    private float CalculateAngleOffset(int index, int total)
    {
        if (total <= 1)
        {
            return Random.Range(-45f, 45f);
        }

        int halfCount = total / 2;
        if (index < halfCount)
        {
            return Random.Range(-45f, -15f);
        }
        else if (index > halfCount)
        {
            return Random.Range(15f, 45f);
        }
        else
        {
            return 0f;
        }
    }

    /// <summary>
    /// 创建分裂子弹
    /// </summary>
    private void CreateSplitProjectile(
        Vector3 hitTargetPos,
        float damageInheritance,
        int childGeneration,
        bool canChildSplitAgain,
        HashSet<string> baseExcludedTargets,
        HashSet<string> allocatedTargetIdsInBurst)
    {
        var childExcludedTargets = new HashSet<string>(baseExcludedTargets);

        // 分裂子弹优先朝最近可攻击敌人飞行，且同一轮分裂不重复目标。
        if (!TryResolveNearestEnemyDirection(
                hitTargetPos,
                childExcludedTargets,
                allocatedTargetIdsInBurst,
                out var targetDirection,
                out var selectedTargetId))
        {
            return;
        }

        if (!string.IsNullOrEmpty(selectedTargetId) && allocatedTargetIdsInBurst != null)
        {
            allocatedTargetIdsInBurst.Add(selectedTargetId);
        }

        var splitProjectile = EffectManager.Instance.PlayProjectileEffect(
            _projectileName,
            hitTargetPos,
            10f,
            scale: Vector3.one,
            parent: GameLand.Default != null ? GameLand.Default.Bullet : null);
        if (splitProjectile == null)
        {
            return;
        }

        var splitProjectileScript = splitProjectile.GetComponent<Projectile_new>();
        if (splitProjectileScript == null)
        {
            splitProjectileScript = splitProjectile.AddComponent<Projectile_new>();
        }

        if (splitProjectileScript == null)
        {
            return;
        }

        // 配置分裂子弹参数并启动
        var childSplitModule = new SplitHitModule
        {
            SplitsNum = this.SplitsNum,
            SplitsDamage = this.SplitsDamage,
            SplitsDamageAgain = this.SplitsDamageAgain,
            SplitsAgainCount = canChildSplitAgain ? this.SplitsAgainCount : 0,
            OriginalSpeed = this.OriginalSpeed,
            CurrentSplitGeneration = childGeneration,
            ParentExcludedTargetIds = new HashSet<string>(childExcludedTargets)
        };

        splitProjectileScript.LaunchSplitWithModules(
            targetDirection,
            OriginalSpeed / 2f,
            _skillCard,
            _parentCard,
            damageInheritance,
            canChildSplitAgain ? this.SplitsAgainCount : 0,
            childSplitModule,
            childExcludedTargets
        );

        // 将分裂伤害倍率写入 SkillContext（MonsterManager 使用 ExtraNormalAttackDamage 做最终倍率叠加）
        // 约定：最终伤害倍率 = 1 + ExtraNormalAttackDamage，因此这里传入 (damageInheritance - 1)
        if (splitProjectileScript._skillContext != null)
        {
            splitProjectileScript._skillContext.SetExtraData("ExtraNormalAttackDamage", damageInheritance - 1f);
        }
    }

    private HashSet<string> BuildBaseExcludedTargets(Projectile_new parentProjectile, HitContext context)
    {
        var baseExcludedTargets = new HashSet<string>(ParentExcludedTargetIds);

        if (parentProjectile?.ExcludedTargetIds != null)
        {
            foreach (var targetId in parentProjectile.ExcludedTargetIds)
            {
                baseExcludedTargets.Add(targetId);
            }
        }

        // 兜底再加一次当前命中目标，确保不会分裂回当前击中的怪物。
        if (context?.Hits != null)
        {
            for (int i = 0; i < context.Hits.Count; i++)
            {
                var hit = context.Hits[i];
                if (hit?.target == null || hit.target.name != "HitBox")
                {
                    continue;
                }

                var monster = hit.target.transform.parent != null ? hit.target.transform.parent.GetComponent<MonsterCard>() : null;
                if (monster != null && !string.IsNullOrEmpty(monster.uniqueId))
                {
                    baseExcludedTargets.Add(monster.uniqueId);
                }
            }
        }

        return baseExcludedTargets;
    }

    private bool TryResolveNearestEnemyDirection(
        Vector3 fromPosition,
        HashSet<string> hardExcludedTargetIds,
        HashSet<string> preferredExcludedTargetIds,
        out Vector3 targetDirection,
        out string selectedTargetId)
    {
        targetDirection = Vector3.zero;
        selectedTargetId = null;
        if (!TryFindNearestEnemy(fromPosition, hardExcludedTargetIds, preferredExcludedTargetIds, out var nearestEnemy))
        {
            return false;
        }

        selectedTargetId = nearestEnemy.uniqueId;
        var targetPos = nearestEnemy.transform.position + GlobalConst.monsterPosOffset;
        var direction = (targetPos - fromPosition).normalized;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        targetDirection = direction;
        return true;
    }

    private bool TryFindNearestEnemy(
        Vector3 fromPosition,
        HashSet<string> hardExcludedTargetIds,
        HashSet<string> preferredExcludedTargetIds,
        out MonsterCard nearestEnemy)
    {
        nearestEnemy = null;
        float nearestDistanceSqr = float.MaxValue;

        if (MonsterManager.Instance == null || MonsterManager.Instance.enemies == null || MonsterManager.Instance.enemies.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < MonsterManager.Instance.enemies.Count; i++)
        {
            var enemy = MonsterManager.Instance.enemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (enemy.monsterData.monsterState == MonsterState.die)
            {
                continue;
            }

            if (hardExcludedTargetIds != null && hardExcludedTargetIds.Contains(enemy.uniqueId))
            {
                continue;
            }

            if (preferredExcludedTargetIds != null && preferredExcludedTargetIds.Contains(enemy.uniqueId))
            {
                continue;
            }

            var enemyPosition = enemy.transform.position + GlobalConst.monsterPosOffset;
            float distanceSqr = (enemyPosition - fromPosition).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy != null;
    }

    /// <summary>
    /// 重置命中目标列表
    /// </summary>
    public void ResetHitTarget(Projectile_new p)
    {
        // 分裂模块不需要重置
    }
}
