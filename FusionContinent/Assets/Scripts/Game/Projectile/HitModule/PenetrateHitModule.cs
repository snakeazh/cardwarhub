using System.Collections.Generic;
using Config;
using FusionContinent;
using UnityEngine;

/// <summary>
/// 穿透命中模块 - 使用射线检测，命中后继续飞行
/// </summary>
public class PenetrateHitModule : IProjectileHitModule
{
    /// <summary>
    /// 最大穿透次数，0 表示无限穿透
    /// </summary>
    public int MaxCount;
    
    /// <summary>
    /// 当前穿透计数
    /// </summary>
    public int curCount;

    /// <summary>
    /// 对同一目标的重复伤害间隔（秒）
    /// = 0：每个目标只能受伤一次
    /// > 0：每隔此时间允许对同一目标再次造成伤害
    /// </summary>
    public float _hitTargetDuration;

    /// <summary>
    /// 对每个目标的重复命中计时器
    /// </summary>
    private readonly Dictionary<string, float> _hitTimers = new Dictionary<string, float>();

    // 复用缓存，避免GC
    private static readonly List<string> _tempKeys = new List<string>();

    /// <summary>
    /// 收集命中结果 - 射线检测
    /// </summary>
    public bool CollectHits(Projectile_new p, HitContext context)
    {
        float tickDeltaTime = p.HitDetectionDeltaTime > 0f
            ? p.HitDetectionDeltaTime
            : Time.deltaTime * Global.SpeedMultiplier.Value;

        // 持续伤害模式：每帧更新计时器
        if (_hitTargetDuration > 0f)
        {
            _tempKeys.Clear();
            foreach (var key in _hitTimers.Keys)
            {
                _tempKeys.Add(key);
            }

            for (int i = 0; i < _tempKeys.Count; i++)
            {
                string key = _tempKeys[i];
                _hitTimers[key] += tickDeltaTime;
            }
        }

        // 执行射线检测
        Vector2 currentPos = p.transform.position;
        Vector2 forwardDir = p.transform.up;

        float moveDistance = p.Config.speed * tickDeltaTime;
        // 增加检测范围，确保能检测到薄型目标和高速子弹
        float rayLength = moveDistance;

#if UNITY_EDITOR
        Debug.DrawRay(currentPos, forwardDir * 3f, Color.red);
        Debug.DrawRay(currentPos, p.Direction * rayLength, Color.green);
#endif
        // Debug.Log(rayLength);
        var hits = HitSystem2D.Ray(currentPos, forwardDir, rayLength,
            LayerMask.GetMask("MonsterBox"));

        // 过滤已命中目标，添加有效命中到 context
        int validHitCount = 0;
        foreach (var hit in hits)
        {
            if (hit.target == null) continue;
            if (hit.target.name != "HitBox") continue;

            MonsterCard monster = hit.target.transform.parent.GetComponent<MonsterCard>();
            if (monster == null || !monster.enabled) continue;

            string uniqueId = monster.uniqueId;

            // 检查是否在排除列表中（分裂子弹不应再次命中的目标）
            if (p.ExcludedTargetIds != null && p.ExcludedTargetIds.Contains(uniqueId))
            {
                continue;
            }

            // 检查 _hitTimers（同一个目标在一个 Tick 中只能命中一次）
            if (_hitTimers.ContainsKey(uniqueId))
            {
                // 非持续伤害模式或计时器未超时：跳过
                if (_hitTargetDuration <= 0f || _hitTimers[uniqueId] < _hitTargetDuration)
                {
                    continue;
                }
                // 超时：移除，允许再次命中
                _hitTimers.Remove(uniqueId);
            }

            // 添加到上下文
            context.AddHits(new List<HitResult> { hit });
            context.AddHitUniqueId(uniqueId);
            validHitCount++;
        }

        return validHitCount > 0;
    }

    /// <summary>
    /// 处理命中 - 执行伤害逻辑
    /// </summary>
    public void OnHit(Projectile_new p, HitContext context)
    {
        foreach (var hit in context.Hits)
        {
            if (hit.target == null) continue;
            if (hit.target.name != "HitBox") continue;

            MonsterCard monster = hit.target.transform.parent.GetComponent<MonsterCard>();
            if (monster == null || !monster.enabled) continue;

            string uniqueId = monster.uniqueId;

            // 更新该目标的计时器（标记已命中）
            _hitTimers[uniqueId] = 0f;

            // 将命中目标添加到子弹的排除列表（确保分裂模块能获取到）
            if (!string.IsNullOrEmpty(uniqueId))
            {
                p.ExcludedTargetIds.Add(uniqueId);
            }

            // 发送命中事件
            EventBus.Publish(new ProjectileHitEvent()
            {
                skillContext = p._skillContext,
                hitpoint = hit.hitPoint,
                monsterCard = monster
            });

            curCount++;
            if (curCount >= MaxCount && MaxCount > 0)
            {
                p.MissionOver();
                break;
            }
        }

        // 播放命中特效
        if (!string.IsNullOrEmpty(p.Config.hitEffect))
        {
            EffectManager.Instance.PlayEffect(p.Config.hitEffect, p.transform.position);
        }
    }

    /// <summary>
    /// 重置命中目标列表
    /// </summary>
    public void ResetHitTarget(Projectile_new p)
    {
        _hitTimers.Clear();
    }
}
