using System.Collections.Generic;
using FusionContinent;
using FusionContinent.Buff;
using FusionContinent.Skill.Handler;
using UnityEngine;

/// <summary>
/// 附加Buff命中模块 - 命中目标时施加Buff
/// 所有Buff配置从 SkillBuffTemplateProvider 获取
/// </summary>
public class AttachBuffHitModule : IProjectileHitModule
{
    /// <summary>
    /// Buff 实例 ID，从配置中获取所有属性
    /// </summary>
    public int BuffId { get; set; }

    /// <summary>
    /// Buff作用目标：Enemy / Self / Ally
    /// </summary>
    public string Target { get; set; } = "Enemy";

    /// <summary>
    /// Buff施加概率
    /// </summary>
    public float Probability { get; set; } = 1f;

    /// <summary>
    /// 每个目标只生效一次
    /// </summary>
    public bool OneHitPerTarget { get; set; }

    /// <summary>
    /// 已施加过Buff的目标ID集合
    /// </summary>
    private readonly HashSet<string> _appliedTargets = new HashSet<string>();

    /// <summary>
    /// 收集命中结果 - 射线检测
    /// </summary>
    public bool CollectHits(Projectile_new p, HitContext context)
    {
        return false;
    }

    /// <summary>
    /// 处理命中 - 根据Target决定Buff作用目标
    /// </summary>
    public void OnHit(Projectile_new p, HitContext context)
    {
        var skillContext = p._skillContext;
        var caster = skillContext?.Caster;

        for (int i = 0; i < context.Hits.Count; i++)
        {
            var hit = context.Hits[i];
            if (hit.target == null) continue;

            // 概率判断
            if (Probability < 1f && UnityEngine.Random.value > Probability)
            {
                continue;
            }

            // 根据 Target 类型决定Buff作用目标
            if (Target == "Self")
            {
                if (caster == null) continue;

                var uniqueId = $"{caster}_{BuffId}";
                if (OneHitPerTarget && _appliedTargets.Contains(uniqueId))
                {
                    continue;
                }

                var target = caster.GetComponent<IBuffable>();
                if (target != null && BuffApplier.ApplyBuff(BuffId, skillContext, target))
                {
                    _appliedTargets.Add(uniqueId);
                    LogTool.Log($"[AttachBuffHitModule] 向 {caster.name} (Self) 施加 Buff: buffId={BuffId}");
                }
            }
            else if (Target == "Ally")
            {
                // Ally: 对命中目标作为队友施加buff（需要获取命中目标的队友）
                MonsterCard hitMonster = null;
                if (hit.target.name == "HitBox")
                {
                    hitMonster = hit.target.transform.parent.GetComponent<MonsterCard>();
                }

                if (hitMonster == null) continue;
                if (!SkillHitTargetGuard.IsMonsterAlive(hitMonster)) continue;

                // 获取队友（同一个阵营的MonsterCard）
                var allies = FindAllies(hitMonster);
                foreach (var ally in allies)
                {
                    var uniqueId = $"{ally.uniqueId}_{BuffId}";
                    if (OneHitPerTarget && _appliedTargets.Contains(uniqueId))
                    {
                        continue;
                    }

                    var allyTarget = (IBuffable)ally;
                    if (BuffApplier.ApplyBuff(BuffId, skillContext, allyTarget))
                    {
                        _appliedTargets.Add(uniqueId);
                        LogTool.Log($"[AttachBuffHitModule] 向 {ally.name} (Ally) 施加 Buff: buffId={BuffId}");
                    }
                }
            }
            else // "Enemy" 或默认
            {
                MonsterCard monster = null;
                if (hit.target.name == "HitBox")
                {
                    monster = hit.target.transform.parent.GetComponent<MonsterCard>();
                }

                if (monster == null) continue;
                if (!SkillHitTargetGuard.IsMonsterAlive(monster)) continue;

                var uniqueId = $"{monster.uniqueId}_{BuffId}";
                if (OneHitPerTarget && _appliedTargets.Contains(uniqueId))
                {
                    continue;
                }

                var target = (IBuffable)monster;
                if (BuffApplier.ApplyBuff(BuffId, skillContext, target, hitPosition: context.Hits[i].hitPoint))
                {
                    _appliedTargets.Add(uniqueId);
                    if (!string.IsNullOrEmpty(monster.uniqueId))
                    {
                        p.ExcludedTargetIds.Add(monster.uniqueId);
                    }

                    LogTool.Log($"[AttachBuffHitModule] 向 {monster.name} 施加 Buff: buffId={BuffId}");
                }
            }
        }
    }

    /// <summary>
    /// 查找同阵营的所有友方单位
    /// </summary>
    private List<MonsterCard> FindAllies(MonsterCard target)
    {
        var allies = new List<MonsterCard>();
        // 这里简化实现，实际可能需要根据游戏阵营系统来获取
        var monsters = Object.FindObjectsOfType<MonsterCard>();
        foreach (var m in monsters)
        {
            // 简单判断：与目标同类型的为友方（实际应根据阵营判断）
            if (m != target && m.uniqueId == target.uniqueId)
            {
                allies.Add(m);
            }
        }
        return allies;
    }

    /// <summary>
    /// 重置命中目标列表
    /// </summary>
    public void ResetHitTarget(Projectile_new p)
    {
        _appliedTargets.Clear();
    }
}
