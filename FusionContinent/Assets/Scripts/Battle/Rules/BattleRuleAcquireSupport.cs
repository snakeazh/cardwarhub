using Config;
using FusionContinent;
using FusionContinent.Attribute;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 获得规则时的条件取值（无 DamageContext）。
    /// </summary>
    public sealed class BattleRuleAcquireValueProvider : IBattleRuleValueProvider
    {
        public static readonly BattleRuleAcquireValueProvider Instance = new BattleRuleAcquireValueProvider();

        public float GetAttributeValue(BattleRuleTriggerContext context, UnitType unitType, AttributeType attributeType)
        {
            if (BattleRuleSoldierHealthConditionValues.TryResolveGoalAttributeValue(
                    context, unitType, attributeType, out var healthValue))
            {
                return healthValue;
            }

            if (BattleRuleMonsterHealthConditionValues.TryResolveGoalAttributeValue(
                    context, unitType, attributeType, out var monsterHealthValue))
            {
                return monsterHealthValue;
            }

            if (BattleRuleMonsterHealthConditionValues.IsMonsterHealthAttribute(attributeType))
            {
                return BattleRuleMonsterHealthConditionValues.UnresolvedMonsterHealthValue;
            }

            if (unitType == UnitType.Target)
            {
                return GamePlayManager.Default.ModuleAttributes.GetFinal(attributeType);
            }

            return GetRepresentativeSoldierAttribute(attributeType);
        }

        public float GetSpecialValue(BattleRuleTriggerContext context, SpecialsType specialType)
        {
            if (specialType == SpecialsType.MainStage)
            {
                return IsMainStageBattle() ? 1f : 0f;
            }

            if (specialType == SpecialsType.HeroNum)
            {
                return CountAliveHeroes();
            }

            if (specialType == SpecialsType.LiveMageNum)
            {
                return CountHeroesByCareer(CareerType.Wizard);
            }

            if (specialType == SpecialsType.LiveWarriorNum)
            {
                return CountHeroesByCareer(CareerType.Warrior);
            }

            if (specialType == SpecialsType.BuyEffectsNumInBattle)
            {
                return BattleRuleBattleEconomySupport.GetBoughtEffectsNumInBattle();
            }

            if (specialType == SpecialsType.IsAssistant)
            {
                if (context.Source is SoldierCard hero)
                {
                    return GetHeroCareerType(hero) == CareerType.Assistant ? 1f : 0f;
                }

                return 0f;
            }

            if (context.Source is SoldierCard sourceHero)
            {
                if (TryGetPerHeroCareerMatch(sourceHero, specialType, out var careerMatch))
                {
                    return careerMatch;
                }

                if (TryGetPerHeroHighLevelCareerMatch(sourceHero, specialType, out var highLevelMatch))
                {
                    return highLevelMatch;
                }

                if (TryGetPerHeroMainAttributeMatch(sourceHero, specialType, out var mainAttrMatch))
                {
                    return mainAttrMatch;
                }
            }

            var careerType = ToCareerType(specialType);
            if (careerType.HasValue)
            {
                return CountHeroesByCareer(careerType.Value);
            }

            return 0f;
        }

        public float GetComparisonValue(BattleRuleTriggerContext context, ConditionGroupConfig conditionConfig)
        {
            return 0f;
        }

        private static float GetRepresentativeSoldierAttribute(AttributeType attributeType)
        {
            var hero = BattleRuleSoldierHealthConditionValues.ResolveRepresentativeSoldier();
            return hero != null ? hero.BaseAttributes.GetFinal(attributeType) : 0f;
        }

        private static bool IsMainStageBattle()
        {
            var stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(Global.StageId.Value);
            return stageCfg != null && stageCfg.StageType == StageType.Main;
        }

        private static int CountAliveHeroes()
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return 0;
            }

            var count = 0;
            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero.isDie || hero.RoleType == SoldierRoleType.Mirror)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static int CountHeroesByCareer(CareerType careerType)
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return 0;
            }

            var count = 0;
            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero.isDie || hero.RoleType == SoldierRoleType.Mirror)
                {
                    continue;
                }

                if (GetHeroCareerType(hero) == careerType)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryGetPerHeroCareerMatch(SoldierCard hero, SpecialsType specialType, out float value)
        {
            value = 0f;
            var careerType = ToCareerType(specialType);
            if (!careerType.HasValue)
            {
                return false;
            }

            value = GetHeroCareerType(hero) == careerType.Value ? 1f : 0f;
            return true;
        }

        private static bool TryGetPerHeroHighLevelCareerMatch(SoldierCard hero, SpecialsType specialType, out float value)
        {
            value = 0f;
            var careerType = ToHighLevelCareerType(specialType);
            if (!careerType.HasValue)
            {
                return false;
            }

            if (GetHeroCareerType(hero) != careerType.Value)
            {
                value = 0f;
                return true;
            }

            value = IsHighestLevelHeroInCareer(hero, careerType.Value) ? 1f : 0f;
            return true;
        }

        private static bool TryGetPerHeroMainAttributeMatch(SoldierCard hero, SpecialsType specialType, out float value)
        {
            value = 0f;
            MainAttributeType? mainType = specialType switch
            {
                SpecialsType.PowerHero => MainAttributeType.Strength,
                SpecialsType.IntHero => MainAttributeType.Intelligence,
                SpecialsType.AgHero => MainAttributeType.Dexterity,
                _ => null
            };

            if (!mainType.HasValue)
            {
                return false;
            }

            value = hero.MainAttrType == mainType.Value ? 1f : 0f;
            return true;
        }

        private static bool IsHighestLevelHeroInCareer(SoldierCard hero, CareerType careerType)
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return false;
            }

            var heroLevel = hero.level.Value;
            var maxLevel = heroLevel;
            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var other = gamePlay.villageGameCards[i];
                if (other.isDie || other.RoleType == SoldierRoleType.Mirror)
                {
                    continue;
                }

                if (GetHeroCareerType(other) != careerType)
                {
                    continue;
                }

                if (other.level.Value > maxLevel)
                {
                    maxLevel = other.level.Value;
                }
            }

            return heroLevel >= maxLevel;
        }

        private static CareerType GetHeroCareerType(SoldierCard hero)
        {
            var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(hero.cardId);
            return soldierCfg != null ? soldierCfg.CarType : CareerType.Warrior;
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
                SpecialsType.IsWarrior => CareerType.Warrior,
                SpecialsType.IsArcher => CareerType.Archer,
                SpecialsType.IsMage => CareerType.Wizard,
                SpecialsType.IsAssassin => CareerType.Assassin,
                _ => null
            };
        }

        private static CareerType? ToHighLevelCareerType(SpecialsType specialType)
        {
            return specialType switch
            {
                SpecialsType.HighLevelWarriorNum => CareerType.Warrior,
                SpecialsType.HighLevelArcherNum => CareerType.Archer,
                SpecialsType.HighLevelMageNum => CareerType.Wizard,
                SpecialsType.HighLevelAssassinNum => CareerType.Assassin,
                SpecialsType.HighLevelAssistantNum => CareerType.Assistant,
                _ => null
            };
        }
    }

    /// <summary>
    /// 获得/命中后/生命值变化规则的效果执行入口。
    /// 将 EffectGroupConfig.Effects 交给 BattleRuleEffectGroupsApplier.ApplyForAcquire 分发。
    /// </summary>
    public sealed class BattleRuleAcquireEffectExecutor : IBattleRuleEffectExecutor
    {
        public static readonly BattleRuleAcquireEffectExecutor Instance = new BattleRuleAcquireEffectExecutor();

        public void ExecuteEffect(BattleRuleEffectExecuteInfo executeInfo)
        {
            var effectGroup = executeInfo?.EffectGroupConfig;
            var ruleContext = executeInfo?.Context;
            var ruleConfig = executeInfo?.RuleConfig;
            if (effectGroup == null || ruleContext == null || ruleConfig == null)
            {
                return;
            }

            SoldierCard soldier = null;
            if (ruleContext.Source is SoldierCard sourceHero)
            {
                soldier = sourceHero;
            }

            ApplyEffectGroup(ruleConfig, effectGroup, ruleContext, soldier);
        }

        /// <summary>
        /// 将效果组应用到单个英灵（新召唤补同步）；soldier 为空时保持全场逻辑。
        /// </summary>
        public static void ApplyEffectGroup(
            SkillRuleConfig ruleConfig,
            EffectGroupConfig effectGroup,
            BattleRuleTriggerContext ruleContext,
            SoldierCard soldier)
        {
            if (effectGroup == null || ruleContext == null || ruleConfig == null)
            {
                return;
            }

            var sourceKey = BattleRuleNewSoldierSyncSupport.BuildSourceKey(ruleConfig.Id, ruleContext.StackSourceKey);
            var sourceType = ruleContext.ModifierSourceType != 0
                ? ruleContext.ModifierSourceType
                : AttributeSourceType.SchoolCard;
            var useAdditiveStack = BattleRuleNewSoldierSyncSupport.UsesAdditiveModifierStack(ruleConfig);

            BattleRuleEffectGroupsApplier.ApplyForAcquire(
                ruleContext,
                ruleConfig.Trigger,
                effectGroup.Effects,
                sourceKey,
                sourceType,
                soldier,
                ruleConfig,
                useAdditiveStack,
                effectGroup.PersistToAttributeSet);
        }
    }
}
