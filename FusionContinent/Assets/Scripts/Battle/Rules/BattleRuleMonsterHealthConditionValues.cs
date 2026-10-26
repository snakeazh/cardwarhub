using Config;
using FusionContinent;

namespace Game.Battle
{
    /// <summary>
    /// SkillRule 条件组中怪物生命值比较（替代旧 MonsterCard.EvaluateMonsterConditionsForDamage）。
    /// Attribute=MonsterMaxHealth 时返回 currentHp/maxHp；Attribute=MonsterHealth 时返回 currentHp。
    /// </summary>
    public static class BattleRuleMonsterHealthConditionValues
    {
        /// <summary>无法解析受击怪物时返回，使「残血」类比较不成立。</summary>
        public const float UnresolvedMonsterHealthValue = float.MaxValue;

        public static bool IsMonsterHealthAttribute(AttributeType attributeType)
        {
            return attributeType == AttributeType.MonsterMaxHealth
                || attributeType == AttributeType.MonsterHealth;
        }

        public static bool TryResolveGoalAttributeValue(
            BattleRuleTriggerContext context,
            UnitType unitType,
            AttributeType attributeType,
            out float value)
        {
            value = 0f;
            if (!IsMonsterHealthAttribute(attributeType))
            {
                return false;
            }

            var monster = ResolveMonster(context, unitType);
            if (monster == null)
            {
                return false;
            }

            var maxHp = (int)monster.Attributes.GetFinal(AttributeType.MonsterMaxHealth);
            if (maxHp <= 0)
            {
                return false;
            }

            if (attributeType == AttributeType.MonsterMaxHealth)
            {
                value = (float)monster.hp / maxHp;
                return true;
            }

            value = monster.hp;
            return true;
        }

        /// <summary>
        /// 伤害管线优先使用 DamageContext.Monster（本次受击者），避免 context.Target 锁定首个目标导致 AOE 误判。
        /// </summary>
        private static MonsterCard ResolveMonster(BattleRuleTriggerContext context, UnitType unitType)
        {
            if (context == null)
            {
                return null;
            }

            if (context.DamageContext != null && context.DamageContext.Monster != null)
            {
                return context.DamageContext.Monster;
            }

            if (unitType == UnitType.Target)
            {
                if (context.Target is MonsterCard targetMonster)
                {
                    return targetMonster;
                }
            }

            if (context.Source is MonsterCard sourceMonster)
            {
                return sourceMonster;
            }

            return null;
        }
    }
}
