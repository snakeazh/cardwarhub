using System.Collections.Generic;
using Config;

namespace FusionContinent.Soldier
{
    public class SoldierAttributeManager
    {
        private Dictionary<AttributeType, SoldierAttribute> attributes = new Dictionary<AttributeType, SoldierAttribute>();
    
        // 初始化属性
        public void InitializeAttributes(SoldierAttribute[] baseAttributes)
        {
            attributes.Clear();
            foreach (var attr in baseAttributes)
                attributes[attr.type] = new SoldierAttribute(attr.type, attr.baseValue);
        }
    
        // 获取属性值
        public float GetAttributeValue(AttributeType type)
        {
            if (attributes.TryGetValue(type, out SoldierAttribute attr))
                return attr.Value;
            return 0f;
        }
    
        // 添加属性加成
        public void AddModifier(AttributeType type, Modifier modifier)
        {
            if (attributes.TryGetValue(type, out SoldierAttribute attr))
                attr.AddModifier(modifier);
        }
    
        // 移除属性加成
        public void RemoveModifier(AttributeType type, Modifier modifier)
        {
            if (attributes.TryGetValue(type, out SoldierAttribute attr))
                attr.RemoveModifier(modifier);
        }
    
        // 移除来自特定来源的所有加成
        public void RemoveAllModifiersFromSource(object source)
        {
            foreach (var attr in attributes.Values)
            {
                // 这里需要实现具体的移除逻辑
            }
        }
    }
}