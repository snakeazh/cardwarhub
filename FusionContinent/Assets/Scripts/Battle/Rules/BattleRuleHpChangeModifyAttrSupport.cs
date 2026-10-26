using Config;
using FusionContinent;
using Manager;

namespace Game.Battle
{
    /// <summary>
    /// HpChange 持久 ModifyAttr（如命悬一线 111007）：条件满足时写入，不满足时移除。
    /// Buff 类效果仍由独立 AddBuff/RemoveBuff 规则处理，此处只处理 ModifyAttr。
    /// </summary>
    public static class BattleRuleHpChangeModifyAttrSupport
    {
        public static void RemoveModifyAttrEffects(SkillRuleConfig ruleConfig, BattleRuleTriggerContext ruleContext)
        {
            if (ruleConfig == null || ruleContext == null)
            {
                return;
            }

            var soldier = ruleContext.Source as SoldierCard;
            if (soldier == null || soldier.isDie)
            {
                return;
            }

            var sourceKey = BattleRuleNewSoldierSyncSupport.BuildSourceKey(ruleConfig.Id, ruleContext.StackSourceKey);
            var sourceType = ruleContext.ModifierSourceType != AttributeSourceType.Base
                ? ruleContext.ModifierSourceType
                : AttributeSourceType.SchoolCard;

            for (int i = 0; i < ruleConfig.EffectGroupId.Count; i++)
            {
                var effectGroup = DataConfigManager.Tables.TbEffectGroupConfig.GetOrDefault(ruleConfig.EffectGroupId[i]);
                if (effectGroup == null || effectGroup.PersistToAttributeSet != 0 || effectGroup.Effects == null)
                {
                    continue;
                }

                for (int j = 0; j < effectGroup.Effects.Count; j++)
                {
                    var effect = effectGroup.Effects[j];
                    if (effect == null || effect.EffectType != EffectGroupsType.ModifyAttr)
                    {
                        continue;
                    }

                    BattleRuleAcquireSoldierEffectApplier.RemoveEffectFromAttributeSet(
                        soldier.BaseAttributes,
                        effect.AttributeType,
                        sourceType,
                        sourceKey);
                }
            }
        }
    }
}
