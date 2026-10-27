using Config;
using FusionContinent;
using FusionContinent.Attribute;
using Manager;

namespace Game.Battle
{
    /// <summary>
    /// 按 SkillRuleConfig.NewSoldierSync 将已生效规则同步到后召唤英灵。
    /// </summary>
    public static class BattleRuleNewSoldierSyncSupport
    {
        public static bool UsesAdditiveModifierStack(SkillRuleConfig ruleConfig)
        {
            return ruleConfig != null && ruleConfig.NewSoldierSync == SyncType.ByTriggerCount;
        }

        public static bool AllowsOnDamageAfterModifyAttr(SkillRuleConfig ruleConfig)
        {
            return UsesAdditiveModifierStack(ruleConfig);
        }

        public static string BuildSourceKey(int ruleId, string stackSourceKey = null)
        {
            if (string.IsNullOrEmpty(stackSourceKey))
            {
                return "BattleRuleAcquire_" + ruleId;
            }

            return "BattleRuleAcquire_" + ruleId + "_" + stackSourceKey;
        }

        /// <summary>伤害管线 ModifyAttr 写入 AttributeSet 时的来源 Key（按规则 Id 覆盖，避免每次命中叠乘）。</summary>
        public static string BuildDamagePipelineSourceKey(int ruleId)
        {
            return "BattleRuleDamage_" + ruleId;
        }

        public static void ApplyToSoldier(
            BattleRuleService ruleService,
            BattleRuleConditionManager conditionManager,
            TbConditionGroupConfig conditionConfigTable,
            SoldierCard soldier,
            SkillRuleConfig ruleConfig,
            string stackSourceKey = null,
            AttributeSourceType sourceType = AttributeSourceType.SchoolCard)
        {
            if (ruleService == null || soldier == null || soldier.isDie || ruleConfig == null)
            {
                return;
            }

            switch (ruleConfig.NewSoldierSync)
            {
                case SyncType.None:
                    break;
                case SyncType.Once:
                    ApplyOnce(ruleService, conditionManager, conditionConfigTable, soldier, ruleConfig);
                    break;
                case SyncType.ByTriggerCount:
                    ApplyByTriggerCount(ruleService, soldier, ruleConfig);
                    break;
                case SyncType.Reevaluate:
                    ApplyReevaluate(conditionManager, soldier, ruleConfig, stackSourceKey, sourceType);
                    break;
            }
        }

        private static void ApplyOnce(
            BattleRuleService ruleService,
            BattleRuleConditionManager conditionManager,
            TbConditionGroupConfig conditionConfigTable,
            SoldierCard soldier,
            SkillRuleConfig ruleConfig)
        {
            BattleRuleTriggerContext context;
            if (BattleRuleRosterCompositionSupport.IsGloballyEffectiveNoConditionRule(ruleConfig))
            {
                if (!BattleRuleRosterCompositionSupport.IsLineupConditionMet(
                        ruleConfig.ConditionGroupId, conditionManager))
                {
                    return;
                }

                context = BattleRuleRosterCompositionSupport.CreateLineupRuleContext();
            }
            else if (BattleRuleAttachedDamageSupport.RuleHasPersistentAttachedPhysicalModify(ruleConfig))
            {
                context = CreateAcquireContext(soldier);
                if (!conditionManager.AreAllConditionsMet(ruleConfig.ConditionGroupId, context))
                {
                    return;
                }
            }
            else
            {
                LogTool.LogWarning(
                    "[BattleRuleNewSoldierSyncSupport] Once 规则未识别同步策略: RuleId=" + ruleConfig.Id);
                return;
            }

            ApplyAllEffectGroups(ruleConfig, context, soldier, useAdditiveModifierStack: false);
            LogTool.Log(
                "[BattleRuleNewSoldierSyncSupport] Once 同步: RuleId=" + ruleConfig.Id + ", hero=" + soldier.cardId);
        }

        private static void ApplyByTriggerCount(
            BattleRuleService ruleService,
            SoldierCard soldier,
            SkillRuleConfig ruleConfig)
        {
            var triggerCount = ruleService.GetRuleTriggerCount(
                BattleRuleDamageSupport.GlobalBattleOwnerId,
                ruleConfig.Id);
            if (triggerCount <= 0)
            {
                return;
            }

            var context = CreateAcquireContext(soldier);
            context.Trigger = ruleConfig.Trigger;

            for (int stack = 0; stack < triggerCount; stack++)
            {
                ApplyAllEffectGroups(ruleConfig, context, soldier, useAdditiveModifierStack: true);
            }

            LogTool.Log(
                "[BattleRuleNewSoldierSyncSupport] ByTriggerCount 同步: RuleId=" + ruleConfig.Id
                + ", hero=" + soldier.cardId + ", stacks=" + triggerCount);
        }

        private static void ApplyReevaluate(
            BattleRuleConditionManager conditionManager,
            SoldierCard soldier,
            SkillRuleConfig ruleConfig,
            string stackSourceKey,
            AttributeSourceType sourceType)
        {
            if (ruleConfig.Trigger != TriggerType.NoCondition
                && ruleConfig.Trigger != TriggerType.HpChange)
            {
                LogTool.LogWarning(
                    "[BattleRuleNewSoldierSyncSupport] Reevaluate 仅支持 NoCondition/HpChange: RuleId="
                    + ruleConfig.Id);
                return;
            }

            var valueProvider = ruleConfig.Trigger == TriggerType.HpChange
                ? BattleRuleHpChangeValueProvider.Instance
                : BattleRuleRosterCompositionSupport.UsesLineupScopedConditions(
                    ruleConfig, DataConfigManager.Tables.TbConditionGroupConfig)
                    ? (IBattleRuleValueProvider)BattleRuleLineupValueProvider.Instance
                    : BattleRuleAcquireValueProvider.Instance;

            var context = new BattleRuleTriggerContext(ruleConfig.Trigger, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                Source = soldier,
                Target = soldier,
                ValueProvider = valueProvider,
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = UnityEngine.Time.time,
                StackSourceKey = stackSourceKey,
                ModifierSourceType = sourceType,
            };

            if (!conditionManager.AreAllConditionsMet(ruleConfig.ConditionGroupId, context))
            {
                if (ruleConfig.Trigger == TriggerType.HpChange)
                {
                    BattleRuleHpChangeModifyAttrSupport.RemoveModifyAttrEffects(ruleConfig, context);
                }

                return;
            }

            ApplyAllEffectGroups(ruleConfig, context, soldier, useAdditiveModifierStack: false);
            LogTool.Log(
                "[BattleRuleNewSoldierSyncSupport] Reevaluate 同步: RuleId=" + ruleConfig.Id
                + ", StackKey=" + stackSourceKey + ", hero=" + soldier.cardId);
        }

        /// <summary>
        /// 对单个英灵按已学 StackKey 重新判定并施加规则（装备补同步、月影祝福叠加等）。
        /// </summary>
        public static void ReevaluateRuleOnSoldier(
            BattleRuleConditionManager conditionManager,
            SoldierCard soldier,
            SkillRuleConfig ruleConfig,
            string stackSourceKey,
            AttributeSourceType sourceType)
        {
            ApplyReevaluate(conditionManager, soldier, ruleConfig, stackSourceKey, sourceType);
        }

        private static void ApplyAllEffectGroups(
            SkillRuleConfig ruleConfig,
            BattleRuleTriggerContext context,
            SoldierCard soldier,
            bool useAdditiveModifierStack)
        {
            var sourceKey = BuildSourceKey(ruleConfig.Id, context.StackSourceKey);
            var sourceType = context.ModifierSourceType != 0
                ? context.ModifierSourceType
                : AttributeSourceType.SchoolCard;
            for (int i = 0; i < ruleConfig.EffectGroupId.Count; i++)
            {
                var effectGroup = DataConfigManager.Tables.TbEffectGroupConfig.GetOrDefault(ruleConfig.EffectGroupId[i]);
                if (effectGroup == null)
                {
                    continue;
                }

                BattleRuleEffectGroupsApplier.ApplyForAcquire(
                    context,
                    ruleConfig.Trigger,
                    effectGroup.Effects,
                    sourceKey,
                    sourceType,
                    soldier,
                    ruleConfig,
                    useAdditiveModifierStack,
                    effectGroup.PersistToAttributeSet);
            }
        }

        private static BattleRuleTriggerContext CreateAcquireContext(SoldierCard soldier)
        {
            return new BattleRuleTriggerContext(TriggerType.NoCondition, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                Source = soldier,
                Target = soldier,
                ValueProvider = BattleRuleAcquireValueProvider.Instance,
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = UnityEngine.Time.time,
            };
        }
    }
}
