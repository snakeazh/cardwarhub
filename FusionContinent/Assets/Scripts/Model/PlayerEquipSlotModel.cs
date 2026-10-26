using System.Collections.Generic;
using Config;

namespace FusionContinent.Model
{
    public class PlayerEquipSlot
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public EquipPartType PartType { get; set; }
        public int Level { get; set; }
        public EquipSlotConfig Config { get; set; }

        public PlayerEquipSlot()
        {
        }

        public PlayerEquipSlot(PlayerEquipSlot v)
        {
            Id = v.Id;
            UserId = v.UserId;
            PartType = v.PartType;
            Level = v.Level;
            Config = GetSlotConfig(v.PartType);
        }

        public static EquipSlotConfig GetSlotConfig(EquipPartType slotType)
        {
            foreach (var cfg in DataConfigManager.Tables.TbEquipSlotConfig.DataList)
            {
                if (cfg.SlotType == slotType)
                {
                    return cfg;
                }
            }

            return null;
        }

        public static PlayerEquipSlot CreateDefault(EquipPartType slotType)
        {
            return new PlayerEquipSlot
            {
                PartType = slotType,
                Level = 0,
                Config = GetSlotConfig(slotType),
            };
        }

        public SlotLevelListStruct GetLevelCfg(int level = 0)
        {
            if (Config == null)
            {
                return null;
            }

            if (level <= 0)
            {
                level = Level;
            }

            foreach (var cfg in Config.SlotLevelList)
            {
                if (cfg.Level == level)
                {
                    return cfg;
                }
            }

            return null;
        }

        public List<AttributeStruct> GetDisplayAttributes(int level = 0)
        {
            var cfg = GetLevelCfg(level);
            if (cfg == null)
            {
                return new List<AttributeStruct>();
            }

            var list = new List<AttributeStruct>();
            list.AddRange(cfg.BaseAttributes);
            list.AddRange(cfg.ExtraAttributes);
            return list;
        }

        /// <summary>
        /// 获取当前槽位等级已生效的额外属性等级配置（Level &lt;= 槽位等级，取最近一档有 ExtraAttributes 的配置）。
        /// </summary>
        public SlotLevelListStruct GetActiveExtraLevelCfg(int level = 0)
        {
            if (Config == null)
            {
                return null;
            }

            if (level <= 0)
            {
                level = Level;
            }

            SlotLevelListStruct activeCfg = null;
            foreach (var cfg in Config.SlotLevelList)
            {
                if (cfg.Level > level)
                {
                    break;
                }

                if (cfg.ExtraAttributes != null && cfg.ExtraAttributes.Count > 0)
                {
                    activeCfg = cfg;
                }
            }

            return activeCfg;
        }

        /// <summary>
        /// 获取下一档带 ExtraAttributes 的等级配置（Level &gt; 槽位等级）。
        /// </summary>
        public SlotLevelListStruct GetNextExtraLevelCfg(int level = 0)
        {
            if (Config == null)
            {
                return null;
            }

            if (level <= 0)
            {
                level = Level;
            }

            foreach (var cfg in Config.SlotLevelList)
            {
                if (cfg.Level <= level)
                {
                    continue;
                }

                if (cfg.ExtraAttributes != null && cfg.ExtraAttributes.Count > 0)
                {
                    return cfg;
                }
            }

            return null;
        }

        public int GetFightNum(int level = 0)
        {
            var levelCfg = GetLevelCfg(level);
            return levelCfg != null ? levelCfg.FightNum : 0;
        }
    }

    public class EquipSlotUpgradeResult
    {
        public PlayerEquipSlot EquipSlot { get; set; }
        public ItemGroup Items { get; set; }
    }
}
