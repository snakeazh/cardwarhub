using Config;
using FusionContinent;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 施加治疗/护盾后（AfterTreatmentWasApplied）规则触发入口。
    /// 仅当 <see cref="TakeDamageType.Treatment"/> 时判定为治疗或护盾。
    /// </summary>
    public static class BattleRuleTreatmentTrigger
    {
        private static int sReentrancyDepth;

        /// <summary>
        /// 英灵对目标成功施加治疗或护盾后调用。
        /// </summary>
        /// <param name="caster">施放治疗/护盾的英灵（可为空，表示非英灵来源）</param>
        /// <param name="treatedSoldier">接受治疗/护盾的目标英灵</param>
        /// <param name="damageType">效果 DamageType；仅 <see cref="TakeDamageType.Treatment"/> 会触发</param>
        public static void Trigger(
            SoldierCard caster,
            SoldierCard treatedSoldier,
            TakeDamageType damageType)
        {
            if (damageType != TakeDamageType.Treatment)
            {
                return;
            }

            if (treatedSoldier == null || treatedSoldier.isDie)
            {
                return;
            }

            if (sReentrancyDepth > 0)
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

            sReentrancyDepth++;
            try
            {
                var ruleContext = new BattleRuleTriggerContext(
                    TriggerType.AfterTreatmentWasApplied,
                    BattleRuleDamageSupport.GlobalBattleOwnerId)
                {
                    Source = caster != null ? caster : treatedSoldier,
                    Target = treatedSoldier,
                    ValueProvider = BattleRuleAcquireValueProvider.Instance,
                    EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                    CurrentTime = Time.time,
                    DamageContext = new DamageContext
                    {
                        Soldier = caster != null ? caster : treatedSoldier,
                        DamageType = TakeDamageType.Treatment,
                    },
                };

                var triggeredCount = ruleService.TriggerLearnedBattleRules(
                    TriggerType.AfterTreatmentWasApplied,
                    ruleContext);
                LogTool.Log(
                    $"[BattleRuleTreatmentTrigger] 治疗/护盾后规则: caster={(caster != null ? caster.cardId : 0)}, target={treatedSoldier.cardId}, triggered={triggeredCount}");
            }
            finally
            {
                sReentrancyDepth--;
            }
        }
    }
}
