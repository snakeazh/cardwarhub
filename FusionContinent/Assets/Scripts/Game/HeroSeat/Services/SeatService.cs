using System;
using System.Collections.Generic;
using Cardwar;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Model;
using FusionContinent.Utils;
using UIFramework.Services;
using PlayerCityWall = FusionContinent.Model.PlayerCityWall;
using PlayerCityWallSkill = FusionContinent.Model.PlayerCityWallSkill;
using PlayerCityWallArchive = FusionContinent.Model.PlayerCityWallArchive;

namespace Game.HeroSeat
{
    /// <summary>
    /// 城墙/备战席服务：统一管理 Global.PlayerCityWall 数据，并提供协议与业务查询接口。
    /// </summary>
    public class SeatService : GameServiceBase
    {
        public bool IsInitialized { get; private set; }

        public PlayerCityWallArchive Archive => Global.PlayerCityWall;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            IsInitialized = true;
            LogTool.Log("[SeatService] 初始化完成");
        }

        public HeroSeatConfig GetSkillConfig(int skillId)
        {
            return DataConfigManager.Tables.TbHeroSeatConfig.GetOrDefault(skillId);
        }

        public HeroSeatLevelConfig GetWallLevelCfg(int level = 0)
        {
            if (level <= 0)
            {
                level = Archive.Wall.Level;
            }

            return DataConfigManager.Tables.TbHeroSeatLevelConfig.GetOrDefault(level);
        }

        public bool IsWallMaxLevel()
        {
            return GetWallLevelCfg(Archive.Wall.Level + 1) == null;
        }

        /// <summary>
        /// 获取当前城墙等级升到下一级所需的消耗（当前等级 HeroSeatLevelConfig.NeedItems）。
        /// </summary>
        public bool TryGetWallUpgradeConsume(out int itemId, out int needCount)
        {
            itemId = 0;
            needCount = 0;

            if (IsWallMaxLevel())
            {
                return false;
            }

            var levelCfg = GetWallLevelCfg();
            if (levelCfg == null || levelCfg.NeedItems == null || levelCfg.NeedItems.Count == 0)
            {
                return false;
            }

            var consumeItem = levelCfg.NeedItems[0];
            itemId = consumeItem.Id;
            needCount = consumeItem.Num;
            return itemId > 0 && needCount > 0;
        }

        public SeatLevelListStruct GetSkillLevelCfg(int skillId, int level = 0)
        {
            var config = GetSkillConfig(skillId);
            if (config == null)
            {
                return null;
            }

            if (level <= 0)
            {
                var skill = GetSkill(skillId);
                level = skill is { Level: > 0 } ? skill.Level : 1;
            }

            foreach (var cfg in config.SeatLevelList)
            {
                if (cfg.Level == level)
                {
                    return cfg;
                }
            }

            return null;
        }

        public int GetSkillFightNum(int skillId, int level = 0)
        {
            var levelCfg = GetSkillLevelCfg(skillId, level);
            return levelCfg != null ? levelCfg.FightNum : 0;
        }

        /// <summary>
        /// 当前城墙等级对应的基础属性加成。
        /// </summary>
        public AttributeAggregation GetWallAttributes()
        {
            var aggregation = new AttributeAggregation();
            var wallLevelCfg = GetWallLevelCfg();
            AppendSeatAttributes(aggregation, wallLevelCfg?.BaseAttributes);
            return aggregation;
        }

        /// <summary>
        /// 已装备到城墙槽位上的技能，按当前等级汇总属性加成。
        /// </summary>
        public AttributeAggregation GetEquippedSlotSkillAttributes()
        {
            var aggregation = new AttributeAggregation();
            var slotCount = GetSeatSlotCount();
            EnsureSlotSkillsSize(Archive.SlotSkills, slotCount);

            for (var i = 0; i < slotCount; i++)
            {
                if (!IsSeatSlotUnlocked(i))
                {
                    continue;
                }

                var skillId = GetSlotSkillId(i);
                if (skillId <= 0 || !IsSkillActivated(skillId))
                {
                    continue;
                }

                var levelCfg = GetSkillLevelCfg(skillId);
                AppendSeatAttributes(aggregation, levelCfg?.BaseAttributes);
                AppendSeatAttributes(aggregation, levelCfg?.ExtraAttributes);
            }

            return aggregation;
        }

        private static void AppendSeatAttributes(AttributeAggregation aggregation, List<AttributeStruct> attributes)
        {
            if (attributes == null || attributes.Count == 0)
            {
                return;
            }

            FusionContinent.Model.Equip.AddNonModuleBaseValues(aggregation.Attributes, attributes);
            FusionContinent.Model.Equip.AddNonModuleSubAttributes(aggregation.SubAttributes, attributes);
        }

        public void ApplyArchive(PlayerCityWallArchive archive)
        {
            if (archive == null)
            {
                Global.PlayerCityWall = new PlayerCityWallArchive();
                return;
            }

            Global.PlayerCityWall = new PlayerCityWallArchive
            {
                Wall = new PlayerCityWall { Level = archive.Wall.Level },
                Skills = new Dictionary<int, PlayerCityWallSkill>(archive.Skills),
                SlotSkills = new List<int>(archive.SlotSkills),
            };
        }

        public PlayerCityWallSkill GetSkill(int skillId)
        {
            Archive.Skills.TryGetValue(skillId, out var skill);
            return skill;
        }

        public bool IsSkillActivated(int skillId)
        {
            return GetSkill(skillId) is { Level: > 0 };
        }

        public int GetSlotSkillId(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Archive.SlotSkills.Count)
            {
                return 0;
            }

            return Archive.SlotSkills[slotIndex];
        }

        /// <summary>
        /// 城墙技能槽位是否已解锁。槽位 slotIndex 对应第 slotIndex + 1 条 UnLockSeat &gt; 0 的等级配置。
        /// </summary>
        public bool IsSeatSlotUnlocked(int slotIndex)
        {
            return Archive.Wall.Level >= GetSeatSlotUnlockLevel(slotIndex);
        }

        /// <summary>
        /// 获取指定槽位解锁所需的城墙等级（slotIndex 对应第 slotIndex + 1 条 UnLockSeat &gt; 0 的等级配置）。
        /// </summary>
        public int GetSeatSlotUnlockLevel(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return int.MaxValue;
            }

            var unlockIndex = 0;
            foreach (var cfg in DataConfigManager.Tables.TbHeroSeatLevelConfig.DataList)
            {
                if (cfg.UnLockSeat <= 0)
                {
                    continue;
                }

                if (unlockIndex == slotIndex)
                {
                    return cfg.Level;
                }

                unlockIndex++;
            }

            return int.MaxValue;
        }

        public int GetSeatSlotCount()
        {
            var count = 0;
            foreach (var cfg in DataConfigManager.Tables.TbHeroSeatLevelConfig.DataList)
            {
                if (cfg.UnLockSeat > 0)
                {
                    count++;
                }
            }

            return count > 0 ? count : 1;
        }

        public int FindEquippedSlotIndex(int skillId)
        {
            for (var i = 0; i < Archive.SlotSkills.Count; i++)
            {
                if (Archive.SlotSkills[i] == skillId)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsSkillEquipped(int skillId)
        {
            return FindEquippedSlotIndex(skillId) >= 0;
        }

        public bool HasEmptyUnlockedSlot()
        {
            return TryGetFirstEmptyUnlockedSlot(out _);
        }

        public bool TryGetFirstEmptyUnlockedSlot(out int slotIndex)
        {
            slotIndex = -1;
            var slotCount = GetSeatSlotCount();
            EnsureSlotSkillsSize(Archive.SlotSkills, slotCount);

            for (var i = 0; i < slotCount; i++)
            {
                if (!IsSeatSlotUnlocked(i) || GetSlotSkillId(i) > 0)
                {
                    continue;
                }

                slotIndex = i;
                return true;
            }

            return false;
        }

        public bool IsSkillMaxLevel(int skillId)
        {
            var skill = GetSkill(skillId);
            if (skill == null || skill.Level <= 0)
            {
                return false;
            }

            return GetSkillLevelCfg(skillId, skill.Level + 1) == null;
        }

        public bool TryGetSkillActivateConsume(int skillId, out int itemId, out int needCount)
        {
            itemId = 0;
            needCount = 0;

            if (IsSkillActivated(skillId))
            {
                return false;
            }

            var config = GetSkillConfig(skillId);
            if (config?.NeedItems == null || config.NeedItems.Count == 0)
            {
                return false;
            }

            var consumeItem = config.NeedItems[0];
            itemId = consumeItem.Id;
            needCount = consumeItem.Num;
            return itemId > 0 && needCount > 0;
        }

        public bool TryGetSkillUpgradeConsume(int skillId, out int itemId, out int needCount)
        {
            itemId = 0;
            needCount = 0;

            if (!IsSkillActivated(skillId) || IsSkillMaxLevel(skillId))
            {
                return false;
            }

            var levelCfg = GetSkillLevelCfg(skillId);
            if (levelCfg?.LevelUpItems == null || levelCfg.LevelUpItems.Count == 0)
            {
                return false;
            }

            var consumeItem = levelCfg.LevelUpItems[0];
            itemId = consumeItem.Id;
            needCount = consumeItem.Num;
            return itemId > 0 && needCount > 0;
        }

        /// <summary>
        /// 待激活：尚未激活且激活材料足够。
        /// </summary>
        public bool CanActivate(int skillId)
        {
            if (!TryGetSkillActivateConsume(skillId, out int itemId, out int needCount))
            {
                return false;
            }

            return Global.GetItemNumById(itemId) >= needCount;
        }

        /// <summary>
        /// 可升级：已激活、未满级且升级材料足够。
        /// </summary>
        public bool CanUpgrade(int skillId)
        {
            if (!TryGetSkillUpgradeConsume(skillId, out int itemId, out int needCount))
            {
                return false;
            }

            return Global.GetItemNumById(itemId) >= needCount;
        }

        /// <summary>
        /// 可装备（使用）：已激活、未装备且有空解锁槽位。
        /// </summary>
        public bool CanWear(int skillId)
        {
            return IsSkillActivated(skillId) && !IsSkillEquipped(skillId) && HasEmptyUnlockedSlot();
        }

        /// <summary>
        /// 城墙可升级：未满级且升级材料足够。
        /// </summary>
        public bool CanUpgradeWall()
        {
            if (!TryGetWallUpgradeConsume(out int itemId, out int needCount))
            {
                return false;
            }

            return Global.GetItemNumById(itemId) >= needCount;
        }

        /// <summary>
        /// 列表项是否应亮红点（可激活 / 可升级；不含可装备）。
        /// </summary>
        public bool HasSkillActionRedDot(int skillId)
        {
            return CanActivate(skillId) || CanUpgrade(skillId);
        }

        public int CountActivatableSkills()
        {
            return CountSkillsMatching(CanActivate);
        }

        public int CountUpgradableSkills()
        {
            return CountSkillsMatching(CanUpgrade);
        }

        int CountSkillsMatching(System.Func<int, bool> predicate)
        {
            var list = DataConfigManager.Tables.TbHeroSeatConfig.DataList;
            if (list == null || list.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i].Id))
                {
                    count++;
                }
            }

            return count;
        }

        public async UniTask<bool> WearSkillAsync(int skillId)
        {
            if (!IsSkillActivated(skillId))
            {
                return false;
            }

            if (IsSkillEquipped(skillId))
            {
                return true;
            }

            if (!TryGetFirstEmptyUnlockedSlot(out var slotIndex))
            {
                Toast.Show("没有可装备的槽位");
                return false;
            }

            var slotSkills = new List<int>(Archive.SlotSkills);
            EnsureSlotSkillsSize(slotSkills, GetSeatSlotCount());
            slotSkills[slotIndex] = skillId;
            return await SaveSlotSkillsAsync(slotSkills);
        }

        public async UniTask<bool> UnWearSkillAsync(int skillId)
        {
            var slotIndex = FindEquippedSlotIndex(skillId);
            if (slotIndex < 0)
            {
                return true;
            }

            var slotSkills = new List<int>(Archive.SlotSkills);
            EnsureSlotSkillsSize(slotSkills, GetSeatSlotCount());
            slotSkills[slotIndex] = 0;
            return await SaveSlotSkillsAsync(slotSkills);
        }

        private static void EnsureSlotSkillsSize(IList<int> slotSkills, int slotCount)
        {
            while (slotSkills.Count < slotCount)
            {
                slotSkills.Add(0);
            }
        }

        public async UniTask<bool> UpgradeWallAsync()
        {
            try
            {
                var request = new CityWallUpgradeRequest();
                var proto = await WebDataFun.SendProtoPostRequest<CityWallUpgradeResponse>(
                    ApiKeys.ApiCityWallUpgrade, request);
                if (proto == null || proto.CityWall == null)
                {
                    return false;
                }

                Archive.Wall = ProtoMappers.MapPlayerCityWall(proto.CityWall);
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                Global.Publisher.Publish(MessagePublisher.CITY_WALL_DATA_CHANGED, null);
                return true;
            }
            catch (Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[SeatService] UpgradeWallAsync error: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> ActivateSkillAsync(int skillId)
        {
            try
            {
                var request = new CityWallSkillActivateRequest { SkillId = skillId };
                var proto = await WebDataFun.SendProtoPostRequest<CityWallSkillActivateResponse>(
                    ApiKeys.ApiCityWallSkillActivate, request);
                if (proto == null || proto.Skill == null)
                {
                    return false;
                }

                ApplySkillResponse(proto.Skill, proto.Items);
                Global.Publisher.Publish(MessagePublisher.CITY_WALL_DATA_CHANGED, skillId);
                return true;
            }
            catch (Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[SeatService] ActivateSkillAsync error: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> UpgradeSkillAsync(int skillId)
        {
            try
            {
                var request = new CityWallSkillUpgradeRequest { SkillId = skillId };
                var proto = await WebDataFun.SendProtoPostRequest<CityWallSkillUpgradeResponse>(
                    ApiKeys.ApiCityWallSkillUpgrade, request);
                if (proto == null || proto.Skill == null)
                {
                    return false;
                }

                ApplySkillResponse(proto.Skill, proto.Items);
                Global.Publisher.Publish(MessagePublisher.CITY_WALL_DATA_CHANGED, skillId);
                return true;
            }
            catch (Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[SeatService] UpgradeSkillAsync error: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> SaveSlotSkillsAsync(IList<int> slotSkills)
        {
            try
            {
                var request = new CityWallSaveRequest();
                request.SlotSkills.AddRange(slotSkills);
                var proto = await WebDataFun.SendProtoPostRequest<CityWallSaveResponse>(
                    ApiKeys.ApiCityWallSave, request);
                if (proto == null)
                {
                    return false;
                }

                Archive.SlotSkills.Clear();
                Archive.SlotSkills.AddRange(proto.SlotSkills);
                
                Items.Update(proto.Items);
                Global.Publisher.Publish(MessagePublisher.CITY_WALL_DATA_CHANGED, null);
                return true;
            }
            catch (Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[SeatService] SaveSlotSkillsAsync error: {ex.Message}");
                return false;
            }
        }

        private void ApplySkillResponse(Cardwar.PlayerCityWallSkill skill, ResultItems items)
        {
            var local = ProtoMappers.MapPlayerCityWallSkill(skill);
            if (local == null)
            {
                return;
            }

            Archive.Skills[local.SkillId] = local;
            if (items != null)
            {
                Items.Update(items);
            }
        }
    }
}
