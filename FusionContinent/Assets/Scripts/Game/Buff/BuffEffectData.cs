using System;
using Config;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// Buff 效果数据
    /// 描述单个属性修正效果
    /// </summary>
    [Serializable]
    public class BuffEffectData
    {
        /// <summary>效果类型（属性修正 / 其他类型）</summary>
        public BuffEffectType EffectType = BuffEffectType.AttributeModifier;

        /// <summary>属性类型（当 EffectType = AttributeModifier 时生效）</summary>
        public AttributeType AttributeType = AttributeType.AddMagicDamage;

        /// <summary>效果值（根据 ValueType 解释为固定值或百分比）</summary>
        public float Value = 0f;

        /// <summary>数值类型（固定值 / 百分比）</summary>
        public ValueType ValueType = ValueType.Number;

        /// <summary>百分比效果值（可选）</summary>
        public float PercentValue = 0f;

        /// <summary>百分比属性来源（可选）</summary>
        public AttributeType PercentAttr = AttributeType.AddMagicDamage;
    }

    /// <summary>
    /// Buff 效果类型
    /// </summary>
    public enum BuffEffectType
    {
        /// <summary>属性修正（攻击、血量等）</summary>
        AttributeModifier = 0,

        /// <summary>生命恢复</summary>
        HealthRegen = 1,

        /// <summary>护甲提升</summary>
        Armor = 2,

        /// <summary>魔抗提升</summary>
        MagicResist = 3,

        /// <summary>移动速度</summary>
        MovementSpeed = 4,
    }
}
