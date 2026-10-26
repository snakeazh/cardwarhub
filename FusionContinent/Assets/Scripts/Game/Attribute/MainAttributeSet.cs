using System;
using System.Collections.Generic;
using Config;
using UnityEngine;

namespace FusionContinent.Attribute
{
    // -----------------------------
    // 主属性集合
    // -----------------------------
    [System.Serializable]
    [Obsolete("废弃的版本待删除")]
    public class MainAttributeSet
    {
        private AttributeSet _baseAttributes;
        private MainAttributeType _mainAttrType;
        private readonly Dictionary<MainAttributeType, float> _values = new();
        private TbMainAttributeConfig _mainAttributeConfig;
        public Dictionary<MainAttributeType, float> GetAll() => _values;
        
        private Dictionary<MainAttributeType, AttributeValue> _attrs = new();

        public MainAttributeSet(MainAttributeType mainAttrType, AttributeSet baseAttributes)
        {
            _mainAttrType = mainAttrType;
            _baseAttributes = baseAttributes;
            _mainAttributeConfig = DataConfigManager.Tables.TbMainAttributeConfig;
        }

        // 设置主属性，并同步更新基础属性
        public void Set(MainAttributeType type, float value, bool isAdditive = false)
        {
            _values[type] = isAdditive ? _values[type] + value : value;
            ConvertToBase(type, value);
        }
                                                        
        // 设置主属性百分比，并同步更新基础属性
        public void SetPer(MainAttributeType type, float perValue)
        {
            var addV = _values[type] * perValue;
            _values[type] += addV;
            ConvertToBase(type, addV);
        }
        
        // 获取某属性，自动初始化
        public AttributeValue GetAttr(MainAttributeType type)
        {
            if (!_attrs.TryGetValue(type, out var val))
            {
                var cfg =  _mainAttributeConfig.GetOrDefault(type);
                val = new AttributeValue(new AttributeConfig()
                {
                    Name = cfg.Name,
                    ValueLimit =  cfg.ValueLimit,
                    DisplayType = cfg.DisplayType,
                });
                _attrs[type] = val;
            }
            return val;
        }
        
        public float Get(MainAttributeType type)
        {
            var baseValue = _values.GetValueOrDefault(type, 0);
            
            float sumFlatAdd = 0;
            float sumPercentAdd = 1;

            if (_attrs.TryGetValue(type, out var val))
            {
                foreach (var mod in val.GetModifiers().Values)
                {
                    // 判断概率生效
                    if (!mod.CheckProbabilityTrigger()) continue;
                
                    sumFlatAdd += mod.FlatAdd;
                    sumPercentAdd += mod.PercentAdd;
                }
            }
            
            var finalValue = (baseValue * sumPercentAdd) + sumFlatAdd;
            
            // 数值限制（下限）
            var attrCfg = _mainAttributeConfig.GetOrDefault(type);
            if (finalValue < attrCfg.ValueLimit.Minimum) finalValue = attrCfg.ValueLimit.Minimum;
            // 数值限制（上限）
            if (finalValue > attrCfg.ValueLimit.Maximum) finalValue = attrCfg.ValueLimit.Maximum;

            // 重新转换加成
            ConvertToBase2(type, new AttributeModifier()
            {
                SourceKey = $"{type}",
                SourceType = AttributeSourceType.MainAttribute
            }, finalValue, false);
            
            return finalValue;
        }
        
        // 主属性转基础属性逻辑（可按需要扩展映射规则）
        private void ConvertToBase(MainAttributeType type, float value, bool isAdditive = true)
        {
            var attrCfg = _mainAttributeConfig.GetOrDefault(type);
            if (attrCfg != null && attrCfg.ConvertToBaseAttributes.Count > 0)
            {
                for (int i = 0; i < attrCfg.ConvertToBaseAttributes.Count; i++)
                {
                    var v = attrCfg.ConvertToBaseAttributes[i];
                    // 首个为专属性加成
                    if (i == 0 && type != _mainAttrType)
                    {
                        continue;
                    }
                
                    _baseAttributes.GetAttr(v.Type).SetBaseValue(v.Value * value, isAdditive);
                }
            }
        }
        
        private void ConvertToBase2(MainAttributeType type, AttributeModifier modifier, float value, bool isAdditive = true)
        {
            var attrCfg = _mainAttributeConfig.GetOrDefault(type);
            if (attrCfg != null && attrCfg.ConvertToBaseAttributes.Count > 0)
            {
                for (int i = 0; i < attrCfg.ConvertToBaseAttributes.Count; i++)
                {
                    var v = attrCfg.ConvertToBaseAttributes[i];
                    // 首个为专属性加成
                    if (i == 0 && type != _mainAttrType)
                    {
                        continue;
                    }

                    modifier.FlatAdd = v.Value * value;
                    modifier.PercentAdd = 0;
                    _baseAttributes.GetAttr(v.Type).AddModifier(modifier, isAdditive);
                }
            }
        }
        
        // 获取显示属性
        public string GetValueDisplay(MainAttributeType type)
        {
            string str;
            var value = Get(type);
            var attrCfg = _mainAttributeConfig.GetOrDefault(type);
            switch (attrCfg.DisplayType)
            {
                case ValueDisplayType.RoundUp:
                    str = $"{Mathf.CeilToInt(value)}";
                    break;
                case ValueDisplayType.RoundDown:
                    str = $"{Mathf.FloorToInt(value)}";
                    break;
                case ValueDisplayType.RoundOff:
                    str = $"{Mathf.Round(value)}";
                    break;
                case ValueDisplayType.KeepDecimal2:
                    str = $"{Math.Round(value, 2)}";
                    break;
                default:
                    str = $"{value}";
                    break;
            }

            return str;
        }
        
    }
}