using Config;

using FusionContinent;

using FusionContinent.Attribute;

using Manager;

using System.Collections.Generic;



namespace Game.Battle

{

    /// <summary>

    /// GloballyEffective=1 的规则：按入局编队 Global.SoldierTeams 判条件，满足后对全队英灵施加效果。

    /// GloballyEffective=0 的规则：按英灵逐个判条件（见 BattleRuleAcquireValueProvider）。

    /// </summary>

    public static class BattleRuleRosterCompositionSupport

    {

        public static bool IsGloballyEffectiveRule(SkillRuleConfig ruleConfig)

        {

            return ruleConfig != null && ruleConfig.GloballyEffective != 0;

        }



        public static bool IsGloballyEffectiveNoConditionRule(SkillRuleConfig ruleConfig)

        {

            return ruleConfig != null

                && ruleConfig.Trigger == TriggerType.NoCondition

                && IsGloballyEffectiveRule(ruleConfig);

        }



        /// <summary>

        /// 是否走入局编队 + 全队生效链路（等价于 GloballyEffective=1 的 NoCondition 规则）。

        /// </summary>

        public static bool IsRosterCompositionRule(

            SkillRuleConfig ruleConfig,

            TbConditionGroupConfig conditionConfigTable)

        {

            return IsGloballyEffectiveNoConditionRule(ruleConfig);

        }



        public static bool IsRosterCompositionSpecialType(SpecialsType specialType)

        {

            return specialType switch

            {

                SpecialsType.WarriorNum => true,

                SpecialsType.ArcherNum => true,

                SpecialsType.MageNum => true,

                SpecialsType.AssassinNum => true,

                SpecialsType.AssistantNum => true,

                SpecialsType.HeroNum => true,

                SpecialsType.PowerHero => true,

                SpecialsType.IntHero => true,

                SpecialsType.AgHero => true,

                SpecialsType.MainStage => true,

                SpecialsType.DifferentProNum => true,

                SpecialsType.LiveMageNum => true,

                SpecialsType.LiveWarriorNum => true,

                SpecialsType.BuyEffectsNumInBattle => true,

                _ => false,

            };

        }



        /// <summary>
        /// 条件组是否依赖入局编队统计（含 DifferentProNum），须用 BattleRuleLineupValueProvider 判条件。
        /// </summary>
        public static bool UsesLineupScopedConditions(
            SkillRuleConfig ruleConfig,
            TbConditionGroupConfig conditionConfigTable)
        {
            if (ruleConfig?.ConditionGroupId == null || conditionConfigTable == null)
            {
                return false;
            }

            for (int i = 0; i < ruleConfig.ConditionGroupId.Count; i++)
            {
                var conditionConfig = conditionConfigTable.GetOrDefault(ruleConfig.ConditionGroupId[i]);
                if (conditionConfig != null && IsRosterCompositionSpecialType(conditionConfig.SpecialType))
                {
                    return true;
                }
            }

            return false;
        }



        public static string BuildAcquireSourceKey(int ruleId)

        {

            return $"BattleRuleAcquire_{ruleId}";

        }



        public static bool IsLineupConditionMet(

            IReadOnlyList<int> conditionGroupIds,

            BattleRuleConditionManager conditionManager)

        {

            if (conditionGroupIds == null || conditionGroupIds.Count == 0)

            {

                return true;

            }



            var context = CreateLineupRuleContext();

            return conditionManager.AreAllConditionsMet(conditionGroupIds, context);

        }



        public static BattleRuleTriggerContext CreateLineupRuleContext()

        {

            return new BattleRuleTriggerContext(TriggerType.NoCondition, BattleRuleDamageSupport.GlobalBattleOwnerId)

            {

                ValueProvider = BattleRuleLineupValueProvider.Instance,

                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,

                CurrentTime = UnityEngine.Time.time,

            };

        }



        public static string DescribeLineupConditions(

            IReadOnlyList<int> conditionGroupIds,

            TbConditionGroupConfig conditionConfigTable)

        {

            if (conditionGroupIds == null || conditionGroupIds.Count == 0 || conditionConfigTable == null)

            {

                return string.Empty;

            }



            var context = CreateLineupRuleContext();

            var parts = new List<string>(conditionGroupIds.Count);

            for (int i = 0; i < conditionGroupIds.Count; i++)

            {

                var conditionConfig = conditionConfigTable.GetOrDefault(conditionGroupIds[i]);

                if (conditionConfig == null)

                {

                    continue;

                }



                if (!IsRosterCompositionSpecialType(conditionConfig.SpecialType))

                {

                    continue;

                }



                var current = BattleRuleLineupValueProvider.Instance.GetSpecialValue(context, conditionConfig.SpecialType);

                parts.Add($"{conditionConfig.SpecialType}={current:F0}/{conditionConfig.ComparisonValue:F0}");

            }



            return string.Join(", ", parts);

        }



        private static bool IsLineupSoldierCard(BattleCardGroup card)

        {

            return card != null

                   && card.CardType == CardType.Soldier

                   && card.CardId != 0

                   && card.IsUnlock.Value;

        }



        public static int CountLineupHeroes()

        {

            var count = 0;

            for (int i = 0; i < Global.SoldierTeams.Count; i++)

            {

                if (IsLineupSoldierCard(Global.SoldierTeams[i]))

                {

                    count++;

                }

            }



            return count;

        }



        public static int CountLineupHeroesByCareer(CareerType careerType)

        {

            var count = 0;

            for (int i = 0; i < Global.SoldierTeams.Count; i++)

            {

                var card = Global.SoldierTeams[i];

                if (!IsLineupSoldierCard(card))

                {

                    continue;

                }



                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(card.CardId);

                if (soldierCfg != null && soldierCfg.CarType == careerType)

                {

                    count++;

                }

            }



            return count;

        }



        public static int CountLineupHeroesByMainAttribute(MainAttributeType mainAttributeType)

        {

            var count = 0;

            for (int i = 0; i < Global.SoldierTeams.Count; i++)

            {

                var card = Global.SoldierTeams[i];

                if (!IsLineupSoldierCard(card))

                {

                    continue;

                }



                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(card.CardId);

                if (soldierCfg != null && soldierCfg.MainAttrType == mainAttributeType)

                {

                    count++;

                }

            }



            return count;

        }



        public static int CountLineupDistinctCareers()

        {

            var careers = new HashSet<CareerType>();

            for (int i = 0; i < Global.SoldierTeams.Count; i++)

            {

                var card = Global.SoldierTeams[i];

                if (!IsLineupSoldierCard(card))

                {

                    continue;

                }



                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(card.CardId);

                if (soldierCfg != null)

                {

                    careers.Add(soldierCfg.CarType);

                }

            }



            return careers.Count;

        }

    }



    /// <summary>

    /// 入局编队条件取值：统计 Global.SoldierTeams，不读场上存活/已生成英灵。

    /// </summary>

    public sealed class BattleRuleLineupValueProvider : IBattleRuleValueProvider

    {

        public static readonly BattleRuleLineupValueProvider Instance = new BattleRuleLineupValueProvider();



        public float GetAttributeValue(BattleRuleTriggerContext context, UnitType unitType, AttributeType attributeType)

        {

            return BattleRuleAcquireValueProvider.Instance.GetAttributeValue(context, unitType, attributeType);

        }



        public float GetSpecialValue(BattleRuleTriggerContext context, SpecialsType specialType)

        {

            if (specialType == SpecialsType.HeroNum)

            {

                return BattleRuleRosterCompositionSupport.CountLineupHeroes();

            }



            if (specialType == SpecialsType.DifferentProNum)

            {

                return BattleRuleRosterCompositionSupport.CountLineupDistinctCareers();

            }

            if (specialType == SpecialsType.LiveMageNum
                || specialType == SpecialsType.LiveWarriorNum
                || specialType == SpecialsType.BuyEffectsNumInBattle
                || specialType == SpecialsType.IsAssistant
                || specialType == SpecialsType.MainStage)
            {
                return BattleRuleAcquireValueProvider.Instance.GetSpecialValue(context, specialType);
            }

            var careerType = ToCareerType(specialType);

            if (careerType.HasValue)

            {

                return BattleRuleRosterCompositionSupport.CountLineupHeroesByCareer(careerType.Value);

            }



            var mainAttrType = ToMainAttributeType(specialType);

            if (mainAttrType.HasValue)

            {

                return BattleRuleRosterCompositionSupport.CountLineupHeroesByMainAttribute(mainAttrType.Value);

            }



            return 0f;

        }



        public float GetComparisonValue(BattleRuleTriggerContext context, ConditionGroupConfig conditionConfig)

        {

            return 0f;

        }



        private static CareerType? ToCareerType(SpecialsType specialType)

        {

            return specialType switch

            {

                SpecialsType.WarriorNum => CareerType.Warrior,

                SpecialsType.ArcherNum => CareerType.Archer,

                SpecialsType.MageNum => CareerType.Wizard,

                SpecialsType.AssassinNum => CareerType.Assassin,

                SpecialsType.AssistantNum => CareerType.Assistant,

                _ => null,

            };

        }



        private static MainAttributeType? ToMainAttributeType(SpecialsType specialType)

        {

            return specialType switch

            {

                SpecialsType.PowerHero => MainAttributeType.Strength,

                SpecialsType.AgHero => MainAttributeType.Dexterity,

                SpecialsType.IntHero => MainAttributeType.Intelligence,

                _ => null,

            };

        }

    }

}


