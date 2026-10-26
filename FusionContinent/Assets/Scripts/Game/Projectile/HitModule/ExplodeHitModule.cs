using System.Collections.Generic;
using FusionContinent;
using UnityEngine;

/// <summary>
/// 爆炸命中模块 - 射线/到达触发后，将爆炸范围内所有敌人写入 HitContext
/// </summary>
public class ExplodeHitModule : IProjectileHitModule
{
    private static readonly int MonsterBoxLayer = LayerMask.GetMask("MonsterBox");

    /// <summary>
    /// 爆炸半径（米）
    /// </summary>
    public float radius;

    /// <summary>
    /// 爆炸延迟时间（秒）
    /// </summary>
    public float loadingtime;

    /// <summary>
    /// 收集命中结果：触发后将爆炸范围内敌人写入 context（供 AttachBuff 等模块复用）
    /// </summary>
    public bool CollectHits(Projectile_new p, HitContext context)
    {
        Vector2 currentPos = p.transform.position;
        if (!p.ForceHitAtCurrentPosition && !HasExplosionTrigger(p, currentPos))
        {
            return false;
        }

        CollectExplosionTargets(p, context, currentPos);
        return context.Hits.Count > 0;
    }

    /// <summary>
    /// 处理命中 - 对 context 中的爆炸目标造成伤害
    /// </summary>
    public void OnHit(Projectile_new p, HitContext context)
    {
        for (int i = 0; i < context.Hits.Count; i++)
        {
            var hit = context.Hits[i];
            if (!TryGetMonster(p, hit, out MonsterCard monster))
            {
                continue;
            }

            EventBus.Publish(new ProjectileHitEvent()
            {
                skillContext = p._skillContext,
                hitpoint = hit.hitPoint,
                monsterCard = monster
            });
        }

        if (!string.IsNullOrEmpty(p.Config.hitEffect))
        {
            EffectManager.Instance.PlayEffect(p.Config.hitEffect, p.transform.position);
        }

        p.RequestMissionOver();
    }

    public void ResetHitTarget(Projectile_new p)
    {
    }

    private bool HasExplosionTrigger(Projectile_new p, Vector2 currentPos)
    {
        Vector2 lastPos = p.HitCheckSegmentStart;
        Vector2 movement = currentPos - lastPos;
        float distance = movement.magnitude;

        List<HitResult> hits;
        if (distance > 0.0001f)
        {
            hits = HitSystem2D.Ray(lastPos, movement.normalized, distance + p.Config.checkRadius, MonsterBoxLayer);
        }
        else
        {
            float tickDeltaTime = p.HitDetectionDeltaTime > 0f
                ? p.HitDetectionDeltaTime
                : Time.deltaTime * Global.SpeedMultiplier.Value;
            float rayLength = p.Config.speed * tickDeltaTime + p.Config.checkRadius;
            Vector2 forwardDir = ((Vector2)p.Direction).sqrMagnitude > 0.0001f
                ? ((Vector2)p.Direction).normalized
                : (Vector2)p.transform.up;
            hits = HitSystem2D.Ray(currentPos, forwardDir, rayLength, MonsterBoxLayer);
        }

        for (int i = 0; i < hits.Count; i++)
        {
            if (TryGetMonster(p, hits[i], out _))
            {
                return true;
            }
        }

        return false;
    }

    private void CollectExplosionTargets(Projectile_new p, HitContext context, Vector2 center)
    {
        float explodeRadius = radius > 0f ? radius : 3f;
        var hits = HitSystem2D.Circle(center, explodeRadius, MonsterBoxLayer);
        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            if (!TryGetMonster(p, hit, out MonsterCard monster))
            {
                continue;
            }

            string uniqueId = monster.uniqueId;
            if (context.HasHit(uniqueId))
            {
                continue;
            }

            context.AddHits(new List<HitResult> { hit });
            context.AddHitUniqueId(uniqueId);
        }
    }

    private bool TryGetMonster(Projectile_new p, HitResult hit, out MonsterCard monster)
    {
        monster = null;
        if (hit.target == null)
        {
            return false;
        }

        if (hit.target.name != "HitBox")
        {
            return false;
        }

        monster = hit.target.transform.parent.GetComponent<MonsterCard>();
        if (monster == null || !monster.enabled)
        {
            return false;
        }

        if (p.ExcludedTargetIds != null && p.ExcludedTargetIds.Contains(monster.uniqueId))
        {
            return false;
        }

        return true;
    }
}
