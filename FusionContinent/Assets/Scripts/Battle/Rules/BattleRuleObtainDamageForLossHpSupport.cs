using Config;
using FusionContinent;
using FusionContinent.Attribute;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 每损失血量获得伤害修正（ObtainDamageForLossHP / 规则 111006）。
    /// 每损失固定 5% 最大生命叠一层，层数 * Effect.BaseValue 写入 AllDamageBonuses。
    /// </summary>
    public static class BattleRuleObtainDamageForLossHpSupport
    {
        /// <summary>每层对应的生命损失比例（固定 5%）。</summary>
        public const float LostHpStepRatio = 0.05f;

        public static bool TryApply(
            BattleRuleTriggerContext ruleContext,
            EffectParamStruct effect,
            string sourceKey,
            AttributeSourceType sourceType,
            SoldierCard targetSoldier)
        {
            if (effect == null || effect.AttributeType != AttributeType.ObtainDamageForLossHP)
            {
                return false;
            }

            var ampPerStack = effect.BaseValue;
            if (ampPerStack <= 0f)
            {
                return true;
            }

            var damageBonusSourceKey = string.IsNullOrEmpty(sourceKey)
                ? "ObtainDamageForLossHP"
                : sourceKey + "_ObtainDamageForLossHP";

            if (targetSoldier != null)
            {
                ApplyToSoldier(targetSoldier, ampPerStack, damageBonusSourceKey, sourceType);
                return true;
            }

            if (ruleContext != null && ruleContext.Source is SoldierCard sourceSoldier)
            {
                ApplyToSoldier(sourceSoldier, ampPerStack, damageBonusSourceKey, sourceType);
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
                    ApplyToSoldier(hero, ampPerStack, damageBonusSourceKey, sourceType);
                }
            });

            return true;
        }

        private static void ApplyToSoldier(
            SoldierCard soldier,
            float ampPerStack,
            string sourceKey,
            AttributeSourceType sourceType)
        {
            if (soldier == null || soldier.isDie)
            {
                return;
            }

            var maxHp = soldier.MaxHp;
            if (maxHp <= 0)
            {
                maxHp = (int)soldier.BaseAttributes.GetFinal(AttributeType.SoldierMaxHealth);
            }

            if (maxHp <= 0)
            {
                return;
            }

            var lostRatio = 1f - (float)soldier.CurrentHp / maxHp;
            if (lostRatio < 0f)
            {
                lostRatio = 0f;
            }

            var stacks = Mathf.FloorToInt(lostRatio / LostHpStepRatio);
            var bonus = stacks * ampPerStack;

            var modifier = new AttributeModifier
            {
                SourceKey = sourceKey,
                SourceType = sourceType,
                FlatAdd = bonus,
                PercentAdd = 0f,
            };

            soldier.BaseAttributes.GetAttr(AttributeType.AllDamageBonuses).AddModifier(modifier, false);

            LogTool.Log(
                $"[BattleRuleObtainDamageForLossHp] hero={soldier.cardId}, hp={soldier.CurrentHp}/{maxHp}, " +
                $"lost={lostRatio:F3}, stacks={stacks}, bonus={bonus:F3}, sourceKey={sourceKey}");
        }
    }
}
