using Config;
using FusionContinent;
using FusionContinent.Skill;
using FusionContinent.Skill.Handler;
using UnityEngine;

/// <summary>
/// 怪物受击请求：单段伤害或技能多类型伤害。
/// </summary>
public struct MonsterDamageRequest
{
    public MonsterCard Target;
    public TakeDamageType DamageType;
    public int BaseDamage;
    public ISkill Skill;
    public SoldierCard Soldier;
    public Vector3 HitPosition;
    public float DamageInheritance;
    public DamagePipelineType PipelineType;
    public bool IsAgainDamage;
    public bool SuppressKaLinCooldown;
    public bool SkipBattleRuleBeforeTrigger;
    public bool ApplySkillSumDamageTypes;

    public SkillContext SkillContext;

    public static MonsterDamageRequest ForSkillSumTypes(
        MonsterCard target,
        ISkill skill,
        Vector3 hitPosition,
        float damageInheritance = 1f,
        SoldierCard soldier = null,
        SkillContext skillContext = null)
    {
        return new MonsterDamageRequest
        {
            Target = target,
            Skill = skill,
            HitPosition = hitPosition,
            DamageInheritance = damageInheritance,
            Soldier = soldier,
            SkillContext = skillContext,
            ApplySkillSumDamageTypes = true,
            PipelineType = DamagePipelineType.SoldierDamage
        };
    }

    public static MonsterDamageRequest Single(
        MonsterCard target,
        TakeDamageType damageType,
        int baseDamage,
        ISkill skill,
        Vector3 hitPosition,
        SoldierCard soldier = null,
        DamagePipelineType pipelineType = DamagePipelineType.SoldierDamage,
        bool isAgainDamage = false,
        bool skipBattleRuleBeforeTrigger = false,
        bool suppressKaLinCooldown = false)
    {
        return new MonsterDamageRequest
        {
            Target = target,
            DamageType = damageType,
            BaseDamage = baseDamage,
            Skill = skill,
            Soldier = soldier,
            HitPosition = hitPosition,
            PipelineType = pipelineType,
            IsAgainDamage = isAgainDamage,
            SuppressKaLinCooldown = suppressKaLinCooldown,
            SkipBattleRuleBeforeTrigger = skipBattleRuleBeforeTrigger,
            ApplySkillSumDamageTypes = false,
            DamageInheritance = 1f
        };
    }
}
