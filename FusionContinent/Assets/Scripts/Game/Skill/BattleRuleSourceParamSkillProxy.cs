using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;

namespace FusionContinent.Skill
{
    /// <summary>
    /// 链式学习规则（装备/流派/轮语者等）的参数代理技能：仅 <see cref="Attributes"/> 参与 Buff 动态数值解析。
    /// </summary>
    public sealed class BattleRuleSourceParamSkillProxy : ISkill
    {
        public BattleRuleSourceParamSkillProxy(AttributeSet attributes)
        {
            Attributes = attributes;
        }

        public int Id => 0;
        public int Level => 1;
        public SpecialEffectType Type => default;
        public SoldierCard Parent => null;
        public SpecialEffectsConfig Config => null;
        public AttributeSet Attributes { get; }

        public void Initialize(SoldierCard parent, int level, SpecialEffectsConfig cfg)
        {
        }

        public void Cast(object obj)
        {
        }

        public bool IsReady(bool isActiveRelease) => false;

        public bool IsNormalAttack() => false;

        public float GetCriticalDamage() => 0f;

        public Dictionary<TakeDamageType, int> SumDamageTypes() => new Dictionary<TakeDamageType, int>();
    }
}
