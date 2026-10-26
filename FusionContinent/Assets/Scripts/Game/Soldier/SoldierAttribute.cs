using System.Collections.Generic;
using Config;

namespace FusionContinent.Soldier
{
    public class SoldierAttribute
    {
        public AttributeType type;
        public float baseValue;
        private float currentValue;
    
        // 不同类型的加成列表
        private List<Modifier> flatModifiers = new List<Modifier>();
        private List<Modifier> percentModifiers = new List<Modifier>();
    
        public float Value
        {
            get { RecalculateValue(); return currentValue; }
        }
    
        public SoldierAttribute(AttributeType type, float baseValue)
        {
            this.type = type;
            this.baseValue = baseValue;
            currentValue = baseValue;
        }
    
        // 添加加成
        public void AddModifier(Modifier modifier)
        {
            if (modifier.type == ModifierType.Flat)
                flatModifiers.Add(modifier);
            else if (modifier.type == ModifierType.Percent)
                percentModifiers.Add(modifier);
            
            RecalculateValue();
        }
    
        // 移除加成
        public void RemoveModifier(Modifier modifier)
        {
            if (modifier.type == ModifierType.Flat)
                flatModifiers.Remove(modifier);
            else if (modifier.type == ModifierType.Percent)
                percentModifiers.Remove(modifier);
            
            RecalculateValue();
        }
    
        // 重新计算属性值
        private void RecalculateValue()
        {
            // 1. 应用所有固定值加成 (按添加顺序)
            float finalValue = baseValue;
            foreach (var modifier in flatModifiers)
                finalValue += modifier.value;
            
            // 2. 应用所有百分比加成 (按添加顺序)
            foreach (var modifier in percentModifiers)
                finalValue *= (1 + modifier.value);
            
            currentValue = finalValue;
        }
    }
    
    // 加成类型
    public enum ModifierType { Flat, Percent }

    // 加成类
    public class Modifier
    {
        public ModifierType type;
        public float value;
        public object source; // 加成来源，例如技能或装备
    
        public Modifier(ModifierType type, float value, object source = null)
        {
            this.type = type;
            this.value = value;
            this.source = source;
        }
    }
}