using Config;
using FusionContinent;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 回合/波次开始（RoundStart）规则触发入口。
    /// 典型：被动 2150102 学习规则 113002，每波给全体友方加 Buff。
    /// </summary>
    public static class BattleRuleRoundStartTrigger
    {
        public static void Trigger()
        {
            if (AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null)
            {
                return;
            }

            var ruleContext = new BattleRuleTriggerContext(
                TriggerType.RoundStart,
                BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                ValueProvider = BattleRuleAcquireValueProvider.Instance,
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = Time.time,
            };

            BattleRuleComputerSupport.OnRoundStart(ruleContext);

            var triggeredCount = ruleService.TriggerLearnedBattleRules(TriggerType.RoundStart, ruleContext);
            LogTool.Log(
                $"[BattleRuleRoundStartTrigger] 回合开始规则触发完成: day={Global.Date.Value}, triggered={triggeredCount}");
        }
    }
}
