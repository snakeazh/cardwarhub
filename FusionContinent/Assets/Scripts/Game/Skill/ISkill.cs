using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;
using UnityEngine;

namespace FusionContinent.Skill
{
    public interface  ISkill
    {
        int Id { get; }
        int Level { get; }
        SpecialEffectType Type { get; }
        SoldierCard Parent { get; }// 父级
        SpecialEffectsConfig Config { get; }
        AttributeSet Attributes { get; }// 属性

        void Initialize(SoldierCard parent, int level, SpecialEffectsConfig cfg); 
        void Cast(object obj);

        bool IsReady(bool isActiveRelease); // 是否冷却完成
        bool IsNormalAttack(); // 是否为普攻
        
        float GetCriticalDamage(); // 获取暴击伤害
        Dictionary<TakeDamageType, int> SumDamageTypes(); // 计算伤害类型
        
        

    }

}