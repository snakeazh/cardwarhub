using System;
using System.Collections.Generic;
using Config;
using ValueType = Config.ValueType;

namespace FusionContinent.Attribute
{
    /// <summary>
    /// 静态属性计算器
    /// 兼容现有 SoldierBattleConfig / MainAttributeConfig 配表。
    /// 用于：外部预览、排行榜、阵容搭配。
    /// </summary>
    public static class StaticAttributeCalculator
    {
        public static Dictionary<AttributeType, float> CalcBaseAttributes(
            int configId,
            int level
        )
        {
            var result = new Dictionary<AttributeType, float>();
            var cfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(configId);
            if (cfg == null)
            {
                LogTool.LogError($"[StaticAttributeCalculator] 配置不存在: {configId}");
                return result;
            }

            foreach (var attrCfg in cfg.BaseAttributes)
            {
                AddAttribute(result, attrCfg.Type, attrCfg.Value, attrCfg.ValueType);
            }

            var levelProperty = MainAttributeConfigHelper.GetLevelProperty(configId, level);
            
            if (levelProperty != null)
            {
                AddAttribute(result, AttributeType.SoldierMaxHealth, levelProperty.Hp);
                AddAttribute(result, AttributeType.SoldierAttackPower, levelProperty.Attack);
                AddAttribute(result, AttributeType.SoldierSpellPower, levelProperty.SpellPower);
                AddAttribute(result, AttributeType.SoldierAttackRange, levelProperty.AttackRange);
                AddAttribute(result, AttributeType.SoldierMissNum, levelProperty.MissRate);
                AddAttribute(result, AttributeType.SoldierCriticalRate, levelProperty.CriticalRate);
                AddAttribute(result, AttributeType.SoldierCriticalDamage, levelProperty.CriticalDamage);
                AddAttribute(result, AttributeType.SoldierAttackInterval, levelProperty.AttackTime);

                if (levelProperty.Resistances != null)
                {
                    foreach (var resistance in levelProperty.Resistances)
                    {
                        switch (resistance.Type)
                        {
                            case ResistanceType.Armor:
                            case ResistanceType.Physical:
                                AddAttribute(result, AttributeType.SoldierPhysicalResistance, resistance.Value);
                                break;
                            case ResistanceType.Magic:
                                AddAttribute(result, AttributeType.SoldierMagicResistance, resistance.Value);
                                break;
                        }
                    }
                }
            }

            ApplyMainAttributes(result, configId, level);

            // if (star > 0)
            // {
            //     ApplyStarBonus(result, configId, star);
            // }
            //
            // if (awaken > 0)
            // {
            //     ApplyAwakenBonus(result, configId, awaken);
            // }

            return result;
        }

        public static float GetAttribute(int configId, int level, AttributeType type)
        {
            var attrs = CalcBaseAttributes(configId, level);
            return attrs.TryGetValue(type, out var val) ? val : 0f;
        }

        public static Dictionary<MainAttributeType, float> CalcMainAttributes(int configId, int level)
        {
            return MainAttributeConfigHelper.GetMainAttributeValues(configId, level);
        }

        public static AttributeSet CreateAttributeSet(int configId, int level)
        {
            var attrSet = new AttributeSet();
            attrSet.InitFromConfig(configId, level);
            return attrSet;
        }

        private static void ApplyMainAttributes(Dictionary<AttributeType, float> attrs, int configId, int level)
        {
            var cfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(configId);
            if (cfg == null)
            {
                return;
            }

            var ownerMainType = cfg.MainAttrType;
            var mainAttrs = MainAttributeConfigHelper.GetMainAttributeValues(configId, level);
            foreach (var kv in mainAttrs)
            {
                var mainCfg = MainAttributeConfigHelper.GetConfig(kv.Key);
                if (mainCfg?.ConvertToBaseAttributes == null)
                {
                    continue;
                }

                for (int i = 0; i < mainCfg.ConvertToBaseAttributes.Count; i++)
                {
                    var convertAttr = mainCfg.ConvertToBaseAttributes[i];
                    if (i == 0 && kv.Key != ownerMainType)
                    {
                        continue;
                    }

                    AddAttribute(attrs, convertAttr.Type, convertAttr.Value * kv.Value, convertAttr.ValueType);
                }
            }
        }

        private static void AddAttribute(Dictionary<AttributeType, float> attrs, AttributeType type, float value, ValueType valueType = ValueType.Number)
        {
            if (!attrs.ContainsKey(type))
            {
                attrs[type] = 0f;
            }

            if (valueType == ValueType.Percentage)
            {
                attrs[type] += attrs[type] * value;
            }
            else
            {
                attrs[type] += value;
            }
        }

        private static void ApplyStarBonus(Dictionary<AttributeType, float> attrs, int configId, int star)
        {
            // var starCfg = DataConfigManager.Tables.TbStarConfig?.GetOrDefault(star);
            // if (starCfg == null)
            // {
            //     return;
            // }
            //
            // float percentBonus = starCfg.AttributeBonusPercent;
            // var keys = new List<AttributeType>(attrs.Keys);
            // foreach (var key in keys)
            // {
            //     attrs[key] = attrs[key] * (1 + percentBonus);
            // }
        }

        private static void ApplyAwakenBonus(Dictionary<AttributeType, float> attrs, int configId, int awaken)
        {
            // var awakenCfg = DataConfigManager.Tables.TbAwakenConfig?.GetOrDefault(awaken);
            // if (awakenCfg == null)
            // {
            //     return;
            // }
            //
            // foreach (var bonus in awakenCfg.AttributeBonuses)
            // {
            //     AddAttribute(attrs, bonus.Type, bonus.Value, bonus.ValueType);
            // }
        }
    }
}
