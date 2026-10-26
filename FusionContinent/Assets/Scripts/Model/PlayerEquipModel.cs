using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Attribute;
using Manager;

namespace FusionContinent.Model
{
    public class PlayerEquipModel
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int EquipId { get; set; } // 装备ID
        public EquipPartType PartType { get; set; } // 部位类型
        public int WearerId { get; set; } // 佩戴者
        public int Phase { get; set; } // 阶段
        public List<int> Entrys { get; set; } = new List<int>(); // 词条
        public List<int> TempEntrys { get; set; } = new List<int>(); // 临时词条
        public List<int> Effects { get; set; } = new List<int>(); // 特效词条
        public List<int> TempEffects { get; set; } = new List<int>(); // 临时特效词条
    }
    
    public class PlayerEquip
    {
        public int Id { get; set; } // 主键
        public int UserId { get; set; } // 用户ID
        public int EquipId { get; set; } // 装备ID
        public QualityType Quality { get; set; } // 稀有度
        public EquipPartType PartType { get; set; } // 部位类型
        public int WearerId { get; set; } // 佩戴者
        public int Phase { get; set; } // 阶段
        public List<int> Entrys { get; set; } // 词条
        public List<int> TempEntrys { get; set; } // 临时词条
        public List<int> Effects { get; set; } // 特效词条
        public List<int> TempEffects { get; set; } // 临时特效词条

        public EquipConfig Config { get; set; } // 装备配置

        public PlayerEquip(PlayerEquipModel v)
        {
            Config = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(v.EquipId);
            if (Config == null)
            {
                throw new Exception($"EquipId[{v.EquipId}], 配置不存在");
            }

            Id = v.Id;
            EquipId = v.EquipId;
            PartType = v.PartType;
            WearerId = v.WearerId;
            Phase = v.Phase;
            Quality = Config.Quality;
            Entrys = v.Entrys != null ? new List<int>(v.Entrys) : new List<int>();
            TempEntrys = v.TempEntrys != null ? new List<int>(v.TempEntrys) : new List<int>();
            Effects = v.Effects != null ? new List<int>(v.Effects) : new List<int>();
            TempEffects = v.TempEffects != null ? new List<int>(v.TempEffects) : new List<int>();
        }
        
    }


    public class EquipModuleAttributeEntry
    {
        public AttributeData Attribute;
        public string StackSourceKey;
        /// <summary>默认装备；融合石佩戴属性用 FusionGem。</summary>
        public AttributeSourceType SourceType = AttributeSourceType.Equip;
    }

    public class Equip
    {
        private static AttributeAggregation _attributeAggregation = new AttributeAggregation();

        /// <summary>
        /// Module 类属性（含 LearnSkillRule）须在战斗开局走 AttributeManager，不在数值聚合阶段写入。
        /// </summary>
        public static bool IsModuleCategoryAttribute(AttributeType type)
        {
            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(type);
            return attrCfg != null && attrCfg.Category == AttributeCategoryType.Module;
        }

        /// <summary>
        /// 获取编队佩戴装备的 Module 类属性（含 LearnSkillRule），带实例级 StackSourceKey。
        /// </summary>
        public static List<EquipModuleAttributeEntry> GetModuleAttributeEntries(List<PlayerEquip> equipList = null)
        {
            var entries = new List<EquipModuleAttributeEntry>();
            equipList = ResolveWearEquipList(equipList);
            if (equipList.Count == 0)
            {
                return entries;
            }

            foreach (var equip in equipList)
            {
                CollectEquipModuleEntries(equip, entries);
            }

            return entries;
        }

        // 获取佩戴装备属性（不含 Module 类，避免与战斗开局 SetAttributesLogic 重复叠加）
        public static AttributeAggregation GetAttributes(List<PlayerEquip> equipList = null)
        {
            equipList = ResolveWearEquipList(equipList);
            if (equipList.Count == 0) return _attributeAggregation;

            _attributeAggregation = new AttributeAggregation();
            CollectEquipAttributeStructs(equipList, attrs =>
            {
                AddNonModuleBaseValues(_attributeAggregation.Attributes, attrs);
            });

            return _attributeAggregation;
        }
        
        
        public static AttributeAggregation GetAttributes(int cardId)
        {
            var currentTeam = Team.GetTeamByTeamId(Talent.GetUseTalent().TalentId);
            var equipList = currentTeam.WearEquipListByCardId(cardId);
            if (equipList.Count == 0) return null;

            _attributeAggregation = new AttributeAggregation();
            foreach (var equip in equipList)
            {
                if (Global.PlayerEquipSlots.TryGetValue(equip.PartType, out var slot))
                {
                    var levelCfg = slot.GetLevelCfg();
                    if (levelCfg != null)
                    {
                        AddNonModuleBaseValues(_attributeAggregation.Attributes, levelCfg.BaseAttributes);
                        AddNonModuleBaseValues(_attributeAggregation.Attributes, levelCfg.ExtraAttributes);
                        _attributeAggregation.SubAttributes.AddRange(levelCfg.BaseAttributes);
                        _attributeAggregation.SubAttributes.AddRange(levelCfg.ExtraAttributes);
                    }
                }

                foreach (var entryId in equip.Entrys)
                {
                    var entryCfg = DataConfigManager.Tables.TbEquipEntryConfig.GetOrDefault(entryId);
                    AddNonModuleBaseValues(_attributeAggregation.Attributes, entryCfg.Attributes);
                    AddNonModuleSubAttributes(_attributeAggregation.SubAttributes, entryCfg.Attributes);
                }

                foreach (var effectId in equip.Effects)
                {
                    var effectCfg = DataConfigManager.Tables.TbEquipEffectsConfig.GetOrDefault(effectId);
                    if (effectCfg == null) continue;

                    AddNonModuleBaseValues(_attributeAggregation.Attributes, effectCfg.Attributes);
                    AddNonModuleSubAttributes(_attributeAggregation.SubAttributes, effectCfg.Attributes);
                }
                            
                var wearGemList = Global.PlayerFusionGems.Values.Where(v => v.WearerId == equip.Id).ToList();
                foreach (var gem in wearGemList)
                {
                    var gemLvCfg = gem.GetLevelCfg();
                    _attributeAggregation.MainAttributes.AddRange(gemLvCfg.MainAttributes);
                    AddNonModuleBaseValues(_attributeAggregation.Attributes, gemLvCfg.Attributes);
                    _attributeAggregation.SubAttributes.AddRange(gemLvCfg.Attributes);
                }
            }

            return _attributeAggregation;
        }

        private static List<PlayerEquip> ResolveWearEquipList(List<PlayerEquip> equipList)
        {
            if (equipList != null)
            {
                return equipList;
            }

            var currentTeam = Team.GetTeamByTeamId(Talent.GetUseTalent().TalentId);
            return currentTeam.WearEquipList();
        }

        private static void CollectEquipModuleEntries(PlayerEquip equip, List<EquipModuleAttributeEntry> entries)
        {
            var entryRuleBonusStackCount = GetEquipEntryEffectBonusStackCount(equip);

            foreach (var entryId in equip.Entrys)
            {
                var entryCfg = DataConfigManager.Tables.TbEquipEntryConfig.GetOrDefault(entryId);
                if (entryCfg == null)
                {
                    continue;
                }

                var stackSourceKey = BuildEquipEntryStackSourceKey(equip.Id, entryId);
                for (int i = 0; i < entryCfg.Attributes.Count; i++)
                {
                    var attr = entryCfg.Attributes[i];
                    if (!IsModuleCategoryAttribute(attr.Type))
                    {
                        continue;
                    }

                    var attributeData = new AttributeData(attr);
                    entries.Add(new EquipModuleAttributeEntry
                    {
                        Attribute = attributeData,
                        StackSourceKey = stackSourceKey,
                    });

                    if (attr.Type == AttributeType.LearnSkillRule && entryRuleBonusStackCount > 0)
                    {
                        AppendEquipEntryLearnSkillRuleBonusStacks(
                            entries,
                            attributeData,
                            equip.Id,
                            entryId,
                            entryRuleBonusStackCount);
                    }
                }
            }

            foreach (var effectId in equip.Effects)
            {
                var effectCfg = DataConfigManager.Tables.TbEquipEffectsConfig.GetOrDefault(effectId);
                if (effectCfg == null)
                {
                    continue;
                }

                var stackSourceKey = BuildEquipEffectStackSourceKey(equip.Id, effectId);
                for (int i = 0; i < effectCfg.Attributes.Count; i++)
                {
                    var attr = effectCfg.Attributes[i];
                    if (!IsModuleCategoryAttribute(attr.Type))
                    {
                        continue;
                    }

                    if (attr.Type == AttributeType.EquipEntryEffectCorrection)
                    {
                        continue;
                    }

                    entries.Add(new EquipModuleAttributeEntry
                    {
                        Attribute = new AttributeData(attr),
                        StackSourceKey = stackSourceKey,
                    });
                }
            }

            CollectFusionGemModuleEntries(equip, entries);
        }

        /// <summary>StackSourceKey：FusionGem_{融合石实例Id}</summary>
        public const string FusionGemStackSourceKeyPrefix = "FusionGem_";

        public static string BuildFusionGemStackSourceKey(int gemInstanceId)
        {
            return FusionGemStackSourceKeyPrefix + gemInstanceId;
        }

        public static bool TryParseFusionGemStackSourceKey(string stackSourceKey, out int gemInstanceId)
        {
            gemInstanceId = 0;
            if (string.IsNullOrEmpty(stackSourceKey)
                || !stackSourceKey.StartsWith(FusionGemStackSourceKeyPrefix))
            {
                return false;
            }

            return int.TryParse(
                stackSourceKey.Substring(FusionGemStackSourceKeyPrefix.Length),
                out gemInstanceId);
        }

        private static void CollectFusionGemModuleEntries(
            PlayerEquip equip,
            List<EquipModuleAttributeEntry> entries)
        {
            foreach (var gem in Global.PlayerFusionGems.Values)
            {
                if (gem == null || gem.WearerId != equip.Id)
                {
                    continue;
                }

                var gemLvCfg = gem.GetLevelCfg();
                if (gemLvCfg == null || gemLvCfg.Attributes == null)
                {
                    continue;
                }

                var stackSourceKey = BuildFusionGemStackSourceKey(gem.Id);
                for (int i = 0; i < gemLvCfg.Attributes.Count; i++)
                {
                    var attr = gemLvCfg.Attributes[i];
                    if (!IsModuleCategoryAttribute(attr.Type))
                    {
                        continue;
                    }

                    entries.Add(new EquipModuleAttributeEntry
                    {
                        Attribute = new AttributeData(attr),
                        StackSourceKey = stackSourceKey,
                        SourceType = AttributeSourceType.FusionGem,
                    });
                }
            }
        }

        /// <summary>
        /// 同件装备特效「装备词条属性效果修正」(126)：Value=1 表示洗练词条 LearnSkillRule 额外叠加 1 次（词条本身不增加）。
        /// </summary>
        public static int GetEquipEntryEffectBonusStackCount(PlayerEquip equip)
        {
            float bonus = 0f;
            for (int i = 0; i < equip.Effects.Count; i++)
            {
                var effectCfg = DataConfigManager.Tables.TbEquipEffectsConfig.GetOrDefault(equip.Effects[i]);
                if (effectCfg == null)
                {
                    continue;
                }

                for (int j = 0; j < effectCfg.Attributes.Count; j++)
                {
                    var attr = effectCfg.Attributes[j];
                    if (attr.Type == AttributeType.EquipEntryEffectCorrection)
                    {
                        bonus += attr.Value;
                    }
                }
            }

            return bonus > 0f ? UnityEngine.Mathf.RoundToInt(bonus) : 0;
        }

        private static void AppendEquipEntryLearnSkillRuleBonusStacks(
            List<EquipModuleAttributeEntry> entries,
            AttributeData attributeData,
            int equipInstanceId,
            int entryId,
            int bonusStackCount)
        {
            for (int bonusIndex = 1; bonusIndex <= bonusStackCount; bonusIndex++)
            {
                entries.Add(new EquipModuleAttributeEntry
                {
                    Attribute = attributeData,
                    StackSourceKey = BuildEquipEntryEffectBonusStackSourceKey(
                        equipInstanceId,
                        entryId,
                        bonusIndex),
                });
            }
        }

        public static string BuildEquipEntryEffectBonusStackSourceKey(
            int equipInstanceId,
            int entryId,
            int bonusIndex)
        {
            return "Equip_" + equipInstanceId + "_" + entryId + "_EntryEffectBonus_" + bonusIndex;
        }

        public static string BuildEquipEntryStackSourceKey(int equipInstanceId, int entryId)
        {
            return "Equip_" + equipInstanceId + "_" + entryId;
        }

        /// <summary>
        /// 解析洗练词条 StackSourceKey：Equip_{实例Id}_{词条Id}（不含 EntryEffectBonus / EquipEffect）。
        /// </summary>
        public static bool TryParseEquipEntryStackSourceKey(
            string stackSourceKey,
            out int equipInstanceId,
            out int entryId)
        {
            equipInstanceId = 0;
            entryId = 0;
            if (string.IsNullOrEmpty(stackSourceKey)
                || !stackSourceKey.StartsWith("Equip_")
                || stackSourceKey.StartsWith("EquipEffect_")
                || stackSourceKey.IndexOf("_EntryEffectBonus_", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            var prefixLength = "Equip_".Length;
            var firstSep = stackSourceKey.IndexOf('_', prefixLength);
            if (firstSep < 0)
            {
                return false;
            }

            var equipPart = stackSourceKey.Substring(prefixLength, firstSep - prefixLength);
            var entryPart = stackSourceKey.Substring(firstSep + 1);
            if (entryPart.IndexOf('_') >= 0)
            {
                return false;
            }

            return int.TryParse(equipPart, out equipInstanceId)
                && int.TryParse(entryPart, out entryId);
        }

        public static string BuildEquipEffectStackSourceKey(int equipInstanceId, int effectId)
        {
            return "EquipEffect_" + equipInstanceId + "_" + effectId;
        }

        private static void CollectEquipAttributeStructs(List<PlayerEquip> equipList, Action<List<AttributeStruct>> onAttributes)
        {
            foreach (var equip in equipList)
            {
                if (Global.PlayerEquipSlots.TryGetValue(equip.PartType, out var slot))
                {
                    var levelCfg = slot.GetLevelCfg();
                    if (levelCfg != null)
                    {
                        onAttributes(levelCfg.BaseAttributes);
                        onAttributes(levelCfg.ExtraAttributes);
                    }
                }

                foreach (var entryId in equip.Entrys)
                {
                    var entryCfg = DataConfigManager.Tables.TbEquipEntryConfig.GetOrDefault(entryId);
                    onAttributes(entryCfg.Attributes);
                }

                foreach (var effectId in equip.Effects)
                {
                    var effectCfg = DataConfigManager.Tables.TbEquipEffectsConfig.GetOrDefault(effectId);
                    if (effectCfg != null)
                    {
                        onAttributes(effectCfg.Attributes);
                    }
                }
            }
        }

        public static void AddNonModuleSubAttributes(List<AttributeStruct> subAttributes, List<AttributeStruct> attrs)
        {
            for (int i = 0; i < attrs.Count; i++)
            {
                var attr = attrs[i];
                if (IsModuleCategoryAttribute(attr.Type))
                {
                    continue;
                }

                subAttributes.Add(attr);
            }
        }

        public static void AddNonModuleBaseValues(AttributeSet attrSet, List<AttributeStruct> attrs, bool isAdditive = true)
        {
            for (int i = 0; i < attrs.Count; i++)
            {
                var attr = attrs[i];
                if (IsModuleCategoryAttribute(attr.Type))
                {
                    continue;
                }

                attrSet.GetAttr(attr.Type).SetBaseValue(attr.Value, isAdditive);
            }
        }
    }

    // 返回数据
    public class UpPhaseResult
    {
        public List<PlayerEquipModel> Equips { get; set; } 
        public ItemGroup Items { get; set; } // 同步道具
    }
    
    public class RefreshEntryResult
    {
        public PlayerEquipModel Equip { get; set; } 
        public ItemGroup Items { get; set; } // 同步道具
    }

    public class UseRefreshEntryResult
    {
        public PlayerEquipModel Equip { get; set; }
    }
    
    // 返回数据
    public class FragmentEquipResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<int> FragmentIds { get; set; } 
        public ItemGroup Items { get; set; } // 同步道具
        public List<RewardItem> Rewards { get; set; } // 奖励内容
    }
    
}