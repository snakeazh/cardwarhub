using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

/// <summary>
/// 矩形检测穿透模块
/// 使用矩形（攻击盒）进行碰撞检测，支持穿透多个目标
/// 矩形尺寸基于子弹的宽度和长度
/// </summary>
public class RectHitModule : IProjectileHitModule
{
    /// <summary>
    /// 最大穿透次数，0或不设置表示无限穿透
    /// </summary>
    public int MaxCount;

    /// <summary>
    /// 当前穿透计数
    /// </summary>
    public int curCount;

    /// <summary>
    /// 对同一目标的重复伤害间隔（秒），0表示每次碰撞都造成伤害
    /// </summary>
    public float _hitTargetDuration;

    /// <summary>
    /// 矩形宽度（米）
    /// </summary>
    public float rectWidth;

    /// <summary>
    /// 矩形长度（米）
    /// </summary>
    public float rectLength;

    /// <summary>
    /// 记录每个目标最后一次命中的时间 { monsterUniqueId -> lastHitTime }
    /// </summary>
    private Dictionary<string, float> _lastHitTimes = new Dictionary<string, float>();

    /// <summary>
    /// 收集命中结果 - 矩形检测
    /// </summary>
    public bool CollectHits(Projectile_new p, HitContext context)
    {
        Vector2 currentPos = p.transform.position;
        Vector2 forwardDir = p.transform.up;

        // 使用配置的矩形尺寸
        float width = rectWidth > 0 ? rectWidth : 1f;
        float length = rectLength > 0 ? rectLength : 1f;

        // 矩形角度：forwardDir 是特效 up 朝向，转换为 X+ 轴对齐的角度
        float angle = Mathf.Atan2(forwardDir.y, forwardDir.x) * Mathf.Rad2Deg;

        // 计算检测位置：沿飞行方向偏移半个长度，避免漏检
        Vector2 checkPos = currentPos + forwardDir * (length * 0.5f);
        Vector2 size = new Vector2(width, length);

        // 调试绘制：显示实际碰撞矩形
        DebugDrawHelper.DrawBox(p.transform.position, size, angle, new Color(1f, 0.2f, 0.2f), 3f);

        var hits = HitSystem2D.Box(p.transform.position, size, angle, LayerMask.GetMask("MonsterBox"));

        // 过滤并添加到上下���
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

            // 检查命中间隔
            if (_hitTargetDuration > 0f)
            {
                if (_lastHitTimes.TryGetValue(uniqueId, out float lastHitTime))
                {
                    if (currentTime - lastHitTime < _hitTargetDuration)
                    {
                        continue; // 未达到间隔，跳过
                    }
                }
            }

            // 添加到上下文
            context.AddHits(new List<HitResult> { hit });
            context.AddHitUniqueId(uniqueId);
            validHitCount++;

            // 记录命中时间
            if (_hitTargetDuration > 0f)
            {
                _lastHitTimes[uniqueId] = currentTime;
            }
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

            curCount++;

            // 发送命中事件
            EventBus.Publish(new ProjectileHitEvent()
            {
                skillContext = p._skillContext,
                hitpoint = hit.hitPoint,
                monsterCard = monster
            });

            if (MaxCount > 0 && curCount >= MaxCount)
            {
                p.MissionOver();
                return;
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
        _lastHitTimes.Clear();
    }
}