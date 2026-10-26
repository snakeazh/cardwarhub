using Config;
using FusionContinent;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 附加物理伤害 ModifyAttr 在配表 Trigger 为命中时机时，需持久写入英灵并由 SumDamageTypes 汇总。
    /// AddMagicDamage 不走此路径，应在 OnAttackBefore + 物理普攻 DamageContext 上累加 AttachedMagicDamage。
    /// </summary>
    public static class BattleRuleAttachedDamageSupport
    {
        public static bool IsPersistentAttachedPhysicalDamage(AttributeType attributeType)
        {
            return attributeType == AttributeType.AddPhysicalDamage;
        }

        public static bool RuleHasPersistentAttachedPhysicalModify(SkillRuleConfig ruleConfig)
        {
            if (ruleConfig == null || ruleConfig.EffectGroupId == null || ruleConfig.EffectGroupId.Count == 0)
            {
                return false;
            }

            var effectTable = DataConfigManager.Tables.TbEffectGroupConfig;
            for (int i = 0; i < ruleConfig.EffectGroupId.Count; i++)
            {
                var effectGroup = effectTable.GetOrDefault(ruleConfig.EffectGroupId[i]);
                if (effectGroup?.Effects == null)
                {
                    continue;
                }

                for (int j = 0; j < effectGroup.Effects.Count; j++)
                {
                    var effect = effectGroup.Effects[j];
                    if (effect.EffectType == EffectGroupsType.ModifyAttr
                        && effectGroup.PersistToAttributeSet == 0
                        && IsPersistentAttachedPhysicalDamage(effect.AttributeType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryApplyAttachedPhysicalDamageRule(int ruleId, SoldierCard targetSoldier = null)
        {
            var ruleConfig = DataConfigManager.Tables.TbSkillRuleConfig.GetOrDefault(ruleId);
            if (ruleConfig == null || !RuleHasPersistentAttachedPhysicalModify(ruleConfig))
            {
                return false;
            }

            if (ruleConfig.Trigger == TriggerType.NoCondition)
            {
                return false;
            }

            var context = CreateAcquireContext(targetSoldier);
            var conditionManager = new BattleRuleConditionManager(DataConfigManager.Tables.TbConditionGroupConfig);
            if (!conditionManager.AreAllConditionsMet(ruleConfig.ConditionGroupId, context))
            {
                return false;
            }

            for (int i = 0; i < ruleConfig.EffectGroupId.Count; i++)
            {
                var effectGroup = DataConfigManager.Tables.TbEffectGroupConfig.GetOrDefault(ruleConfig.EffectGroupId[i]);
                if (effectGroup == null)
                {
                    continue;
                }

                BattleRuleAcquireEffectExecutor.ApplyEffectGroup(
                    ruleConfig,
                    effectGroup,
                    context,
                    targetSoldier);
            }

            var heroInfo = targetSoldier != null ? $"hero={targetSoldier.cardId}" : "全场英灵";
            LogTool.Log(
                $"[BattleRuleAttachedDamageSupport] 附加物理伤害规则已持久生效: RuleId={ruleId}, Trigger={ruleConfig.Trigger}, {heroInfo}");
            return true;
        }

        public static void ApplyLearnedAttachedPhysicalDamageRulesToSoldier(SoldierCard soldier)
        {
            if (soldier == null || soldier.isDie || soldier.RoleType == SoldierRoleType.Mirror)
            {
                return;
            }

            foreach (var ruleId in AttributeManager.BattleLearnedSkillRuleIds)
            {
                TryApplyAttachedPhysicalDamageRule(ruleId, soldier);
            }
        }

        public static void TriggerPendingAttachedPhysicalDamageRulesOnInit()
        {
            foreach (var ruleId in AttributeManager.BattleLearnedSkillRuleIds)
            {
                TryApplyAttachedPhysicalDamageRule(ruleId);
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
                CurrentTime = Time.time,
            };
        }
    }
}
