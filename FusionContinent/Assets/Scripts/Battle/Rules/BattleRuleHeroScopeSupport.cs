using Config;
using FusionContinent;
using Manager;

namespace Game.Battle
{
    /// <summary>
    /// 英雄专属规则作用域：流派卡 <see cref="SchoolCardConfig.IsAddHero"/> 学习的规则仅对指定英灵生效。
    /// </summary>
    public static class BattleRuleHeroScopeSupport
    {
        public static bool IsHeroScoped(in BattleLearnedRuleEntry entry)
        {
            return entry.ScopeHeroCardId > 0;
        }

        /// <summary>当前触发上下文是否命中该规则的英雄作用域。</summary>
        public static bool MatchesTriggerContext(in BattleLearnedRuleEntry entry, BattleRuleTriggerContext context)
        {
            if (!IsHeroScoped(entry))
            {
                return true;
            }

            var ruleConfig = DataConfigManager.Tables.TbSkillRuleConfig.GetOrDefault(entry.RuleId);
            if (ruleConfig != null
                && context != null
                && context.Trigger == TriggerType.RoundStart
                && BattleRuleRosterCompositionSupport.IsGloballyEffectiveRule(ruleConfig))
            {
                return IsScopedHeroAliveOnField(entry.ScopeHeroCardId);
            }

            var soldier = ResolveSoldier(context);
            return soldier != null && soldier.cardId == entry.ScopeHeroCardId;
        }

        public static bool IsScopedHeroAliveOnField(int scopeHeroCardId)
        {
            if (scopeHeroCardId <= 0)
            {
                return true;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return false;
            }

            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero != null && !hero.isDie && hero.cardId == scopeHeroCardId)
                {
                    return true;
                }
            }

            return false;
        }

        public static SoldierCard ResolveSoldier(BattleRuleTriggerContext context)
        {
            if (context == null)
            {
                return null;
            }

            if (context.Source is SoldierCard sourceHero)
            {
                return sourceHero;
            }

            if (context.DamageContext != null)
            {
                return context.DamageContext.Soldier;
            }

            return null;
        }

        /// <summary>击杀计数是否按英灵实例累加（英雄专属规则 + 对应 ComputerGroup）。</summary>
        public static bool UsesPerSoldierKillCounter(int computerGroupId, int killerHeroCardId)
        {
            if (computerGroupId <= 0 || killerHeroCardId <= 0)
            {
                return false;
            }

            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                if (entry.ScopeHeroCardId != killerHeroCardId)
                {
                    continue;
                }

                if (RuleUsesComputerGroup(entry.RuleId, computerGroupId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>该 ComputerGroup 是否被任意英雄专属规则引用。</summary>
        public static bool HasHeroScopedKillComputerGroup(int computerGroupId)
        {
            if (computerGroupId <= 0)
            {
                return false;
            }

            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                if (entry.ScopeHeroCardId <= 0)
                {
                    continue;
                }

                if (RuleUsesComputerGroup(entry.RuleId, computerGroupId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>存在英雄专属击杀计数时，非目标英灵的击杀不计入全局池。</summary>
        public static bool ShouldSkipGlobalKillCount(int computerGroupId, SoldierCard killer)
        {
            if (killer == null || !HasHeroScopedKillComputerGroup(computerGroupId))
            {
                return false;
            }

            if (UsesPerSoldierKillCounter(computerGroupId, killer.cardId))
            {
                return false;
            }

            return !HasGlobalKillRuleForComputerGroup(computerGroupId);
        }

        private static bool HasGlobalKillRuleForComputerGroup(int computerGroupId)
        {
            var learnedRules = AttributeManager.BattleLearnedSkillRules;
            for (int i = 0; i < learnedRules.Count; i++)
            {
                var entry = learnedRules[i];
                if (entry.ScopeHeroCardId > 0)
                {
                    continue;
                }

                if (RuleUsesComputerGroup(entry.RuleId, computerGroupId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RuleUsesComputerGroup(int ruleId, int computerGroupId)
        {
            var ruleConfig = DataConfigManager.Tables.TbSkillRuleConfig.GetOrDefault(ruleId);
            if (ruleConfig == null || ruleConfig.ComputerGroupId == null)
            {
                return false;
            }

            for (int i = 0; i < ruleConfig.ComputerGroupId.Count; i++)
            {
                if (ruleConfig.ComputerGroupId[i] == computerGroupId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
