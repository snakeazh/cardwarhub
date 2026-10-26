using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Attribute;
using Manager;
using QFramework;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 属性加成 Buff（通用类）
    /// OnAdd: 注册属性修改器
    /// OnTick: 仅累加计时器
    /// OnRemove: 移除属性修改器
    /// </summary>
    public class EnergyPouringBuff : BuffBase
    {
        /// <summary>要修改的属性类型</summary>
        public AttributeType TargetAttribute { get; }

        /// <summary>修饰模式：Additive=叠加基础值，Multiply=乘法倍率</summary>
        public ModifierMode Mode { get; }

        /// <summary>本 Buff 实例的唯一 key（用于独立移除）</summary>
        protected readonly string _modifierKey;

        public EnergyPouringBuff(BuffType type, float duration, float value, ValueType valueType,
            AttributeType targetAttr, ModifierMode mode = ModifierMode.Additive)
            : base(type, duration, value, valueType)
        {
            TargetAttribute = targetAttr;
            Mode = mode;
            _modifierKey = $"buff_{(int)type}_{Guid.NewGuid():N}";
        }

        private List<AttributeData> _attributes = new List<AttributeData>();
        
        public override void OnAdd(object owner)
        {
            base.OnAdd(owner);
            
            if (owner is not SoldierCard soldier) return;

            var level = SkillContext?.Level ?? 1;
            float attrValue = Value;
            
            if (SkillContext != null && SkillContext.Config != null)
            {
                var lvCfg = DataConfigManager.Tables.TbSpecialEffectsConfig[SkillContext.SkillId].LevelPropertyList[level - 1];
                foreach (var attr in lvCfg.Attributes)
                {
                    if (attr.Type == AttributeType.SoldierAttackPower)
                    {
                        _attributes.Add(new AttributeData(attr));
                    }
                }
            }

            var modifier = new AttributeModifier
            {
                SourceType = AttributeSourceType.Skill,
                SourceKey = _modifierKey,
                FlatAdd = Mode == ModifierMode.Additive ? attrValue : 0f,
                PercentAdd = Mode == ModifierMode.Additive ? 0f : attrValue,
                ProbabilityTrigger = 0f
            };
            
            AttributeManager.SetAttributesLogic(modifier, _attributes, false,1,(owner as SoldierCard).cardId);
            // soldier.BaseAttributes.GetAttr(TargetAttribute).AddModifier(modifier);
        }

        public override void OnRemove(object owner)
        {
            base.OnRemove(owner);
            if (owner is not SoldierCard soldier) return;

            soldier.BaseAttributes.GetAttr(TargetAttribute)
                .RemoveModifier(AttributeSourceType.Skill, _modifierKey);
        }

        private float GetModifierValue()
        {
            return ValueType == ValueType.Percentage ? Value : Value;
        }
    }

    /// <summary>
    /// 修饰模式
    /// </summary>
    public enum ModifierMode
    {
        /// <summary>叠加基础值</summary>
        Additive = 0,
        /// <summary>乘法倍率（百分比加成）</summary>
        Multiply = 1
    }
}
