using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Buff;
using FusionContinent.Skill.Handler;
using FusionContinent.Utils;
using Manager;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace Game.Battle
{
    /// <summary>
    /// 战斗规则效果组分发器：按 EffectGroupsType 路由 EffectParamStruct。
    /// <para>
    /// 两条入口：
    /// - ApplyForDamagePipeline：OnDamageBefore / OnAttackBefore / OnSkillBefore / OnCriticalBefore
    /// - ApplyForAcquire：NoCondition / HpChange / RoundStart / 命中后（OnAttackAfter 等）
    /// </para>
    /// </summary>
    public static class BattleRuleEffectGroupsApplier
    {
        /// <summary>
        /// 伤害管线入口。仅支持 AddADDamage / AddAPDamage / AddRealDamage、ModifyAttr（即时）、AddBuff。
        /// </summary>
        public static void ApplyForDamagePipeline(
            DamageContext damageContext,
            BattleRuleTriggerContext ruleContext,
            IReadOnlyList<EffectParamStruct> effects,
            SkillRuleConfig ruleConfig,
            int persistToAttributeSet)
        {
            if (damageContext == null || effects == null || effects.Count == 0)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                ApplyForDamagePipeline(
                    damageContext, ruleContext, effects[i], ruleConfig, persistToAttributeSet);
            }
        }

        /// <summary>兼容旧调用：无规则配置时不写入 AttributeSet 型 ModifyAttr。</summary>
        public static void ApplyForDamagePipeline(
            DamageContext damageContext,
            BattleRuleTriggerContext ruleContext,
            IReadOnlyList<EffectParamStruct> effects)
        {
            ApplyForDamagePipeline(damageContext, ruleContext, effects, null, 0);
        }

        /// <summary>
        /// 获得/命中管线入口。支持全部 EffectGroupsType（ModifyAttr 受 allowModifyAttr 约束）。
        /// </summary>
        /// <param name="targetSoldier">非 null 时仅作用于该英灵（新召唤补同步）；null 为全场逻辑</param>
        public static void ApplyForAcquire(
            BattleRuleTriggerContext ruleContext,
            TriggerType ruleTrigger,
            IReadOnlyList<EffectParamStruct> effects,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard targetSoldier)
        {
            ApplyForAcquire(
                ruleContext, ruleTrigger, effects, sourceKey, sourceType, targetSoldier, null, false, 0);
        }

        public static void ApplyForAcquire(
            BattleRuleTriggerContext ruleContext,
            TriggerType ruleTrigger,
            IReadOnlyList<EffectParamStruct> effects,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard targetSoldier,
            SkillRuleConfig ruleConfig,
            bool useAdditiveModifierStack,
            int persistToAttributeSet)
        {
            if (effects == null || effects.Count == 0)
            {
                return;
            }

            // RoundStart：若效果组仍含 ModifyAttr（非 AddBuff）须放行
            var allowModifyAttr = ruleTrigger == TriggerType.NoCondition
                || ruleTrigger == TriggerType.HpChange
                || ruleTrigger == TriggerType.RoundStart
                || BattleRuleNewSoldierSyncSupport.AllowsOnDamageAfterModifyAttr(ruleConfig);
            if (ruleConfig != null
                && BattleRuleNewSoldierSyncSupport.UsesAdditiveModifierStack(ruleConfig))
            {
                useAdditiveModifierStack = true;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                ApplyForAcquire(
                    ruleContext,
                    effects[i],
                    sourceKey,
                    sourceType,
                    targetSoldier,
                    allowModifyAttr,
                    useAdditiveModifierStack,
                    persistToAttributeSet);
            }
        }

        private static void ApplyForDamagePipeline(
            DamageContext damageContext,
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect,
            SkillRuleConfig ruleConfig,
            int persistToAttributeSet)
        {
            var flowEnabled = DamagePipelineFlowLogger.IsEnabled;
            var before = flowEnabled ? DamagePipelineFlowLogger.Capture(damageContext) : default;
            var ruleId = ruleConfig != null ? ruleConfig.Id : 0;
            var trigger = ruleContext != null ? ruleContext.Trigger : default;
            var attrFinalBefore = float.NaN;
            var attrFinalAfter = float.NaN;

            if (flowEnabled
                && effect != null
                && effect.EffectType == EffectGroupsType.ModifyAttr
                && damageContext != null
                && damageContext.Soldier != null)
            {
                attrFinalBefore = damageContext.Soldier.BaseAttributes.GetFinal(effect.AttributeType);
            }

            switch (effect.EffectType)
            {
                case EffectGroupsType.AddADDamage:
                case EffectGroupsType.AddAPDamage:
                case EffectGroupsType.AddRealDamage:
                    BattleRuleEffectApplier.ApplyAddDamageEffect(damageContext, effect);
                    break;

                case EffectGroupsType.ModifyAttr:
                    ApplyModifyAttrInDamagePipeline(
                        damageContext, ruleContext, effect, ruleConfig, persistToAttributeSet);
                    break;

                case EffectGroupsType.AddBuff:
                    BattleRuleLearnBuffEffectApplier.TryApply(effect, ruleContext);
                    break;

                default:
                    LogTool.LogWarning(
                        $"[BattleRuleEffectGroupsApplier] 伤害管线不支持的效果类型: {effect.EffectType}");
                    break;
            }

            if (flowEnabled)
            {
                if (!float.IsNaN(attrFinalBefore)
                    && damageContext != null
                    && damageContext.Soldier != null)
                {
                    attrFinalAfter = damageContext.Soldier.BaseAttributes.GetFinal(effect.AttributeType);
                }

                DamagePipelineFlowLogger.LogRuleEffect(
                    ruleId,
                    trigger,
                    effect,
                    before,
                    DamagePipelineFlowLogger.Capture(damageContext),
                    persistToAttributeSet,
                    attrFinalBefore,
                    attrFinalAfter);
            }
        }

        private static void ApplyForAcquire(
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard targetSoldier,
            bool allowModifyAttr,
            bool useAdditiveModifierStack,
            int persistToAttributeSet)
        {
            switch (effect.EffectType)
            {
                case EffectGroupsType.AddBuff:
                    BattleRuleLearnBuffEffectApplier.TryApply(effect, ruleContext);
                    break;

                case EffectGroupsType.RemoveBuff:
                    BattleRuleRemoveBuffApplier.TryApply(effect, ruleContext);
                    break;

                case EffectGroupsType.CastSkill:
                    ApplyCastSkill(ruleContext, effect);
                    break;

                case EffectGroupsType.ReduceCoolDown:
                    ApplyReduceCoolDown(ruleContext, effect);
                    break;

                case EffectGroupsType.ResetCoolDown:
                    ApplyResetCoolDown(effect);
                    break;

                case EffectGroupsType.ModifyAttr:
                    if (allowModifyAttr && persistToAttributeSet == 0)
                    {
                        ApplyModifyAttrForAcquire(
                            ruleContext, effect, sourceKey, sourceType, targetSoldier, useAdditiveModifierStack);
                    }
                    break;

                case EffectGroupsType.ContinuousAttack:
                    ApplyContinuousAttack(ruleContext, effect);
                    break;

                default:
                    LogTool.LogWarning(
                        $"[BattleRuleEffectGroupsApplier] 获得/命中规则不支持的效果类型: {effect.EffectType}");
                    break;
            }
        }

        /// <summary>
        /// 伤害管线中的 ModifyAttr：穿透/附加魔法等单次命中属性写 DamageContext；
        /// 英灵/技能/模块属性写 AttributeSet，由 GetFinal 统一结算。
        /// </summary>
        private static void ApplyModifyAttrInDamagePipeline(
            DamageContext damageContext,
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect,
            SkillRuleConfig ruleConfig,
            int persistToAttributeSet)
        {
            if (BattleRuleMustCriticalSupport.TryApplyMustCritical(damageContext, effect))
            {
                return;
            }

            // 即时治疗不写 AttributeSet（与 AttributeManager / Acquire 管线一致）
            var healTarget = damageContext != null ? damageContext.Soldier : null;
            if (BattleRuleInstantHealSupport.TryApply(ruleContext, effect, healTarget))
            {
                return;
            }

            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(effect.AttributeType);
            if (attrCfg == null)
            {
                LogTool.LogWarning(
                    $"[BattleRuleEffectGroupsApplier] 属性配置不存在: {effect.AttributeType}");
                return;
            }

            if (BattleRuleEffectApplier.IsImmediateDamageAttribute(attrCfg.Category, effect.AttributeType))
            {
                BattleRuleEffectApplier.ApplyModifyAttrToDamageContext(damageContext, effect, ruleContext);
                return;
            }

            if (attrCfg.Category == AttributeCategoryType.Module)
            {
                ApplyModuleModifyAttrInDamagePipeline(ruleContext, effect, damageContext.Soldier);
                return;
            }

            // PersistToAttributeSet: 1=临时(仅当次 DamageContext)，0=持久(AttributeSet)
            if (persistToAttributeSet == 1)
            {
                BattleRuleEffectApplier.ApplyModifyAttrToDamageContext(damageContext, effect, ruleContext);
                return;
            }

            if (ruleConfig == null)
            {
                LogTool.LogWarning(
                    $"[BattleRuleEffectGroupsApplier] 伤害管线 ModifyAttr 缺少规则配置，已跳过 AttributeSet 写入: {effect.AttributeType}");
                return;
            }

            ApplySoldierOrSkillModifyAttrInDamagePipeline(
                damageContext, effect, ruleConfig, attrCfg.Category);
        }

        private static void ApplySoldierOrSkillModifyAttrInDamagePipeline(
            DamageContext damageContext,
            EffectParamStruct effect,
            SkillRuleConfig ruleConfig,
            AttributeCategoryType category)
        {
            var soldier = damageContext.Soldier;
            if (soldier == null || soldier.isDie)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] 伤害管线 ModifyAttr: 未找到英灵");
                return;
            }

            var sourceKey = BattleRuleNewSoldierSyncSupport.BuildDamagePipelineSourceKey(ruleConfig.Id);
            var sourceType = AttributeSourceType.SchoolCard;
            var useAdditiveStack = BattleRuleNewSoldierSyncSupport.UsesAdditiveModifierStack(ruleConfig);

            if (category == AttributeCategoryType.Skill)
            {
                var skillId = damageContext.SkillId;
                if (skillId <= 0)
                {
                    LogTool.LogWarning(
                        $"[BattleRuleEffectGroupsApplier] 技能属性 ModifyAttr 缺少 SkillId: {effect.AttributeType}");
                    return;
                }

                var skillCard = soldier.GetSkillById(skillId);
                if (skillCard == null)
                {
                    LogTool.LogWarning(
                        $"[BattleRuleEffectGroupsApplier] 技能属性 ModifyAttr 未找到技能: skillId={skillId}, attr={effect.AttributeType}");
                    return;
                }

                BattleRuleAcquireSoldierEffectApplier.ApplyEffectToAttributeSet(
                    skillCard.Attributes,
                    effect,
                    sourceKey,
                    sourceType,
                    soldier,
                    useAdditiveStack);
                return;
            }

            BattleRuleAcquireSoldierEffectApplier.ApplyEffectToSoldier(
                effect, sourceKey, sourceType, soldier, useAdditiveStack);
        }

        /// <summary>
        /// 获得/持久 ModifyAttr：模块属性写 ModuleAttributes，英灵属性写 BaseAttributes。
        /// SourceAttrType 为技能类属性时，从学习该规则的技能实例（StackSourceKey 解析）读取按等级数值。
        /// </summary>
        private static void ApplyModifyAttrForAcquire(
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard targetSoldier,
            bool useAdditiveModifierStack)
        {
            if (BattleRuleObtainDamageForLossHpSupport.TryApply(
                    ruleContext, effect, sourceKey, sourceType, targetSoldier))
            {
                return;
            }

            if (BattleRuleInstantHealSupport.TryApply(ruleContext, effect, targetSoldier))
            {
                return;
            }

            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(effect.AttributeType);
            if (attrCfg == null)
            {
                LogTool.LogWarning(
                    $"[BattleRuleEffectGroupsApplier] 属性配置不存在: {effect.AttributeType}");
                return;
            }

            if (attrCfg.Category == AttributeCategoryType.Module)
            {
                ApplyModuleModifyAttrForAcquire(effect, sourceKey, sourceType, targetSoldier);
                return;
            }

            if (attrCfg.Category != AttributeCategoryType.Soldier
                && attrCfg.Category != AttributeCategoryType.Skill)
            {
                LogTool.LogWarning(
                    $"[BattleRuleEffectGroupsApplier] 直接触发仅支持英灵/技能/模块属性: {effect.AttributeType}");
                return;
            }

            FusionContinent.Skill.ISkill sourceSkill = null;
            if (BattleRuleEffectParamUtil.UsesSkillSourceAttribute(effect))
            {
                sourceSkill = BattleRuleSkillSourceSupport.ResolveSourceSkill(ruleContext);
            }

            if (targetSoldier != null)
            {
                BattleRuleAcquireSoldierEffectApplier.ApplyEffectToSoldier(
                    effect, sourceKey, sourceType, targetSoldier, useAdditiveModifierStack, sourceSkill, ruleContext);
                return;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] ModifyAttr: GamePlayManager 不可用");
                return;
            }

            gamePlay.TraverseVillageSoldier(hero =>
            {
                if (!hero.isDie)
                {
                    BattleRuleAcquireSoldierEffectApplier.ApplyEffectToSoldier(
                        effect, sourceKey, sourceType, hero, useAdditiveModifierStack, sourceSkill, ruleContext);
                }
            });
        }

        private static void ApplyModuleModifyAttrInDamagePipeline(
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect,
            SoldierCard soldier)
        {
            if (effect.AttributeType == AttributeType.LockAtkInterval
                || effect.AttributeType == AttributeType.LockAtkSpeed)
            {
                var sourceKey = $"BattleRule_{ruleContext.Trigger}_{ruleContext.OwnerId}";
                var modifier = BattleRuleEffectParamUtil.BuildModifier(
                    effect, sourceKey, AttributeSourceType.SchoolCard, soldier);
                GamePlayManager.Default.ModuleAttributes
                    .GetAttr(effect.AttributeType)
                    .AddModifier(modifier, false);
                if (effect.AttributeType == AttributeType.LockAtkInterval)
                {
                    AttributeManager.ApplyLockAtkInterval();
                }
                else
                {
                    AttributeManager.ApplyLockAtkSpeed();
                }
                return;
            }

            var moduleSourceKey = $"BattleRule_{ruleContext.Trigger}_{ruleContext.OwnerId}";
            var moduleModifier = BattleRuleEffectParamUtil.BuildModifier(
                effect, moduleSourceKey, AttributeSourceType.SchoolCard, soldier);
            GamePlayManager.Default.ModuleAttributes
                .GetAttr(effect.AttributeType)
                .AddModifier(moduleModifier, false);
        }

        private static void ApplyModuleModifyAttrForAcquire(
            EffectParamStruct effect,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard targetSoldier)
        {
            if (effect.AttributeType == AttributeType.LockAtkInterval
                || effect.AttributeType == AttributeType.LockAtkSpeed)
            {
                // 模块级锁定：全场只生效一次；逐英灵补同步时跳过
                if (targetSoldier != null)
                {
                    return;
                }

                var modifier = BattleRuleEffectParamUtil.BuildModifier(
                    effect, sourceKey, sourceType,
                    BattleRuleSoldierHealthConditionValues.ResolveRepresentativeSoldier());
                GamePlayManager.Default.ModuleAttributes
                    .GetAttr(effect.AttributeType)
                    .AddModifier(modifier, false);
                if (effect.AttributeType == AttributeType.LockAtkInterval)
                {
                    AttributeManager.ApplyLockAtkInterval();
                }
                else
                {
                    AttributeManager.ApplyLockAtkSpeed();
                }
                return;
            }

            if (targetSoldier != null)
            {
                return;
            }

            var moduleModifier = BattleRuleEffectParamUtil.BuildModifier(
                effect, sourceKey, sourceType,
                BattleRuleSoldierHealthConditionValues.ResolveRepresentativeSoldier());
            GamePlayManager.Default.ModuleAttributes
                .GetAttr(effect.AttributeType)
                .AddModifier(moduleModifier, false);
        }

        /// <summary>EffectGroupsType.CastSkill：BaseValue = 技能 Id；规则施法走 SkillManager，不要求英灵已学习该技能。</summary>
        private static void ApplyCastSkill(BattleRuleTriggerContext ruleContext, EffectParamStruct effect)
        {
            var skillId = (int)effect.BaseValue;
            if (skillId <= 0)
            {
                LogTool.LogError($"[BattleRuleEffectGroupsApplier] CastSkill 无效技能 Id: {effect.BaseValue}");
                return;
            }

            var soldiers = BattleRuleEffectTargets.ResolveTargetSoldiers(ruleContext);
            for (int i = 0; i < soldiers.Count; i++)
            {
                CastRuleSkillAtPosition(soldiers[i], skillId, ruleContext).Forget();
            }
        }

        private static async UniTaskVoid CastRuleSkillAtPosition(
            SoldierCard hero,
            int skillId,
            BattleRuleTriggerContext ruleContext)
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null || hero == null || hero.isDie)
            {
                return;
            }

            if (SkillManager.instance == null)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] CastSkill: SkillManager 不可用");
                return;
            }

            await gamePlay._skillConfigManager.SkillConfigInsert(skillId);
            var castPosition = ResolveRuleCastPosition(ruleContext, hero);
            await SkillManager.instance.CastSkill(skillId, hero, castPosition);
            LogTool.Log(
                $"[BattleRuleEffectGroupsApplier] CastSkill: skill={skillId}, hero={hero.cardId}, pos={castPosition}");
        }

        private static Vector3 ResolveRuleCastPosition(BattleRuleTriggerContext ruleContext, SoldierCard hero)
        {
            if (ruleContext != null && ruleContext.HitPosition != Vector3.zero)
            {
                return ruleContext.HitPosition;
            }

            var monster = ruleContext?.DamageContext?.Monster ?? ruleContext?.Target as MonsterCard;
            if (monster != null)
            {
                return monster.transform.position;
            }

            if (ruleContext != null && ruleContext.CastDirection != Vector3.zero)
            {
                return hero.transform.position + ruleContext.CastDirection;
            }

            return hero.transform.position + hero.transform.forward;
        }

        /// <summary>
        /// EffectGroupsType.ReduceCoolDown：BaseValue = 修正秒数（负值缩短剩余 CD，正值延长）。
        /// 支持 SourceAttrType × PreValue（如武器 SkillCdTime × -1 清空冷却）。
        /// BaseValue≈0 且无来源秒数时，重置 DamageContext.SkillId 的冷却。
        /// 纯秒数修正（无来源属性换算）作用于目标英灵技能栏上正在冷却的技能。
        /// </summary>
        private static void ApplyReduceCoolDown(BattleRuleTriggerContext ruleContext, EffectParamStruct effect)
        {
            var adjustSeconds = ResolveReduceCoolDownSeconds(ruleConfig: null, ruleContext, effect);
            if (Mathf.Approximately(adjustSeconds, 0f))
            {
                var skillIdFromContext = ruleContext != null && ruleContext.DamageContext != null
                    ? ruleContext.DamageContext.SkillId
                    : 0;
                if (skillIdFromContext > 0)
                {
                    var gameLandForReset = GameLand.Default;
                    if (gameLandForReset != null)
                    {
                        gameLandForReset.ReduceSkillCooling(skillIdFromContext, float.MaxValue);
                    }

                    return;
                }

                return;
            }

            var gameLand = GameLand.Default;
            if (gameLand == null)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] ReduceCoolDown: GameLand 不可用");
                return;
            }

            // BaseValue 为正整数且非来源换算时，视为指定技能 Id。
            var configuredSkillId = (int)effect.BaseValue;
            if (configuredSkillId > 0 && Mathf.Approximately(effect.BaseValue, configuredSkillId)
                && !BattleRuleEffectParamUtil.UsesSkillSourceAttribute(effect)
                && !BattleRuleEffectParamUtil.HasPercentValue(effect))
            {
                ApplyReduceCoolDownForSkillCard(gameLand, configuredSkillId, adjustSeconds);
                return;
            }

            // 来源属性 × PreValue（如武器 SkillCdTime × -1）：只修正上下文技能。
            if (BattleRuleEffectParamUtil.HasPercentValue(effect)
                && BattleRuleEffectParamUtil.HasSourceAttribute(effect))
            {
                var contextSkillId = ruleContext != null && ruleContext.DamageContext != null
                    ? ruleContext.DamageContext.SkillId
                    : 0;
                if (contextSkillId > 0)
                {
                    ApplyReduceCoolDownForSkillCard(gameLand, contextSkillId, adjustSeconds);
                    return;
                }
            }

            // 纯秒数修正（如 206001 BaseValue=-1）：降低目标英灵栏上所有冷却中技能。
            var soldiers = BattleRuleEffectTargets.ResolveTargetSoldiers(ruleContext);
            for (int i = 0; i < soldiers.Count; i++)
            {
                ApplyReduceCoolDownToSoldierSkillBar(soldiers[i], adjustSeconds, gameLand);
            }
        }

        private static float ResolveReduceCoolDownSeconds(
            SkillRuleConfig ruleConfig,
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect)
        {
            var seconds = effect.BaseValue;
            if (BattleRuleEffectParamUtil.HasPercentValue(effect)
                && BattleRuleEffectParamUtil.HasSourceAttribute(effect))
            {
                var sourceValue = BattleRuleEffectParamUtil.ResolveSourceAttributeValue(
                    effect, null, BattleRuleSkillSourceSupport.ResolveSourceSkill(ruleContext), ruleContext);
                seconds += sourceValue * effect.PreValue;
            }

            return seconds;
        }

        private static void ApplyReduceCoolDownForSkillCard(GameLand gameLand, int skillId, float adjustSeconds)
        {
            if (adjustSeconds < 0f)
            {
                gameLand.ReduceSkillCooling(skillId, -adjustSeconds);
                return;
            }

            gameLand.AddSkillCurrentCooldownTime(skillId, adjustSeconds);
        }

        /// <summary>缩短/延长该英灵技能栏上正在冷却中的技能卡（与 ReduceSoldierAllSkillsCooling 一致）。</summary>
        private static void ApplyReduceCoolDownToSoldierSkillBar(
            SoldierCard soldier,
            float adjustSeconds,
            GameLand gameLand)
        {
            if (adjustSeconds < 0f)
            {
                gameLand.ReduceSoldierAllSkillsCooling(soldier.cardId, -adjustSeconds);
                return;
            }

            foreach (var card in Global.SoldierSkillTeams)
            {
                if (card.CardId == 0 || card.CardType != CardType.Skill)
                {
                    continue;
                }

                var specialCardCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(card.CardId);
                if (specialCardCfg == null)
                {
                    continue;
                }

                if (specialCardCfg.ActsOnCard != 0 && specialCardCfg.ActsOnCard != soldier.cardId)
                {
                    continue;
                }

                gameLand.AddSkillCurrentCooldownTime(card.CardId, adjustSeconds);
            }
        }

        /// <summary>
        /// EffectGroupsType.ResetCoolDown：BaseValue = 技能 Id；0 表示重置所有冷却中技能。
        /// </summary>
        private static void ApplyResetCoolDown(EffectParamStruct effect)
        {
            var gameLand = GameLand.Default;
            if (gameLand == null)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] ResetCoolDown: GameLand 不可用");
                return;
            }

            var skillId = (int)effect.BaseValue;
            if (skillId > 0)
            {
                gameLand.AddSkillCurrentCooldownTime(skillId, -999f);
                return;
            }

            foreach (var pair in gameLand.skill)
            {
                if (pair.Value.Cool)
                {
                    gameLand.AddSkillCurrentCooldownTime(pair.Key, -999f);
                }
            }
        }

        /// <summary>
        /// 施法时刻效果（连击/CastSkill 等）：由 BattleRuleCastSupport 在 UseSkill 时调用。
        /// </summary>
        public static void ApplyCastEffect(
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect,
            SkillRuleConfig ruleConfig,
            int persistToAttributeSet)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.RuleExecute))
            {
            switch (effect.EffectType)
            {
                case EffectGroupsType.ContinuousAttack:
                    ApplyContinuousAttack(ruleContext, effect);
                    break;

                case EffectGroupsType.AddSkillNoBuff:
                    ApplyAddSkillNoBuff(ruleContext, effect);
                    break;

                case EffectGroupsType.ModifyAttr:
                    ApplyCastTimeModifyAttr(ruleContext, effect, ruleConfig, persistToAttributeSet);
                    break;

                case EffectGroupsType.CastSkill:
                    ApplyCastSkill(ruleContext, effect);
                    break;

                case EffectGroupsType.ReduceCoolDown:
                    ApplyReduceCoolDown(ruleContext, effect);
                    break;

                case EffectGroupsType.ResetCoolDown:
                    ApplyResetCoolDown(effect);
                    break;

                default:
                    break;
            }
            }
        }

        /// <summary>
        /// EffectGroupsType.ContinuousAttack：延迟连击。
        /// PreValue = 连击次数（优先）；BaseValue = 技能 Id（0 则用上下文技能或普攻）。
        /// </summary>
        private static void ApplyContinuousAttack(BattleRuleTriggerContext ruleContext, EffectParamStruct effect)
        {
            var soldiers = BattleRuleEffectTargets.ResolveTargetSoldiers(ruleContext);
            if (soldiers.Count == 0)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] ContinuousAttack: 未找到施法英灵");
                return;
            }

            var attackCount = Mathf.Max(1, Mathf.RoundToInt(
                !Mathf.Approximately(effect.PreValue, 0f) ? effect.PreValue : effect.BaseValue));
            if (attackCount <= 0)
            {
                attackCount = 1;
            }

            for (int i = 0; i < soldiers.Count; i++)
            {
                var hero = soldiers[i];
                var skillId = ResolveContinuousAttackSkillId(ruleContext, hero, effect);
                var direction = ResolveCastDirection(ruleContext, hero);
                DelayContinuousAttack(hero, skillId, direction, attackCount).Forget();
            }
        }

        /// <summary>每次连击间隔 100ms，skipBeforeTriggerEvent 避免递归触发 Before 规则。</summary>
        private static async UniTaskVoid DelayContinuousAttack(
            SoldierCard soldier,
            int skillId,
            Vector3 direction,
            int attackCount)
        {
            for (int i = 0; i < attackCount; i++)
            {
                await UniTask.Delay(100);
                if (soldier == null || soldier.isDie)
                {
                    return;
                }

                soldier.UseSkill(skillId, direction, skipBeforeTriggerEvent: true);
                LogTool.Log(
                    $"[BattleRuleEffectGroupsApplier] ContinuousAttack: skill={skillId}, hero={soldier.cardId}, index={i + 1}");
            }
        }

        /// <summary>
        /// EffectGroupsType.AddSkillNoBuff：重复释放当前主动技能（跳过增益节点）。
        /// PreValue = 重复次数（优先）；BaseValue = 重复次数（PreValue 为 0 时使用）。
        /// </summary>
        private static void ApplyAddSkillNoBuff(BattleRuleTriggerContext ruleContext, EffectParamStruct effect)
        {
            var soldiers = BattleRuleEffectTargets.ResolveTargetSoldiers(ruleContext);
            if (soldiers.Count == 0)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] AddSkillNoBuff: 未找到施法英灵");
                return;
            }

            var skillId = ruleContext.DamageContext?.SkillId ?? 0;
            if (skillId <= 0)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] AddSkillNoBuff: 上下文技能 Id 无效");
                return;
            }

            var repeatCount = Mathf.Max(1, Mathf.RoundToInt(
                !Mathf.Approximately(effect.PreValue, 0f) ? effect.PreValue : effect.BaseValue));
            if (repeatCount <= 0)
            {
                repeatCount = 1;
            }

            ruleContext.CastRepeatCount += repeatCount;
        }

        /// <summary>
        /// 全部 OnSkillBefore 规则结算后：统一注册减伤会话并调度重复施法，避免多条规则互相覆盖次数。
        /// </summary>
        public static void FinalizeSkillCastRepeats(BattleRuleTriggerContext ruleContext)
        {
            if (ruleContext == null)
            {
                return;
            }

            var skillId = ruleContext.DamageContext?.SkillId ?? 0;
            if (skillId <= 0)
            {
                return;
            }

            var soldiers = BattleRuleEffectTargets.ResolveTargetSoldiers(ruleContext);
            if (soldiers.Count == 0)
            {
                return;
            }

            int totalCasts = 1 + ruleContext.CastRepeatCount;
            if (ruleContext.PendingCastDamageEffect != null)
            {
                for (int i = 0; i < soldiers.Count; i++)
                {
                    BattleRuleCastDamageSession.Register(
                        soldiers[i],
                        skillId,
                        ruleContext.PendingCastDamageEffect,
                        totalCasts);
                }
            }

            if (ruleContext.CastRepeatCount > 0)
            {
                for (int i = 0; i < soldiers.Count; i++)
                {
                    var hero = soldiers[i];
                    var castTarget = ResolveCastDirection(ruleContext, hero);
                    DelayAddSkillNoBuff(hero, skillId, castTarget, ruleContext.CastRepeatCount).Forget();
                }
            }

            ruleContext.CastRepeatCount = 0;
            ruleContext.PendingCastDamageEffect = null;
        }

        /// <summary>每次重复间隔 100ms，skipBeforeTriggerEvent 避免递归触发 Before 规则。</summary>
        private static async UniTaskVoid DelayAddSkillNoBuff(
            SoldierCard soldier,
            int skillId,
            Vector3 castTarget,
            int repeatCount)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                await UniTask.Delay(100);
                if (soldier == null || soldier.isDie)
                {
                    return;
                }

                soldier.UseSkill(
                    skillId,
                    castTarget,
                    skipBeforeTriggerEvent: true,
                    skipBuffOnCast: true,
                    useSnapshotCastTarget: true);
                LogTool.Log(
                    $"[BattleRuleEffectGroupsApplier] AddSkillNoBuff: skill={skillId}, hero={soldier.cardId}, index={i + 1}");
            }
        }

        /// <summary>
        /// 施法时刻 ModifyAttr：写入临时 AttributeSet 会话，覆盖原施法 + AddSkillNoBuff 重复施放。
        /// </summary>
        private static void ApplyCastTimeModifyAttr(
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect,
            SkillRuleConfig ruleConfig,
            int persistToAttributeSet)
        {
            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(effect.AttributeType);
            if (attrCfg == null)
            {
                LogTool.LogWarning(
                    $"[BattleRuleEffectGroupsApplier] 施法时刻属性配置不存在: {effect.AttributeType}");
                return;
            }

            if (attrCfg.Category == AttributeCategoryType.Module)
            {
                LogTool.LogWarning(
                    $"[BattleRuleEffectGroupsApplier] 施法时刻不支持模块 ModifyAttr: {effect.AttributeType}");
                return;
            }

            if (attrCfg.Category != AttributeCategoryType.Soldier
                && attrCfg.Category != AttributeCategoryType.Skill
                && !BattleRuleEffectApplier.IsImmediateDamageAttribute(attrCfg.Category, effect.AttributeType))
            {
                LogTool.LogWarning(
                    $"[BattleRuleEffectGroupsApplier] 施法时刻不支持 ModifyAttr: {effect.AttributeType}");
                return;
            }

            var soldiers = BattleRuleEffectTargets.ResolveTargetSoldiers(ruleContext);
            if (soldiers.Count == 0)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] 施法时刻 ModifyAttr: 未找到施法英灵");
                return;
            }

            var skillId = ruleContext.DamageContext?.SkillId ?? 0;
            if (skillId <= 0)
            {
                LogTool.LogWarning("[BattleRuleEffectGroupsApplier] 施法时刻 ModifyAttr: 上下文技能 Id 无效");
                return;
            }

            // 1=临时：施法会话，技能结束移除；0=持久：立即写入 AttributeSet
            if (persistToAttributeSet == 1)
            {
                ruleContext.PendingCastDamageEffect = effect;
                return;
            }

            if (ruleConfig == null)
            {
                LogTool.LogWarning(
                    "[BattleRuleEffectGroupsApplier] 施法时刻持久 ModifyAttr 缺少规则配置");
                return;
            }

            var sourceKey = BattleRuleNewSoldierSyncSupport.BuildSourceKey(ruleConfig.Id);
            var sourceType = AttributeSourceType.SchoolCard;
            var useAdditiveStack = BattleRuleNewSoldierSyncSupport.UsesAdditiveModifierStack(ruleConfig);
            for (int i = 0; i < soldiers.Count; i++)
            {
                ApplyModifyAttrForAcquire(
                    ruleContext, effect, sourceKey, sourceType, soldiers[i], useAdditiveStack);
            }
        }

        /// <summary>连击技能 Id 优先级：配置 BaseValue &gt; 上下文 SkillId &gt; 普攻 Id。</summary>
        private static int ResolveContinuousAttackSkillId(
            BattleRuleTriggerContext ruleContext,
            SoldierCard hero,
            EffectParamStruct effect)
        {
            var configuredSkillId = (int)effect.BaseValue;
            if (configuredSkillId > 0)
            {
                return configuredSkillId;
            }

            var contextSkillId = ruleContext.DamageContext?.SkillId ?? 0;
            if (contextSkillId > 0)
            {
                return contextSkillId;
            }

            return hero.NormalAttackSkillId;
        }

        /// <summary>ReduceCoolDown 指定技能：BaseValue 为正时作 skillId，否则用 DamageContext.SkillId。</summary>
        private static int ResolveSkillIdFromEffect(
            EffectParamStruct effect,
            BattleRuleTriggerContext ruleContext)
        {
            var configuredSkillId = (int)effect.BaseValue;
            if (configuredSkillId > 0)
            {
                return configuredSkillId;
            }

            var contextSkillId = ruleContext?.DamageContext?.SkillId ?? 0;
            return contextSkillId;
        }

        private static Vector3 ResolveCastDirection(BattleRuleTriggerContext ruleContext, SoldierCard hero)
        {
            if (ruleContext != null && ruleContext.CastDirection != Vector3.zero)
            {
                return ruleContext.CastDirection;
            }

            var monster = ruleContext?.DamageContext?.Monster ?? ruleContext?.Target as MonsterCard;
            if (monster != null)
            {
                return (monster.transform.position - hero.transform.position).normalized;
            }

            return hero.transform.forward;
        }
    }
}
