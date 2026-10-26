using Config;
using FusionContinent;
using Manager;
using UnityEngine;
using Unity.Profiling;

namespace Game.Battle
{
    /// <summary>
    /// 施法时刻规则触发（与伤害管线命中前规则分离）。
    /// ContinuousAttack 等应在 UseSkill 时判定一次，而非每次 TakeDamage 命中时重复触发。
    /// </summary>
    public static class BattleRuleCastSupport
    {
        /// <summary>
        /// 普攻施放前：触发 OnAttackBefore 规则中的 ContinuousAttack（双连斩等）。
        /// </summary>
        public static void TriggerNormalAttackBeforeCast(SoldierCard soldier, int skillId, Vector3 direction)
        {
            if (soldier == null || skillId <= 0)
            {
                return;
            }

            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null || AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.RuleExecute))
            {
            var damageContext = new DamageContext
            {
                SkillId = skillId,
                Soldier = soldier,
                PipelineType = DamagePipelineType.SoldierDamage,
            };

            var context = new BattleRuleTriggerContext(TriggerType.OnAttackBefore, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                Source = soldier,
                DamageContext = damageContext,
                CastDirection = direction,
                ValueProvider = BattleRuleAcquireValueProvider.Instance,
                CurrentTime = Time.time,
            };

            ruleService.TriggerLearnedBattleRulesForCastEffects(
                TriggerType.OnAttackBefore,
                context,
                EffectGroupsType.ContinuousAttack);
            }
        }

        /// <summary>
        /// 主动技能施放前：触发 OnSkillBefore 规则中的 AddSkillNoBuff（奥术回音等重复释放）。
        /// </summary>
        public static void TriggerSkillBeforeCast(SoldierCard soldier, int skillId, Vector3 direction)
        {
            if (soldier == null || skillId <= 0)
            {
                return;
            }

            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null || AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.RuleExecute))
            {
            var damageContext = new DamageContext
            {
                SkillId = skillId,
                Soldier = soldier,
                PipelineType = DamagePipelineType.SoldierDamage,
            };

            var context = new BattleRuleTriggerContext(TriggerType.OnSkillBefore, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                Source = soldier,
                DamageContext = damageContext,
                CastDirection = direction,
                ValueProvider = BattleRuleAcquireValueProvider.Instance,
                CurrentTime = Time.time,
            };

            ruleService.TriggerLearnedBattleRulesForCastEffects(
                TriggerType.OnSkillBefore,
                context,
                EffectGroupsType.AddSkillNoBuff);

            BattleRuleEffectGroupsApplier.FinalizeSkillCastRepeats(context);
            }
        }

        /// <summary>
        /// 主动技能施放完成后：触发 OnSkillUse（如武器 1101 概率清空 CD）。
        /// 在 UseSkill 成功后调用一次，避免多段/多目标命中时重复走 OnSkillAfter。
        /// </summary>
        public static void TriggerSkillAfterUse(SoldierCard soldier, int skillId, Vector3 direction)
        {
            if (soldier == null || skillId <= 0)
            {
                return;
            }

            if (skillId == soldier.NormalAttackSkillId)
            {
                return;
            }

            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null || AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.RuleExecute))
            {
                var damageContext = new DamageContext
                {
                    SkillId = skillId,
                    Soldier = soldier,
                    PipelineType = DamagePipelineType.SoldierDamage,
                };

                var context = new BattleRuleTriggerContext(TriggerType.OnSkillUse, BattleRuleDamageSupport.GlobalBattleOwnerId)
                {
                    Source = soldier,
                    DamageContext = damageContext,
                    CastDirection = direction,
                    ValueProvider = BattleRuleAcquireValueProvider.Instance,
                    EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                    CurrentTime = Time.time,
                };

                ruleService.TriggerLearnedBattleRules(TriggerType.OnSkillUse, context);
            }
        }
    }

    /// <summary>
    /// 施法时刻效果执行器：分发 ContinuousAttack / AddSkillNoBuff / 施法期 ModifyAttr / CastSkill / CD 类效果。
    /// </summary>
    public sealed class BattleRuleCastEffectExecutor : IBattleRuleEffectExecutor
    {
        public static readonly BattleRuleCastEffectExecutor Instance = new BattleRuleCastEffectExecutor();

        public void ExecuteEffect(BattleRuleEffectExecuteInfo executeInfo)
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.RuleExecute))
            {
                var effectGroup = executeInfo?.EffectGroupConfig;
                var ruleContext = executeInfo?.Context;
                if (effectGroup == null || ruleContext == null)
                {
                    return;
                }

                var effects = effectGroup.Effects;
                var ruleConfig = executeInfo.RuleConfig;
                var persistToAttributeSet = effectGroup.PersistToAttributeSet;
                for (int i = 0; i < effects.Count; i++)
                {
                    BattleRuleEffectGroupsApplier.ApplyCastEffect(
                        ruleContext, effects[i], ruleConfig, persistToAttributeSet);
                }
            }
        }
    }
}
