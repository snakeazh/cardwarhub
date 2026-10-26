using System.Collections.Generic;
using Config;
using FusionContinent;
using Manager;

namespace Game.Battle
{
    /// <summary>
    /// 条件组 GoalAttribute 生命值比较：
    /// 配置 Attribute=SoldierMaxHealth 时，用 SoldierCard.CurrentHp / MaxHp 作为左值（是否掉血/残血比例）。
    /// 配置 Attribute=SoldierHealth 时，用 SoldierCard.CurrentHp 绝对值。
    /// </summary>
    public static class BattleRuleSoldierHealthConditionValues
    {
        public static bool TryResolveGoalAttributeValue(
            BattleRuleTriggerContext context,
            UnitType unitType,
            AttributeType attributeType,
            out float value)
        {
            value = 0f;
            if (attributeType != AttributeType.SoldierMaxHealth
                && attributeType != AttributeType.SoldierHealth)
            {
                return false;
            }

            var soldier = ResolveSoldier(context, unitType) ?? ResolveRepresentativeSoldier();
            if (soldier == null)
            {
                return false;
            }

            var maxHp = soldier.MaxHp;
            if (maxHp <= 0)
            {
                return false;
            }

            // 与 UI/战斗逻辑一致：CurrentHp 在 OnSoldierHealthChanged 中已从属性系统同步，HpChange 触发时即为最新 hp。
            if (attributeType == AttributeType.SoldierMaxHealth)
            {
                value = (float)soldier.CurrentHp / maxHp;
                return true;
            }

            value = soldier.CurrentHp;
            return true;
        }

        /// <summary>
        /// 条件组是否包含英灵生命值比例/当前生命（1017 满血、1018 非满血等）。
        /// </summary>
        public static bool UsesSoldierHealthConditionGroups(
            IReadOnlyList<int> conditionGroupIds,
            TbConditionGroupConfig conditionConfigTable)
        {
            if (conditionGroupIds == null || conditionGroupIds.Count == 0 || conditionConfigTable == null)
            {
                return false;
            }

            for (int i = 0; i < conditionGroupIds.Count; i++)
            {
                var conditionConfig = conditionConfigTable.GetOrDefault(conditionGroupIds[i]);
                if (conditionConfig == null
                    || conditionConfig.SpecialType != SpecialsType.GoalAttribute)
                {
                    continue;
                }

                if (conditionConfig.Attribute == AttributeType.SoldierMaxHealth
                    || conditionConfig.Attribute == AttributeType.SoldierHealth)
                {
                    return true;
                }
            }

            return false;
        }

        private static SoldierCard ResolveSoldier(BattleRuleTriggerContext context, UnitType unitType)
        {
            if (context == null)
            {
                return null;
            }

            if (unitType == UnitType.Self)
            {
                if (context.Source is SoldierCard sourceSoldier)
                {
                    return sourceSoldier;
                }

                return context.DamageContext?.Soldier;
            }

            if (context.Source is SoldierCard targetFromSource)
            {
                return targetFromSource;
            }

            if (context.Target is SoldierCard targetFromTarget)
            {
                return targetFromTarget;
            }

            return context.DamageContext?.Soldier;
        }

        public static SoldierCard ResolveRepresentativeSoldier()
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null || gamePlay.villageGameCards.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (!hero.isDie)
                {
                    return hero;
                }
            }

            return null;
        }
    }
}
