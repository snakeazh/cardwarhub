using Config;
using FusionContinent;
using FusionContinent.Guide;
using FusionContinent.Skill;
using FusionContinent.Skill.Handler;
using Game.Battle;
using UnityEngine;
using Unity.Profiling;

/// <summary>
/// 怪物受击编排：门禁、多类型技能伤害、管线计算、附加魔法伤害。
/// </summary>
public static class MonsterDamageService
{
    public static int ReceiveDamage(in MonsterDamageRequest request)
    {
        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.DamagePipeline))
        {
            if (request.Target == null)
            {
                return 0;
            }

            if (request.ApplySkillSumDamageTypes)
            {
                ApplySkillDamageTypes(in request);
                return 0;
            }

            return ReceiveSingleDamage(in request);
        }
    }

    private const int ExecuteDamageValue = 99999;

    public static bool CanReceiveDamage(MonsterCard target, int damage)
    {
        if (damage <= 0)
        {
            return false;
        }

        if (target.monsterData.monsterAbnormal != null && target.monsterData.monsterAbnormal.invincible)
        {
            return false;
        }

        if (target.BuffController != null && target.BuffController.HasBuff(BuffType.Invincible))
        {
            return false;
        }

        return true;
    }

    private static bool IsInvincible(MonsterCard target)
    {
        if (target.monsterData.monsterAbnormal != null && target.monsterData.monsterAbnormal.invincible)
        {
            return true;
        }

        return target.BuffController != null && target.BuffController.HasBuff(BuffType.Invincible);
    }

    private static void ApplySkillDamageTypes(in MonsterDamageRequest request)
    {
        var target = request.Target;
        var skillCard = request.Skill;
        var pos = request.HitPosition;
        var damageInheritance = request.DamageInheritance;
        var soldierCard = request.Soldier;

        if (skillCard == null || damageInheritance <= 0)
        {
            return;
        }

        var types = skillCard.SumDamageTypes();
        if (types == null)
        {
            return;
        }

        var skillPerParamOne = skillCard.Attributes.GetFinal(AttributeType.SkillPerParamOne);
        var monsterParamOne = target.Attributes.GetFinal(AttributeType.MonsterParamOne);
        var extraDamageByMonsterParam = Mathf.CeilToInt(skillPerParamOne * monsterParamOne);

        if (soldierCard != null && skillCard.Id != soldierCard.NormalAttackSkillId)
        {
            var addVal = skillCard.Parent.BaseAttributes.GetFinal(AttributeType.RedoubleDamage);
            var tempAddVal = skillCard.Parent.BaseAttributes.GetFinal(AttributeType.TempRedoubleDamage);
            if (tempAddVal != 0)
            {
                damageInheritance += tempAddVal;
                skillCard.Parent.BaseAttributes.GetAttr(AttributeType.TempRedoubleDamage).Clear();
            }

            damageInheritance += addVal;
        }

        var hasKaLinBuff = KaLinBuffCombatSupport.HasKaLinBuff(target);
        var dealtSkillDamage = false;

        foreach (var v in types)
        {
            var baseDamage = v.Value;
            if (skillCard.Config.DamageType.Contains(v.Key))
            {
                baseDamage += extraDamageByMonsterParam;
            }

            var damage = Mathf.CeilToInt(baseDamage * damageInheritance);
            var singleRequest = MonsterDamageRequest.Single(
                target,
                v.Key,
                damage,
                skillCard,
                pos,
                soldierCard,
                suppressKaLinCooldown: true);
            singleRequest.SkillContext = request.SkillContext;
            var applied = ReceiveSingleDamage(in singleRequest);
            if (applied > 0)
            {
                dealtSkillDamage = true;
            }

            var damageToHealth = skillCard.Parent.BaseAttributes.GetFinal(AttributeType.SoldierDamageToHealth);
            if (damageToHealth > 0f)
            {
                var takeDamageToHealth = Mathf.FloorToInt(damage * damageToHealth);
                skillCard.Parent.SetSoldierHp(takeDamageToHealth);
            }

            skillCard.Parent.TotalDamage += damage;
            Global.TotalDamage.Value += damage;
        }

        if (hasKaLinBuff && dealtSkillDamage)
        {
            LogTool.Log(
                $"[KaLinBuff] 多段伤害命中后触发 CD 修正: hitSkillId={skillCard.Id}, monster={target.uniqueId}");
            KaLinBuffCombatSupport.TryApplyCooldownReduction(skillCard, soldierCard);
        }

        if (target.monsterData.monsterState == MonsterState.move)
        {
            var takeMoveSpeed = target.Attributes.GetFinal(AttributeType.MonsterFirstHitMoveSpeedCorrection);
            if (takeMoveSpeed != 0f && !target.HandlingTagsContains("MonsterFirstHitMoveSpeedCorrection"))
            {
                target.AddHandlingTag("MonsterFirstHitMoveSpeedCorrection");
                target.monsterData.monsterTweens.MonsterFirstHitMoveSpeedCorrection(takeMoveSpeed);
            }
        }
    }

    private static int ReceiveSingleDamage(in MonsterDamageRequest request)
    {
        var target = request.Target;
        var damageType = request.DamageType;
        var skillCard = request.Skill;
        var pos = request.HitPosition;
        var soldierCard = request.Soldier;
        var pipelineType = request.PipelineType;
        var isAgainDamage = request.IsAgainDamage;
        var suppressKaLinCooldown = request.SuppressKaLinCooldown;
        var skipBattleRuleBeforeTrigger = request.SkipBattleRuleBeforeTrigger;

        var damage = DamageContextBuilder.ApplyPrePipelineModifiers(target, skillCard, request.BaseDamage);

        TryTriggerEliteGuide(target);

        if (damage <= 0)
        {
            return 0;
        }

        if (target._monsterConfig.MonsterQuality != MonsterQuality.Ordinary)
        {
            pos = target.transform.position + new Vector3(0, 1.5f, 0);
        }

        if (IsInvincible(target))
        {
            return 0;
        }

        var ctx = target.damageContext;
        ctx.TemplateExecuteThreshold = 0f;
        ctx.TemplateExecuteBuffId = 0;
        if (request.SkillContext != null)
        {
            BuffApplier.PrepareTemplateExecuteThreshold(request.SkillContext, target, ctx);
        }

        var damageResult = DamageContextBuilder.CalculateOutgoing(
            ctx,
            target,
            damageType,
            damage,
            skillCard,
            soldierCard,
            pipelineType,
            skipBattleRuleBeforeTrigger);
        target.NotifyMechanicsTakeDamage(ref ctx);
        ApplyExecuteDamageOverride(target, ctx);
        damageResult.FinalDamage = ctx.IsMiss ? 0f : ctx.FinalDamage;
        damageResult.Context = ctx;
        var attachedMagicDamage = ctx.AttachedMagicDamage;
        var attachedPhysicalDamage = ctx.AttachedPhysicalDamage;

        var applied = target.TakeDamage(damageResult, new DamageApplyOptions
        {
            HitPosition = pos,
            DamageType = damageType,
            Skill = skillCard,
            IsAgainDamage = isAgainDamage
        });

        if (applied > 0
            && !isAgainDamage
            && skillCard != null
            && KaLinBuffCombatSupport.HasKaLinBuff(target))
        {
            KaLinBuffCombatSupport.TryApplyDuplicateDamage(
                target,
                damageType,
                damageResult,
                skillCard,
                pos,
                soldierCard);

            if (!suppressKaLinCooldown)
            {
                LogTool.Log(
                    $"[KaLinBuff] 单段伤害命中后触发 CD 修正: hitSkillId={skillCard.Id}, monster={target.uniqueId}");
                KaLinBuffCombatSupport.TryApplyCooldownReduction(skillCard, soldierCard);
            }
        }

        ProcessAttachedMagicDamage(
            target,
            attachedMagicDamage,
            damageType,
            skillCard,
            pos,
            soldierCard,
            pipelineType,
            isAgainDamage,
            skipBattleRuleBeforeTrigger);

        ProcessAttachedPhysicalDamage(
            target,
            attachedPhysicalDamage,
            damageType,
            skillCard,
            pos,
            soldierCard,
            pipelineType,
            isAgainDamage,
            skipBattleRuleBeforeTrigger);

        return applied;
    }

    private static void TryTriggerEliteGuide(MonsterCard target)
    {
        if (target._monsterConfig.MonsterQuality != MonsterQuality.Boss
            && target._monsterConfig.MonsterQuality != MonsterQuality.Elite)
        {
            return;
        }

        if (target.transform.position.y >= 4.5f)
        {
            return;
        }

        GuideManager.Instance.SetGuideNode(
            GuideType.BreakingToughnessWordTip,
            target.Healtheffect.transform.Find("health").gameObject);
        GuideManager.Instance.SetGuideNode(
            GuideType.WeaknessWordTip,
            target.Healtheffect.transform.Find("health").gameObject);
        GamePlayManager.Default.TriggerGuide(GuideType.BreakingToughnessWordTip);
        LogTool.L("击中了屏幕中的精英");
    }

    private static void ProcessAttachedMagicDamage(
        MonsterCard target,
        float attachedMagicDamage,
        TakeDamageType damageType,
        ISkill skillCard,
        Vector3 pos,
        SoldierCard soldierCard,
        DamagePipelineType pipelineType,
        bool isAgainDamage,
        bool skipBattleRuleBeforeTrigger)
    {
        if (attachedMagicDamage <= 0f
            || damageType == TakeDamageType.MagicDamage
            || skillCard == null
            || skipBattleRuleBeforeTrigger)
        {
            return;
        }

        var magicDamage = Mathf.CeilToInt(attachedMagicDamage);
        if (magicDamage <= 0)
        {
            return;
        }

        var magicRequest = MonsterDamageRequest.Single(
            target,
            TakeDamageType.MagicDamage,
            magicDamage,
            skillCard,
            pos,
            soldierCard,
            pipelineType,
            isAgainDamage: true,
            skipBattleRuleBeforeTrigger: true);
        ReceiveSingleDamage(in magicRequest);

        if (skillCard.Parent != null)
        {
            skillCard.Parent.TotalDamage += magicDamage;
            Global.TotalDamage.Value += magicDamage;
        }
    }

    /// <summary>非物理主伤害时，将规则追加的物理伤害独立结算（白字飘字）。</summary>
    private static void ProcessAttachedPhysicalDamage(
        MonsterCard target,
        float attachedPhysicalDamage,
        TakeDamageType damageType,
        ISkill skillCard,
        Vector3 pos,
        SoldierCard soldierCard,
        DamagePipelineType pipelineType,
        bool isAgainDamage,
        bool skipBattleRuleBeforeTrigger)
    {
        if (attachedPhysicalDamage <= 0f
            || damageType == TakeDamageType.PhysicalDamage
            || skillCard == null
            || skipBattleRuleBeforeTrigger)
        {
            return;
        }

        var physicalDamage = Mathf.CeilToInt(attachedPhysicalDamage);
        if (physicalDamage <= 0)
        {
            return;
        }

        var physicalRequest = MonsterDamageRequest.Single(
            target,
            TakeDamageType.PhysicalDamage,
            physicalDamage,
            skillCard,
            pos,
            soldierCard,
            pipelineType,
            isAgainDamage: true,
            skipBattleRuleBeforeTrigger: true);
        ReceiveSingleDamage(in physicalRequest);

        if (skillCard.Parent != null)
        {
            skillCard.Parent.TotalDamage += physicalDamage;
            Global.TotalDamage.Value += physicalDamage;
        }
    }

    /// <summary>
    /// 受击前血量 ≤ 斩杀线时，将本段伤害升格为斩杀伤害（99999），不走单独击杀判定。
    /// </summary>
    private static void ApplyExecuteDamageOverride(MonsterCard target, DamageContext ctx)
    {
        if (ctx.IsMiss)
        {
            return;
        }

        if (target._monsterConfig.MonsterQuality == MonsterQuality.Boss)
        {
            return;
        }

        int hpForThreshold = ctx.PreHitMonsterHp > 0 ? ctx.PreHitMonsterHp : target.hp;
        if (hpForThreshold <= 0)
        {
            return;
        }

        bool moduleExecute = ctx.IsExecution;
        bool templateExecute = ctx.TemplateExecuteThreshold > 0f && hpForThreshold <= ctx.TemplateExecuteThreshold;
        if (!moduleExecute && !templateExecute)
        {
            if (ctx.TemplateExecuteThreshold > 0f)
            {
                BuffApplier.LogExecuteDamageJudgment("未触发", ctx, target, hpForThreshold, moduleExecute, templateExecute);
            }

            return;
        }

        ctx.FinalDamage = ExecuteDamageValue;
        ctx.TrueDamage = 0f;
        ctx.IsExecution = true;
        BuffApplier.LogExecuteDamageJudgment("触发", ctx, target, hpForThreshold, moduleExecute, templateExecute);
    }
}
