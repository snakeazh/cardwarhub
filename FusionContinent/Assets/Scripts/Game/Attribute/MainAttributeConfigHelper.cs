using System.Collections.Generic;
using Config;

namespace FusionContinent.Attribute
{
    /// <summary>
    /// 主属性配置辅助
    /// 适配项目现有的 Luban 主属性配置表。
    /// </summary>
    public static class MainAttributeConfigHelper
    {
        public static MainAttributeType ResolveMainAttributeType(int configId, MainAttributeType? overrideType = null)
        {
            if (overrideType.HasValue)
            {
                return overrideType.Value;
            }

            var cfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(configId);
            return cfg?.MainAttrType ?? MainAttributeType.Strength;
        }

        public static Config.MainAttributeConfig GetConfig(MainAttributeType mainType)
        {
            return DataConfigManager.Tables.TbMainAttributeConfig.GetOrDefault(mainType);
        }

        public static SoldierLevelPropertyList GetLevelProperty(int configId, int level)
        {
            var cfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(configId);
            if (cfg?.LevelPropertyList == null || cfg.LevelPropertyList.Count == 0)
            {
                return null;
            }

            SoldierLevelPropertyList fallback = null;
            foreach (var item in cfg.LevelPropertyList)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.Level == level)
                {
                    return item;
                }

                if (item.Level <= level)
                {
                    fallback = item;
                }
            }

            return fallback ?? cfg.LevelPropertyList[0];
        }

        public static Dictionary<MainAttributeType, float> GetMainAttributeValues(int configId, int level)
        {
            var result = new Dictionary<MainAttributeType, float>();
            var levelProperty = GetLevelProperty(configId, level);
            if (levelProperty?.MainAttributes == null)
            {
                return result;
            }

            foreach (var attr in levelProperty.MainAttributes)
            {
                if (result.ContainsKey(attr.Type))
                {
                    result[attr.Type] += attr.Value;
                }
                else
                {
                    result[attr.Type] = attr.Value;
                }
            }

            return result;
        }

        public static void ApplyMainAttributeDelta(AttributeSet attrSet, MainAttributeType ownerMainType, MainAttributeType changedType, float delta)
        {
            if (attrSet == null || delta == 0f)
            {
                return;
            }

            var cfg = GetConfig(changedType);
            if (cfg?.ConvertToBaseAttributes == null || cfg.ConvertToBaseAttributes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < cfg.ConvertToBaseAttributes.Count; i++)
            {
                var convertAttr = cfg.ConvertToBaseAttributes[i];
                if (i == 0 && changedType != ownerMainType)
                {
                    continue;
                }

                var modifier = new AttributeModifier
                {
                    SourceType = AttributeSourceType.MainAttribute,
                    SourceKey = $"main_{(int)changedType}_{i}",
                    FlatAdd = convertAttr.ValueType == ValueType.Number ? convertAttr.Value * delta : 0f,
                    PercentAdd = convertAttr.ValueType == ValueType.Percentage ? convertAttr.Value * delta : 0f,
                };

                attrSet.GetAttr(convertAttr.Type).AddModifier(modifier, true);
            }
        }
    }
}
