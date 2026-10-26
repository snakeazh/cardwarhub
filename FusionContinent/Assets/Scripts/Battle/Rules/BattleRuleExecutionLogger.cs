using System.Text;
using Config;
using FusionContinent;

namespace Game.Battle
{
    /// <summary>
    /// 规则执行成功时的统一日志输出（时刻 / 规则 / 条件属性 / 效果组）。
    /// </summary>
    public static class BattleRuleExecutionLogger
    {
        public static void LogRuleExecuted(
            int ownerId,
            SkillRuleConfig ruleConfig,
            BattleRuleTriggerContext context,
            BattleRuleConditionManager conditionManager)
        {
#if !UNITY_EDITOR
            return;
#else
            if (ruleConfig == null || context == null)
            {
                return;
            }

            var message = new StringBuilder(256);
            message.Append("[BattleRuleService] 规则执行");
            message.Append(" | time=").Append(context.CurrentTime.ToString("F3"));
            message.Append(" | ownerId=").Append(ownerId);
            message.Append(" | ruleId=").Append(ruleConfig.Id);
            message.Append(" | trigger=").Append(context.Trigger);
            message.Append(" | source=").Append(DescribeUnit(context.Source));
            message.Append(" | target=").Append(DescribeUnit(context.Target));

            if (conditionManager != null)
            {
                var attributeSummary = conditionManager.BuildConditionAttributeSummary(
                    ruleConfig.ConditionGroupId,
                    context);
                if (attributeSummary.Length > 0)
                {
                    message.Append(" | attrs=").Append(attributeSummary);
                }
            }

            if (ruleConfig.EffectGroupId != null && ruleConfig.EffectGroupId.Count > 0)
            {
                message.Append(" | effects=");
                for (int i = 0; i < ruleConfig.EffectGroupId.Count; i++)
                {
                    if (i > 0)
                    {
                        message.Append(',');
                    }

                    message.Append(ruleConfig.EffectGroupId[i]);
                }
            }

            if (context.DamageContext != null)
            {
                message.Append(" | damageCtx=");
                message.Append("skillId=").Append(context.DamageContext.SkillId);
                message.Append(",base=").Append(context.DamageContext.BaseDamage.ToString("F1"));
                message.Append(",final=").Append(context.DamageContext.FinalDamage.ToString("F1"));
                message.Append(",true=").Append(context.DamageContext.TrueDamage.ToString("F1"));
            }

            LogTool.Log(message.ToString());
#endif
        }

        private static string DescribeUnit(object unit)
        {
            if (unit is SoldierCard soldier)
            {
                return "Soldier(cardId=" + soldier.cardId + ",hp=" + soldier.CurrentHp + "/" + soldier.MaxHp + ")";
            }

            if (unit is MonsterCard monster)
            {
                return "Monster(cardId=" + monster._cardId + ",hp=" + monster.hp + "/" + monster.maxHp + ")";
            }

            if (unit == null)
            {
                return "null";
            }

            return unit.GetType().Name;
        }
    }
}
