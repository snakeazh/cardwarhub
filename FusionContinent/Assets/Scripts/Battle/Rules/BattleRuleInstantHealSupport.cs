using Config;
using FusionContinent;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 规则中的即时治疗：
    /// SoldierHealth / RestoreCurrentHealth / SoldierPerRestoreCurrentHealth 不写 AttributeSet，直接 SetSoldierHp。
    /// 若写入 AttributeSet 且 SourceKey 固定，同值覆盖不会触发 OnFinalValueChanged，表现为只回血一次。
    /// </summary>
    public static class BattleRuleInstantHealSupport
    {
        public static bool TryApply(
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect,
            SoldierCard targetSoldier)
        {
            if (effect == null
                || (effect.AttributeType != AttributeType.SoldierHealth
                    && effect.AttributeType != AttributeType.RestoreCurrentHealth
                    && effect.AttributeType != AttributeType.SoldierPerRestoreCurrentHealth))
            {
                return false;
            }

            var healFlat = BattleRuleEffectParamUtil.BuildModifier(
                effect,
                "BattleRuleInstantHeal",
                AttributeSourceType.FusionGem,
                targetSoldier,
                null,
                ruleContext).FlatAdd;

            if (healFlat <= 0f)
            {
                return true;
            }

            if (targetSoldier != null)
            {
                ApplyHeal(targetSoldier, effect.AttributeType, healFlat);
                return true;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return true;
            }

            gamePlay.TraverseVillageSoldier(hero =>
            {
                if (!hero.isDie)
                {
                    ApplyHeal(hero, effect.AttributeType, healFlat);
                }
            });

            return true;
        }

        private static void ApplyHeal(SoldierCard soldier, AttributeType healType, float healFlat)
        {
            var amount = healFlat;
            if (healType == AttributeType.SoldierPerRestoreCurrentHealth)
            {
                amount = healFlat * soldier.BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);
            }

            var healInt = Mathf.CeilToInt(amount);
            if (healInt <= 0)
            {
                return;
            }

            soldier.SetSoldierHp(healInt);
            LogTool.Log(
                $"[BattleRuleInstantHeal] hero={soldier.cardId}, type={healType}, heal={healInt}");
        }
    }
}
