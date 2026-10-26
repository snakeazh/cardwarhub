using Config;
using FusionContinent;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 生命值变化（HpChange）规则：条件取值与触发入口。
    /// </summary>
    public sealed class BattleRuleHpChangeValueProvider : IBattleRuleValueProvider
    {
        public static readonly BattleRuleHpChangeValueProvider Instance = new BattleRuleHpChangeValueProvider();

        public float GetAttributeValue(BattleRuleTriggerContext context, UnitType unitType, AttributeType attributeType)
        {
            if (BattleRuleSoldierHealthConditionValues.TryResolveGoalAttributeValue(
                    context, unitType, attributeType, out var healthValue))
            {
                return healthValue;
            }

            var soldier = ResolveSoldier(context);
            return soldier != null ? soldier.BaseAttributes.GetFinal(attributeType) : 0f;
        }

        public float GetSpecialValue(BattleRuleTriggerContext context, SpecialsType specialType)
        {
            if (specialType == SpecialsType.HeroNum)
            {
                return CountAliveHeroes();
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

        private static SoldierCard ResolveSoldier(BattleRuleTriggerContext context)
        {
            if (context?.Source is SoldierCard sourceSoldier)
            {
                return sourceSoldier;
            }

            return context?.Target as SoldierCard;
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
                if (!gamePlay.villageGameCards[i].isDie)
                {
                    count++;
                }
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
                if (hero.isDie)
                {
                    continue;
                }

                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(hero.cardId);
                if (soldierCfg != null && soldierCfg.CarType == careerType)
                {
                    count++;
                }
            }

            return count;
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
                _ => null
            };
        }
    }

    /// <summary>
    /// 英灵生命值变化后触发 HpChange 规则（扣血、治疗、直接改血均会进入）。
    /// </summary>
    public static class BattleRuleHpChangeTrigger
    {
        public static void Trigger(SoldierCard harmedSoldier, MonsterCard attacker = null)
        {
            if (harmedSoldier == null || harmedSoldier.isDie)
            {
                return;
            }

            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null)
            {
                LogTool.LogWarning(
                    $"[BattleRuleHpChangeTrigger] BattleRuleService 不可用, hero={harmedSoldier.cardId}, hp={harmedSoldier.CurrentHp}/{harmedSoldier.MaxHp}");
                return;
            }

            if (AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                LogTool.Log(
                    $"[BattleRuleHpChangeTrigger] 本局无已学规则, hero={harmedSoldier.cardId}, hp={harmedSoldier.CurrentHp}/{harmedSoldier.MaxHp}");
                return;
            }

            var maxHp = harmedSoldier.MaxHp;
            var hpRatio = maxHp > 0 ? (float)harmedSoldier.CurrentHp / maxHp : 0f;
            LogTool.Log(
                $"[BattleRuleHpChangeTrigger] 开始, hero={harmedSoldier.cardId}, hp={harmedSoldier.CurrentHp}/{maxHp}, ratio={hpRatio:F3}, learnedRules={AttributeManager.BattleLearnedSkillRules.Count}");

            var ruleContext = new BattleRuleTriggerContext(TriggerType.HpChange, BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                Source = harmedSoldier,
                Target = harmedSoldier,
                ValueProvider = BattleRuleHpChangeValueProvider.Instance,
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = Time.time,
            };

            if (attacker != null)
            {
                ruleContext.DamageContext = new DamageContext
                {
                    Soldier = harmedSoldier,
                    Monster = attacker,
                };
            }

            var triggeredCount = ruleService.TriggerLearnedBattleRules(TriggerType.HpChange, ruleContext);
            LogTool.Log(
                $"[BattleRuleHpChangeTrigger] 结束, hero={harmedSoldier.cardId}, hp={harmedSoldier.CurrentHp}/{harmedSoldier.MaxHp}, triggered={triggeredCount}");
        }
    }
}
