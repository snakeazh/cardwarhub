using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

/// <summary>
/// AOE 范围伤害模块
/// 直接对子弹当前位置周围范围内的所有敌人造成伤害
/// 支持持续伤害模式（每帧或间隔检测）
/// </summary>
public class AoeHitModule : IProjectileHitModule
{
    /// <summary>
    /// AOE 伤害半径（米）
    /// </summary>
    public float radius = 3f;

    /// <summary>
    /// 全局检测间隔（秒），0 表示每帧检测
    /// 优先使用 _hitTargetDuration 控制“同一目标受伤间隔”
    /// </summary>
    public float checkInterval = 0f;

    /// <summary>
    /// 对同一目标的重复伤害间隔（秒）
    /// = 0：每个目标只能受伤一次
    /// > 0：每隔此时间允许对同一目标再次造成伤害
    /// </summary>
    public float _hitTargetDuration;

    /// <summary>
    /// 记录每个目标最后一次命中的时间 { monsterUniqueId -> lastHitTime }
    /// </summary>
    private readonly Dictionary<string, float> _lastHitTimes = new Dictionary<string, float>();

    /// <summary>
    /// 当前检测间隔计时器
    /// </summary>
    private float _checkTimer;

    /// <summary>
    /// 是否已经触发过伤害（单次AOE模式）
    /// </summary>
    private bool _hasDealtDamage;

    /// <summary>
    /// 是否是单次触发模式
    /// </summary>
    public bool singleTrigger = false;

    /// <summary>
    /// 收集命中结果 - 圆形范围检测
    /// </summary>
    public bool CollectHits(Projectile_new p, HitContext context)
    {
        float tickDeltaTime = p.HitDetectionDeltaTime > 0f
            ? p.HitDetectionDeltaTime
            : Time.deltaTime * Global.SpeedMultiplier.Value;

        // 单次触发模式下，已经触发过就不要再检测
        if (singleTrigger && _hasDealtDamage)
        {
            return false;
        }

        // 检测间隔判断
        if (checkInterval > 0f)
        {
            _checkTimer += tickDeltaTime;
            if (_checkTimer < checkInterval)
            {
                return false;
            }
            _checkTimer = 0f;
        }

        // 执行 AOE 检测
        Vector2 currentPos = p.transform.position;
        
#if UNITY_EDITOR
        DebugDrawHelper.DrawCircle(p.transform.position, radius, new Color(1f, 0.2f, 0.2f), 3f);
#endif
        
        var hits = HitSystem2D.Circle(currentPos, radius, LayerMask.GetMask("MonsterBox"));

        // 过滤并添加到上下文
        int validHitCount = 0;
        float currentTime = Time.time;
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

            // 同一怪物受伤间隔控制：间隔内不重复受伤
            if (_hitTargetDuration > 0f)
            {
                if (_lastHitTimes.TryGetValue(uniqueId, out float lastHitTime))
                {
                    if (currentTime - lastHitTime < _hitTargetDuration)
                    {
                        continue;
                    }
                }
            }
            else if (context.HasHit(uniqueId))
            {
                // 不配置间隔时，默认同一目标仅命中一次
                continue;
            }

            // 添加到上下文
            context.AddHits(new List<HitResult> { hit });
            context.AddHitUniqueId(uniqueId);
            validHitCount++;

            if (_hitTargetDuration > 0f)
            {
                _lastHitTimes[uniqueId] = currentTime;
            }
        }

        // 单次触发模式
        if (singleTrigger && validHitCount > 0)
        {
            _hasDealtDamage = true;
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

            // 发送命中事件
            EventBus.Publish(new ProjectileHitEvent()
            {
                skillContext = p._skillContext,
                hitpoint = hit.hitPoint,
                monsterCard = monster
            });
        }

        // 播放命中特效
        if (!string.IsNullOrEmpty(p.Config.hitEffect))
        {
            EffectManager.Instance.PlayEffect(p.Config.hitEffect, p.transform.position);
        }

        // 单次触发模式结束后销毁子弹
        if (singleTrigger)
        {
            p.MissionOver();
        }
    }

    /// <summary>
    /// 重置命中目标列表
    /// </summary>
    public void ResetHitTarget(Projectile_new p)
    {
        _lastHitTimes.Clear();
        _hasDealtDamage = false;
        _checkTimer = 0f;
    }
}
