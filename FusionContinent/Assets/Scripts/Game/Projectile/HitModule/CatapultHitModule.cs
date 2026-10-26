using Config;
using FusionContinent;
using UnityEngine;

/// <summary>
/// 弹射命中模块 - 命中敌人后若目标死亡，则朝下一个敌人弹射并继续攻击。
/// 需配合 PenetrateHitModule 使用，本模块不执行碰撞检测与伤害。
/// </summary>
public class CatapultHitModule : IProjectileHitModule
{
    /// <summary>
    /// 最大弹射次数，0 表示不限制
    /// </summary>
    public int MaxRicochetCount;

    /// <summary>
    /// 当前已弹射次数
    /// </summary>
    public int CurRicochetCount;

    /// <summary>
    /// 搜索下一个敌人的最大距离，0 表示不限制
    /// </summary>
    public float SearchRadius;

    public bool CollectHits(Projectile_new p, HitContext context)
    {
        return false;
    }

    public void OnHit(Projectile_new p, HitContext context)
    {
        if (context.Hits.Count == 0)
        {
            return;
        }

        if (!HasKilledTarget(context))
        {
            return;
        }

        if (MaxRicochetCount > 0 && CurRicochetCount >= MaxRicochetCount)
        {
            return;
        }

        if (!TryFindNextEnemy(p, out MonsterCard nextEnemy))
        {
            return;
        }

        CurRicochetCount++;

        Vector3 targetPos = nextEnemy.transform.position + GlobalConst.monsterPosOffset;
        Vector3 direction = (targetPos - p.transform.position).normalized;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        p.CancelMissionOver();
        p.RedirectToDirection(direction);
    }

    public void ResetHitTarget(Projectile_new p)
    {
        CurRicochetCount = 0;
    }

    private static bool HasKilledTarget(HitContext context)
    {
        for (int i = 0; i < context.Hits.Count; i++)
        {
            HitResult hit = context.Hits[i];
            if (hit.target == null || hit.target.name != "HitBox")
            {
                continue;
            }

            MonsterCard monster = hit.target.transform.parent.GetComponent<MonsterCard>();
            if (monster == null)
            {
                continue;
            }

            if (IsMonsterDead(monster))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMonsterDead(MonsterCard monster)
    {
        return monster.monsterData.monsterState == MonsterState.die || monster.hp <= 0;
    }

    private bool TryFindNextEnemy(Projectile_new p, out MonsterCard nearestEnemy)
    {
        nearestEnemy = null;
        float nearestDistanceSqr = float.MaxValue;
        float maxDistanceSqr = SearchRadius > 0f ? SearchRadius * SearchRadius : float.MaxValue;

        if (MonsterManager.Instance == null || MonsterManager.Instance.enemies == null)
        {
            return false;
        }

        Vector3 fromPosition = p.transform.position;
        for (int i = 0; i < MonsterManager.Instance.enemies.Count; i++)
        {
            MonsterCard enemy = MonsterManager.Instance.enemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (IsMonsterDead(enemy))
            {
                continue;
            }

            if (p.ExcludedTargetIds.Contains(enemy.uniqueId))
            {
                continue;
            }

            Vector3 enemyPosition = enemy.transform.position + GlobalConst.monsterPosOffset;
            float distanceSqr = (enemyPosition - fromPosition).sqrMagnitude;
            if (distanceSqr > maxDistanceSqr)
            {
                continue;
            }

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy != null;
    }
}
