using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Utils;
using UnityEngine;
using Random = System.Random;

namespace FusionContinent.Attribute
{
    // -----------------------------
    // 单个属性值（基础 + 加成来源集合）
    // -----------------------------
    public class AttributeValue
    {
        // BaseValue 只能由外部调用赋值，例如初始化时设置，游戏运行期间不可更改。
        public float BaseValue { get; private set; }

        // 加成项字典，按来源快速访问
        private readonly Dictionary<(AttributeSourceType, string), AttributeModifier> _modifiers = new();
        
        // 最终值变化监听
        private float _lastFinalValue;
        public event Action<float, float> OnFinalValueChanged;

        // 属性配置
        private readonly AttributeConfig _attrCfg = null;
        public AttributeValue(AttributeType type)
        {
            var cfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(type);
            if (cfg == null)
            {
                throw new Exception($"Init AttributeValue Not Configured Type {type}");
            }

            _attrCfg = new AttributeConfig
            {
                Name = cfg.Name,
                ValueLimit = cfg.ValueLimit,
                DisplayType = cfg.DisplayType
            };
        }
        
        public AttributeValue(AttributeConfig cfg)
        {
            _attrCfg = cfg;
            if (_attrCfg == null)
            {
                throw new Exception($"Init AttributeValue Not Configured");
            }
        }


        // 初始化设置基础值，只允许设置一次（或在限定阶段设置）
        public void SetBaseValue(float value, bool isAdditive = false)
        {
            BaseValue = isAdditive ? BaseValue + value : value;
            CheckAndNotifyChange();
        }
        
        // 最终值 = (基础值 * (1 + 所有百分比加成)) + 所有来源固定加成
        public float FinalValue => SumFinalValue();
        public float SumFinalValue(Func<float, bool> effectParamCond = null)
        {
            // 计算来源加成值
            var modVals = SumModifierValues(effectParamCond);
            // var finalValue = (BaseValue * sumPercentAdd) + sumFlatAdd;
            var finalValue = (BaseValue * modVals.Item2) + modVals.Item1;
            
            // 数值限制（下限）
            if (finalValue < _attrCfg.ValueLimit.Minimum) finalValue = _attrCfg.ValueLimit.Minimum;
            // 数值限制（上限）
            if (finalValue > _attrCfg.ValueLimit.Maximum) finalValue = _attrCfg.ValueLimit.Maximum;

            // 初始化更改值
            // if (_lastFinalValue == 0f) _lastFinalValue = finalValue;

            return finalValue;
        }

        public (float, float) SumModifierValues(Func<float, bool> effectParamCond = null)
        {
            float sumFlatAdd = 0;
            float sumPercentAdd = 1;
            foreach (var mod in _modifiers.Values)
            {
                // 判断概率生效
                if (!mod.CheckProbabilityTrigger()) continue;
                
                // 判断效果参数条件
                if (mod.EffectParam > 0f && effectParamCond != null && !effectParamCond(mod.EffectParam)) continue;
                
                sumFlatAdd += mod.FlatAdd;
                sumPercentAdd += mod.PercentAdd;
            }

            return (sumFlatAdd, sumPercentAdd);
        }
        
        // 获取全部加成项
        public Dictionary<(AttributeSourceType, string), AttributeModifier> GetModifiers() => _modifiers;
        
        // 添加/更新某来源的加成项（支持覆盖或累加）
        public void AddModifier(AttributeModifier mod, bool isAdditive = false)
        {
            var key = (mod.SourceType, mod.SourceKey);
            if (_modifiers.TryGetValue(key, out var existing))
            {
                if (isAdditive)
                {
                    existing.FlatAdd += mod.FlatAdd;
                    existing.PercentAdd += mod.PercentAdd;
                }
                else
                {
                    existing.FlatAdd = mod.FlatAdd;
                    existing.PercentAdd = mod.PercentAdd;
                }
            }
            else
            {
                // 浅拷贝
                _modifiers[key] = (AttributeModifier)mod.Clone();
            }
            
            CheckAndNotifyChange();
            // PrintModifiers();
        }

        // 移除指定来源的加成项
        public void RemoveModifier(AttributeSourceType type, string key)
        {
            _modifiers.Remove((type, key));
        }

        /**
         * 清除属性值
         * 
         * @return 
         * @author Zpp
         * @create 2025/11/25
         **/
        public void Clear()
        {
            _modifiers.Clear();
            BaseValue = 0;
            _lastFinalValue = 0;
        }

        /**
         * 重置最终值（危险的）
         * 
         * @return 
         * @author Zpp
         * @create 2025/12/15
         **/
        public void ResetFinalValue(float value)
        {
            Clear();
            SetBaseValue(value);
        }


        // 打印当前所有加成项（调试用）
        public void PrintModifiers()
        {
            foreach (var mod in _modifiers.Values)
            {
                LogTool.Log($"[{mod.SourceType}:{mod.SourceKey}] Flat={mod.FlatAdd}, Percent={mod.PercentAdd}");
            }
        }
        
        // 检查最终数值是否改变并触发事件
        private void CheckAndNotifyChange()
        {
            if (OnFinalValueChanged == null) return;
            
            float current = FinalValue;
            if (Math.Abs(current - _lastFinalValue) > 0.001f)
            {
                OnFinalValueChanged.Invoke(current, _lastFinalValue);
                _lastFinalValue = current;
            }
        }

        // 获取属性名称
        public string GetAttrName()
        {
            return _attrCfg.Name;
        }

        // 获取显示属性
        public string GetValueDisplay()
        {
            var value = FinalValue;
            var str = _attrCfg.DisplayType switch
            {
                ValueDisplayType.RoundUp => $"{Mathf.CeilToInt(value)}",
                ValueDisplayType.RoundDown => $"{Mathf.FloorToInt(value)}",
                ValueDisplayType.RoundOff => $"{Mathf.Round(value)}",
                ValueDisplayType.KeepDecimal2 => $"{Math.Round(value, 2)}",
                _ => $"{value}"
            };

            return str;
        }
        
    }
    
    // -----------------------------
    // 属性配置
    // -----------------------------
    public class AttributeConfig
    {
        public string Name;
        public ValueLimitStruct ValueLimit;
        public ValueDisplayType DisplayType;
    }

    // -----------------------------
    // 单个加成项（来源、数值）
    // -----------------------------
    [System.Serializable]
    public class AttributeModifier : System.ICloneable
    {
        public AttributeSourceType SourceType; // 来源类型（用于分类）
        public string SourceKey;               // 来源Key（如羁绊ID、BuffID）
        public float FlatAdd;                 // 固定加值
        public float PercentAdd;             // 百分比加成（如0.1 = +10%）
        public float ProbabilityTrigger;     // 概率生效：0.百分百
        public float EffectParam;           // 效果参数（备用）

        
        // 判断概率生效
        public bool CheckProbabilityTrigger()
        {
            if (ProbabilityTrigger > 0f)
            {
                return RandomTool.CheckRandomByTarget(Mathf.CeilToInt(ProbabilityTrigger * 100));
            }

            return true;
        }
        
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        
    }

}