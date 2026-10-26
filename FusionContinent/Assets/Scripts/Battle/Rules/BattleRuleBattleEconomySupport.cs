using System.Collections.Generic;
using Config;
using FusionContinent;
using Manager;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 武器 ExtraAttributes 相关的局内经济/计时/强化种类计数门面。
    /// </summary>
    public static class BattleRuleBattleEconomySupport
    {
        private static readonly HashSet<int> sBoughtEffectCardIds = new HashSet<int>();
        private static int sLastTrackedGold;
        private static bool sGoldWatchRegistered;
        private static float sEverySecondAccumulator;
        private static bool sEverySecondRunning;

        public static void ResetBattleState()
        {
            sBoughtEffectCardIds.Clear();
            sEverySecondAccumulator = 0f;
            sEverySecondRunning = false;
            sLastTrackedGold = Global.GameGold.Value;
        }

        /// <summary>战斗开局后开始监听战斗币消耗与每秒计数。</summary>
        public static void StartBattleWatchers()
        {
            ResetBattleState();
            sEverySecondRunning = true;
            EnsureGoldWatchRegistered();
        }

        public static void StopBattleWatchers()
        {
            sEverySecondRunning = false;
        }

        /// <summary>局内购买强化种类数（流派/技能三选一去重）。</summary>
        public static int GetBoughtEffectsNumInBattle()
        {
            return sBoughtEffectCardIds.Count;
        }

        public static void RegisterBoughtEffectCard(int cardId)
        {
            if (cardId <= 0)
            {
                return;
            }

            var schoolCfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(cardId);
            var exclusiveHeroCardId = 0;
            if (schoolCfg != null && schoolCfg.IsAddHero > 0)
            {
                exclusiveHeroCardId = schoolCfg.IsAddHero;
            }

            // 每次选卡都累加购买强化 Computer（含重复购买同一张）
            BattleRuleComputerSupport.OnPurchaseEffect(exclusiveHeroCardId);

            var added = sBoughtEffectCardIds.Add(cardId);
            if (!added)
            {
                return;
            }

            // 购买强化种类变化后，重试依赖 BuyEffectsNumInBattle 的 NoCondition 规则
            var ruleService = BattleRuleDamageSupport.GetRuleService();
            if (ruleService == null || AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            var context = new BattleRuleTriggerContext(
                TriggerType.NoCondition,
                BattleRuleDamageSupport.GlobalBattleOwnerId)
            {
                ValueProvider = BattleRuleLineupValueProvider.Instance,
                EffectExecutor = BattleRuleAcquireEffectExecutor.Instance,
                CurrentTime = Time.time,
            };
            ruleService.TryTriggerNoConditionRulesForBuyEffects(context);
        }

        /// <summary>由战斗主循环驱动（如 GamePlayManager.Update）。</summary>
        public static void Tick(float deltaTime)
        {
            if (!sEverySecondRunning || AttributeManager.BattleLearnedSkillRules.Count == 0)
            {
                return;
            }

            sEverySecondAccumulator += deltaTime;
            while (sEverySecondAccumulator >= 1f)
            {
                sEverySecondAccumulator -= 1f;
                BattleRuleComputerSupport.OnEverySecond();
            }
        }

        private static void EnsureGoldWatchRegistered()
        {
            if (sGoldWatchRegistered)
            {
                return;
            }

            sGoldWatchRegistered = true;
            sLastTrackedGold = Global.GameGold.Value;
            Global.GameGold.Register(OnGameGoldChanged);
        }

        private static void OnGameGoldChanged(int gold)
        {
            if (!sEverySecondRunning)
            {
                sLastTrackedGold = gold;
                return;
            }

            if (gold < sLastTrackedGold)
            {
                var spent = sLastTrackedGold - gold;
                BattleRuleComputerSupport.OnBattleGoldSpent(spent);
            }

            sLastTrackedGold = gold;
        }
    }
}
