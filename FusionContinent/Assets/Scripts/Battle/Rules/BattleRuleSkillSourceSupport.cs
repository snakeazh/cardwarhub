using Config;
using FusionContinent;
using FusionContinent.Skill;
using Manager;

namespace Game.Battle
{
    /// <summary>
    /// 技能来源规则（SourceType=Skill/GeneralSkill）的施法技能解析。
    /// <para>
    /// StackSourceKey → 技能 Id → 持有该技能的存活英灵 → ISkill 实例，
    /// 供 AddBuff（*AttrName）与 ModifyAttr（Skill 类 SourceAttrType）读取按等级的技能属性。
    /// 非技能实例的链式学习来源（装备/流派/轮语者）见 <see cref="BattleRuleSourceParamSupport"/>。
    /// </para>
    /// </summary>
    public static class BattleRuleSkillSourceSupport
    {
        private const string PassiveSkillStackKeyPrefix = "PassiveSkill_";

        /// <summary>该来源类型的规则是否由技能实例提供动态数值。</summary>
        public static bool UsesSkillInstanceForValues(AttributeSourceType sourceType)
        {
            return sourceType == AttributeSourceType.Skill
                || sourceType == AttributeSourceType.GeneralSkill;
        }

        /// <summary>
        /// 从规则上下文解析学习该规则的技能 Id。
        /// Skill 来源：StackSourceKey 即技能 Id；GeneralSkill 来源：PassiveSkill_{技能Id}。
        /// 非技能来源返回 0。
        /// </summary>
        public static int ResolveSourceSkillId(BattleRuleTriggerContext ruleContext)
        {
            if (ruleContext == null)
            {
                return 0;
            }

            var stackSourceKey = ruleContext.StackSourceKey;
            if (string.IsNullOrEmpty(stackSourceKey))
            {
                return 0;
            }

            if (ruleContext.ModifierSourceType == AttributeSourceType.Skill
                && int.TryParse(stackSourceKey, out var skillId))
            {
                return skillId;
            }

            if (ruleContext.ModifierSourceType == AttributeSourceType.GeneralSkill
                && stackSourceKey.StartsWith(PassiveSkillStackKeyPrefix))
            {
                var passiveSkillIdText = stackSourceKey.Substring(PassiveSkillStackKeyPrefix.Length);
                if (int.TryParse(passiveSkillIdText, out var passiveSkillId))
                {
                    return passiveSkillId;
                }
            }

            return 0;
        }

        /// <summary>
        /// 解析持有该技能的存活英灵。
        /// 优先级：触发源英灵（自身持有该技能）→ ScopeHeroCardId → SpecialEffects.ActsOnCard → 场上第一个持有者。
        /// </summary>
        public static SoldierCard ResolveSourceSoldier(BattleRuleTriggerContext ruleContext, int skillId)
        {
            if (ruleContext.Source is SoldierCard sourceHero
                && !sourceHero.isDie
                && sourceHero.GetSkillById(skillId) != null)
            {
                return sourceHero;
            }

            if (ruleContext.ScopeHeroCardId > 0)
            {
                var scopedHero = FindAliveHero(ruleContext.ScopeHeroCardId, skillId);
                if (scopedHero != null)
                {
                    return scopedHero;
                }
            }

            var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillId);
            if (skillCfg != null && skillCfg.ActsOnCard > 0)
            {
                var actsOnHero = FindAliveHero(skillCfg.ActsOnCard, skillId);
                if (actsOnHero != null)
                {
                    return actsOnHero;
                }
            }

            return FindFirstAliveHeroWithSkill(skillId);
        }

        /// <summary>
        /// 解析学习该规则的技能实例；非技能来源或英灵不可用时返回 null。
        /// </summary>
        public static ISkill ResolveSourceSkill(BattleRuleTriggerContext ruleContext)
        {
            if (ruleContext == null
                || !UsesSkillInstanceForValues(ruleContext.ModifierSourceType))
            {
                return null;
            }

            var skillId = ResolveSourceSkillId(ruleContext);
            if (skillId <= 0)
            {
                return null;
            }

            var soldier = ResolveSourceSoldier(ruleContext, skillId);
            if (soldier == null)
            {
                return null;
            }

            return soldier.GetSkillById(skillId);
        }

        private static SoldierCard FindAliveHero(int heroCardId, int skillId)
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return null;
            }

            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero == null || hero.isDie || hero.cardId != heroCardId)
                {
                    continue;
                }

                if (skillId > 0 && hero.GetSkillById(skillId) == null)
                {
                    continue;
                }

                return hero;
            }

            return null;
        }

        private static SoldierCard FindFirstAliveHeroWithSkill(int skillId)
        {
            var gamePlay = GamePlayManager.Default;
            if (gamePlay == null)
            {
                return null;
            }

            for (int i = 0; i < gamePlay.villageGameCards.Count; i++)
            {
                var hero = gamePlay.villageGameCards[i];
                if (hero == null || hero.isDie)
                {
                    continue;
                }

                if (hero.GetSkillById(skillId) != null)
                {
                    return hero;
                }
            }

            return null;
        }
    }
}
