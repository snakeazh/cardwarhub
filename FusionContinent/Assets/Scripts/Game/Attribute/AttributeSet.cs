using System;
using System.Collections.Generic;
using Config;
using QFramework;
using ValueType = Config.ValueType;

namespace FusionContinent.Attribute
{
    // -----------------------------
    // 属性集合：用于角色属性管理
    // -----------------------------
    [System.Serializable]
    public class AttributeSet
    {
        private Dictionary<AttributeType, AttributeValue> _attrs = new();
        
        // 获取最终值（用于战斗计算）
        public float GetFinal(AttributeType type) => GetFinalSpecial(type);
        public float GetFinal(AttributeType type, Func<float, bool> effectParamCond) => GetAttr(type).SumFinalValue(effectParamCond);
        
        // 获取全部属性
        public Dictionary<AttributeType, AttributeValue> GetAll() => _attrs;

        
        public float GetFinalSpecial(AttributeType type)
        {
            if (type == AttributeType.SoldierAttackSpeed)
            {
                return this.GetAttackTime();
            }

            if (type == AttributeType.SoldierCriticalRate)
            {
                return this.GetCritRate();
            }

            if (type == AttributeType.SoldierCriticalDamage)
            {
                return this.GetCritDamage();
            }

            if (type == AttributeType.SoldierMissNum)
            {
                return this.GetSoldierMissNum();
            }

            return GetAttr(type).FinalValue;
        }


        // 获取某属性，自动初始化
        public AttributeValue GetAttr(AttributeType type)
        {
            if (!_attrs.TryGetValue(type, out var val))
            {
                val = new AttributeValue(type);
                _attrs[type] = val;
            }
            return val;
        }

        // 设置某属性，自动初始化
        public void SetAttr(AttributeType type, float val, ValueType valueType = ValueType.Number, bool isAdditive = true)
        {
            var mod = new AttributeModifier
            {
                SourceType = AttributeSourceType.Base,
                SourceKey = ((int)type).ToString(),
                FlatAdd = valueType == ValueType.Number ? val : 0,
                PercentAdd = valueType == ValueType.Percentage ? val: 0
            };

            GetAttr(type).AddModifier(mod, isAdditive);
        }

        // 添加多个基础值
        public void AddRangeBaseValue(List<AttributeStruct> attrs, bool isAdditive = true)
        {
            foreach (var attr in attrs)
            {
                GetAttr(attr.Type).SetBaseValue(attr.Value, isAdditive);
            }
        }
        public void AddRangeBaseValue(AttributeSet attrSet, bool isAdditive = true)
        {
            foreach (var attr in attrSet.GetAll())
            {
                GetAttr(attr.Key).SetBaseValue(attr.Value.BaseValue, isAdditive);
            }
        }

        // 深拷贝一套属性（不包含 Modifier）用于战斗初始
        public AttributeSet Clone()
        {
            var copy = new AttributeSet();
            foreach (var kv in _attrs)
            {
                var clone = new AttributeValue(kv.Key);
                clone.SetBaseValue(kv.Value.BaseValue);
                copy._attrs[kv.Key] = clone;
            }
            return copy;
        }
        
        public void RegisterListener(AttributeType type, Action<float, float> callback)
        {
            GetAttr(type).OnFinalValueChanged += callback;
        }
        
    }
}