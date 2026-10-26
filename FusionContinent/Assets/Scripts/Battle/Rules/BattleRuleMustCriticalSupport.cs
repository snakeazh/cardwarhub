using Config;
using FusionContinent;
using FusionContinent.Skill.Handler;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 必定暴击：ModifyAttr（ThisAtk/SkillMustCritical）与「施法计数 → 下次技能必暴」。
    /// 六律·技 2205005：OnSkillCast 累加 SkillCastCount，达标后标记下一招技能必暴（当前次释放的伤害不消耗）。
    /// </summary>
    public static class BattleRuleMustCriticalSupport
    {
        public static bool TryApplyMustCritical(DamageContext damageContext, EffectParamStruct effect)
        {
            if (damageContext == null || effect == null)
            {
                return false;
            }

            if (effect.AttributeType == AttributeType.ThisAtkMustCritical)
            {
                if (!BattleRuleDamageSupport.IsNormalAttack(damageContext))
                {
                    return true;
                }

                if (effect.BaseValue > 0f)
                {
                    damageContext.IsCrit = true;
                }

                return true;
            }

            if (effect.AttributeType == AttributeType.ThisSkillMustCritical)
            {
                if (BattleRuleDamageSupport.IsNormalAttack(damageContext))
                {
                    return true;
                }

                if (effect.BaseValue > 0f)
                {
                    damageContext.IsCrit = true;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 主动技能释放后：SkillCastCount 达标则标记「下一招技能」必暴（跳过本次 skillId 的伤害段）。
        /// </summary>
        public static void OnSkillReleased(SoldierCard soldier, int skillId)
        {
            if (soldier == null || skillId <= 0 || skillId == soldier.NormalAttackSkillId)
            {
                return;
            }

            if (AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null)
            {
                return;
            }

            var context = CreateSkillCastContext(soldier, skillId);
            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            var skillRuleTable = DataConfigManager.Tables.TbSkillRuleConfig;
            var effectTable = DataConfigManager.Tables.TbEffectGroupConfig;
            var computerTable = DataConfigManager.Tables.TbComputerGroupConfig;

            for (int i = 0; i < learnedRules.Count; i++)
            {
                var ruleConfig = skillRuleTable.GetOrDefault(learnedRules[i].RuleId);
                if (ruleConfig == null
                    || ruleConfig.ComputerGroupId == null
                    || ruleConfig.ComputerGroupId.Count == 0
                    || ruleConfig.EffectGroupId == null
                    || ruleConfig.EffectGroupId.Count == 0)
                {
                    continue;
                }

                if (!RuleGrantsNextSkillMustCritical(ruleConfig.EffectGroupId, effectTable))
                {
                    continue;
                }

                if (!RuleUsesSkillCastComputer(ruleConfig.ComputerGroupId, computerTable))
                {
                    continue;
                }

                if (!ruleService.ComputerManager.AreComputerGroupsMet(ruleConfig.ComputerGroupId, context))
                {
                    continue;
                }

                soldier.MarkNextSkillMustCritical(skillId);
                ruleService.ComputerManager.ResetAfterTrigger(ruleConfig.ComputerGroupId, context);
                LogTool.Log(
                    $"[BattleRuleMustCriticalSupport] 下次技能必暴: hero={soldier.cardId}, castSkillId={skillId}, ruleId={ruleConfig.Id}");
                return;
            }
        }

        /// <summary>预掷暴击：已标记且非本次施法 skillId 时，先写入 PreRolledCrit（命中时再消耗标记）。</summary>
        public static void ApplyPendingNextSkillMustCriticalPreRoll(SoldierCard soldier, int skillId, SkillContext context)
        {
            if (soldier == null || context == null || skillId <= 0 || skillId == soldier.NormalAttackSkillId)
            {
                return;
            }

            if (!soldier.HasPendingNextSkillMustCritical(skillId))
            {
                return;
            }

            context.HasPreRolledCrit = true;
            context.PreRolledCrit = true;
        }

        /// <summary>伤害结算：消耗「下次技能必暴」并强制暴击。</summary>
        public static void TryApplyPendingNextSkillMustCritical(
            SoldierCard soldier,
            int skillId,
            DamageContext damageContext)
        {
            if (soldier == null || damageContext == null || skillId <= 0 || skillId == soldier.NormalAttackSkillId)
            {
                return;
            }

            if (!soldier.ConsumeNextSkillMustCritical(skillId))
            {
                return;
            }

            damageContext.IsCrit = true;
            LogTool.Log(
                $"[BattleRuleMustCriticalSupport] 下次技能必暴生效: hero={soldier.cardId}, skillId={skillId}");
        }

        private static BattleRuleTriggerContext CreateSkillCastContext(SoldierCard soldier, int skillId)
        {
            return new BattleRuleTriggerContext(TriggerType.OnSkillBefore, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                Source = soldier,
                Target = soldier,
                DamageContext = new DamageContext
                {
                    Soldier = soldier,
                    SkillId = skillId,
                    PipelineType = DamagePipelineType.SoldierDamage,
                },
                CurrentTime = Time.time,
            };
        }

        private static bool RuleGrantsNextSkillMustCritical(
            System.Collections.Generic.IReadOnlyList<int> effectGroupIds,
            TbEffectGroupConfig effectTable)
        {
            for (int i = 0; i < effectGroupIds.Count; i++)
            {
                var effectGroup = effectTable.GetOrDefault(effectGroupIds[i]);
                if (effectGroup == null || effectGroup.Effects == null)
                {
                    continue;
                }

                for (int j = 0; j < effectGroup.Effects.Count; j++)
                {
                    var effect = effectGroup.Effects[j];
                    if (effect.EffectType == EffectGroupsType.ModifyAttr
                        && effect.AttributeType == AttributeType.ThisSkillMustCritical
                        && effect.BaseValue > 0f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool RuleUsesSkillCastComputer(
            System.Collections.Generic.IReadOnlyList<int> computerGroupIds,
            TbComputerGroupConfig computerTable)
        {
            for (int i = 0; i < computerGroupIds.Count; i++)
            {
                var computerConfig = computerTable.GetOrDefault(computerGroupIds[i]);
                if (computerConfig != null && computerConfig.TriggerType == CounterTriggerType.SkillCastCount)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
