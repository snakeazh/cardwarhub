using System.Collections.Generic;
using System.Linq;
using Cardwar;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Model;
using FusionContinent.Utils;
using Manager;
using UIFramework.Services;
using PlayerWeapon = FusionContinent.Model.PlayerWeapon;

namespace Game.Player
{
    /// <summary>
    /// 轮语者武器服务：激活、升级、穿戴/卸下。
    /// </summary>
    public class WeaponService : GameServiceBase
    {
        public bool IsInitialized { get; private set; }

        private static AttributeAggregation _attributeAggregation = new AttributeAggregation();

        /// <summary>
        /// 当前穿戴武器对应星级的基础属性加成（不含 Module 类规则属性）。
        /// </summary>
        public AttributeAggregation GetAttributes()
        {
            _attributeAggregation = new AttributeAggregation();

            if (!TryGetEquippedLevelConfig(out _, out var levelCfg))
            {
                return _attributeAggregation;
            }

            if (levelCfg.BaseAttributes != null && levelCfg.BaseAttributes.Count > 0)
            {
                FusionContinent.Model.Equip.AddNonModuleBaseValues(_attributeAggregation.Attributes, levelCfg.BaseAttributes);
                FusionContinent.Model.Equip.AddNonModuleSubAttributes(_attributeAggregation.SubAttributes, levelCfg.BaseAttributes);
            }

            if (levelCfg.ExtraAttributes != null && levelCfg.ExtraAttributes.Count > 0)
            {
                FusionContinent.Model.Equip.AddNonModuleBaseValues(_attributeAggregation.Attributes, levelCfg.ExtraAttributes);
                FusionContinent.Model.Equip.AddNonModuleSubAttributes(_attributeAggregation.SubAttributes, levelCfg.ExtraAttributes);
            }

            return _attributeAggregation;
        }

        /// <summary>
        /// 当前穿戴武器 ExtraAttributes 中的 Module 类属性（含 LearnSkillRule），战斗开局走 AttributeManager。
        /// </summary>
        public List<EquipModuleAttributeEntry> GetModuleAttributeEntries()
        {
            var entries = new List<EquipModuleAttributeEntry>();
            if (!TryGetEquippedLevelConfig(out _, out var levelCfg)
                || levelCfg.ExtraAttributes == null
                || levelCfg.ExtraAttributes.Count == 0)
            {
                return entries;
            }

            var weaponConfigId = GetEquippedWeaponConfigId();
            var level = GetWeaponLevelByConfigId(weaponConfigId);

            for (int i = 0; i < levelCfg.ExtraAttributes.Count; i++)
            {
                var attr = levelCfg.ExtraAttributes[i];
                if (!FusionContinent.Model.Equip.IsModuleCategoryAttribute(attr.Type))
                {
                    continue;
                }

                if (attr.Type == AttributeType.EquipEntryEffectCorrection)
                {
                    continue;
                }

                var stackSourceKey = BuildWeaponStackSourceKey(weaponConfigId, level, (int)attr.Value);
                entries.Add(new EquipModuleAttributeEntry
                {
                    Attribute = new AttributeData(attr),
                    StackSourceKey = stackSourceKey,
                });
            }

            return entries;
        }

        public static string BuildWeaponStackSourceKey(int weaponConfigId, int level, int ruleId)
        {
            return "SpeakerWeapon_" + weaponConfigId + "_" + level + "_" + ruleId;
        }

        private bool TryGetEquippedLevelConfig(out PlayerWeaponConfig cfg, out WeaponStarListStruct levelCfg)
        {
            cfg = null;
            levelCfg = null;

            var weaponConfigId = GetEquippedWeaponConfigId();
            if (weaponConfigId <= 0 || !IsActivated(weaponConfigId))
            {
                return false;
            }

            cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(weaponConfigId);
            if (cfg == null)
            {
                return false;
            }

            var level = GetWeaponLevelByConfigId(weaponConfigId);
            levelCfg = cfg.WeaponStarList.FirstOrDefault(v => v.Level == level)
                       ?? cfg.WeaponStarList.FirstOrDefault();
            return levelCfg != null;
        }

        /// <summary>
        /// 武器数据变更（激活/升级成功后，参数：weaponId 配置 id）
        /// </summary>
        public System.Action<int> OnWeaponDataChanged;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            IsInitialized = true;
            LogTool.Log("[WeaponService] 初始化完成");
        }

        /// <summary>
        /// 根据 <see cref="PlayerWeaponConfig.Id"/> 从 Global.PlayerWeapons 查找玩家武器实例。
        /// </summary>
        public bool TryGetPlayerWeaponByConfigId(int weaponConfigId, out PlayerWeapon weapon)
        {
            weapon = null;
            if (weaponConfigId <= 0)
            {
                return false;
            }

            foreach (var kv in Global.PlayerWeapons)
            {
                if (kv.Value != null && kv.Value.WeaponId == weaponConfigId)
                {
                    weapon = kv.Value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>未激活返回 0。</summary>
        public int GetWeaponLevelByConfigId(int weaponConfigId)
        {
            if (TryGetPlayerWeaponByConfigId(weaponConfigId, out var weapon))
            {
                return weapon.Level;
            }

            return 0;
        }

        /// <summary>当前穿戴的武器配置 Id，未穿戴返回 0。</summary>
        public int GetEquippedWeaponConfigId()
        {
            return Global.UserPlayerSpeaker.EquippedWeaponId;
        }

        /// <summary>已拥有武器中的第一个配置 Id，无则返回 0。</summary>
        public int GetFirstOwnedWeaponConfigId()
        {
            foreach (var kv in Global.PlayerWeapons)
            {
                if (kv.Value != null && kv.Value.WeaponId > 0)
                {
                    return kv.Value.WeaponId;
                }
            }

            return 0;
        }

        public bool IsActivated(int weaponConfigId)
        {
            return TryGetPlayerWeaponByConfigId(weaponConfigId, out _);
        }

        public bool IsEquipped(int weaponConfigId)
        {
            return weaponConfigId > 0 && Global.UserPlayerSpeaker.EquippedWeaponId == weaponConfigId;
        }

        public bool CanUpgrade(int weaponConfigId)
        {
            if (!IsActivated(weaponConfigId))
            {
                return false;
            }

            var cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(weaponConfigId);
            if (cfg == null || !TryGetPlayerWeaponByConfigId(weaponConfigId, out var weapon))
            {
                return false;
            }

            var maxLevel = cfg.WeaponStarList.Count > 0
                ? cfg.WeaponStarList.Max(v => v.Level)
                : 0;
            return weapon.Level < maxLevel;
        }

        public bool CanAffordActivate(int weaponConfigId)
        {
            var cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(weaponConfigId);
            if (cfg == null)
            {
                return false;
            }

            return CanAffordNeedItems(cfg);
        }

        /// <summary>
        /// 待激活：尚未激活且激活材料足够。
        /// </summary>
        public bool CanActivate(int weaponConfigId)
        {
            return !IsActivated(weaponConfigId) && CanAffordActivate(weaponConfigId);
        }

        /// <summary>
        /// 统计当前可激活的武器数量（用于全局红点）。
        /// </summary>
        public int CountActivatableWeapons()
        {
            var list = DataConfigManager.Tables.TbPlayerWeaponConfig.DataList;
            if (list == null || list.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (CanActivate(list[i].Id))
                {
                    count++;
                }
            }

            return count;
        }

        public bool CanAffordUpgrade(int weaponConfigId)
        {
            if (!CanUpgrade(weaponConfigId))
            {
                return false;
            }

            var cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(weaponConfigId);
            if (cfg == null)
            {
                return false;
            }

            return CanAffordNeedItems(cfg);
        }

        /// <summary>
        /// 可养成：已激活、未满星且升星材料足够。
        /// </summary>
        public bool CanCultivate(int weaponConfigId)
        {
            return CanAffordUpgrade(weaponConfigId);
        }

        /// <summary>
        /// 统计当前可养成的武器数量（用于全局红点）。
        /// </summary>
        public int CountCultivatableWeapons()
        {
            var list = DataConfigManager.Tables.TbPlayerWeaponConfig.DataList;
            if (list == null || list.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (CanCultivate(list[i].Id))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>锻造按钮是否可交互：未激活可尝试激活，已激活且未满星可升星（不校验材料）。</summary>
        public bool CanForge(int weaponConfigId)
        {
            if (!IsActivated(weaponConfigId))
            {
                return DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(weaponConfigId) != null;
            }

            return CanUpgrade(weaponConfigId);
        }

        private static bool CanAffordNeedItems(PlayerWeaponConfig cfg)
        {
            if (cfg.NeedItems == null || cfg.NeedItems.Count == 0)
            {
                return true;
            }

            foreach (var item in cfg.NeedItems)
            {
                if (Global.GetItemNumById(item.Id) < item.Num)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取下一级解锁（激活或升星）消耗；已满级或无消耗配置时返回 false。
        /// </summary>
        public bool TryGetNextUnlockConsume(int weaponConfigId, out int itemId, out int needCount)
        {
            itemId = 0;
            needCount = 0;

            var cfg = DataConfigManager.Tables.TbPlayerWeaponConfig.GetOrDefault(weaponConfigId);
            if (cfg == null || cfg.NeedItems == null || cfg.NeedItems.Count == 0)
            {
                return false;
            }

            if (!IsActivated(weaponConfigId))
            {
                itemId = cfg.NeedItems[0].Id;
                needCount = cfg.NeedItems[0].Num;
                return true;
            }

            if (!CanUpgrade(weaponConfigId))
            {
                return false;
            }

            itemId = cfg.NeedItems[0].Id;
            needCount = cfg.NeedItems[0].Num;
            return true;
        }

        public async UniTask<bool> ActivateAsync(int weaponConfigId)
        {
            if (weaponConfigId <= 0 || !CanAffordActivate(weaponConfigId))
            {
                return false;
            }

            try
            {
                var request = new WeaponActivateRequest { WeaponId = weaponConfigId };
                var proto = await WebDataFun.SendProtoPostRequest<WeaponActivateResponse>(
                    ApiKeys.ApiSpeakerWeaponActivate, request);
                if (proto?.Weapon == null)
                {
                    return false;
                }

                ApplyWeapon(proto.Weapon);
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[WeaponService] ActivateAsync error: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> UpgradeAsync(int weaponConfigId)
        {
            if (weaponConfigId <= 0 || !CanAffordUpgrade(weaponConfigId))
            {
                return false;
            }

            try
            {
                var request = new WeaponUpgradeRequest { WeaponId = weaponConfigId };
                var proto = await WebDataFun.SendProtoPostRequest<WeaponUpgradeResponse>(
                    ApiKeys.ApiSpeakerWeaponUpgrade, request);
                if (proto?.Weapon == null)
                {
                    return false;
                }

                ApplyWeapon(proto.Weapon);
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[WeaponService] UpgradeAsync error: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> EquipAsync(int weaponConfigId)
        {
            if (weaponConfigId <= 0 || !IsActivated(weaponConfigId))
            {
                return false;
            }

            if (IsEquipped(weaponConfigId))
            {
                return true;
            }

            try
            {
                var request = new WeaponEquipRequest { WeaponId = weaponConfigId };
                var proto = await WebDataFun.SendProtoPostRequest<WeaponEquipResponse>(
                    ApiKeys.ApiSpeakerWeaponEquip, request);
                if (proto?.Speaker == null)
                {
                    return false;
                }

                ApplySpeakerModule(proto.Speaker);
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[WeaponService] EquipAsync error: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> UnequipAsync(int weaponConfigId)
        {
            if (weaponConfigId <= 0)
            {
                return false;
            }

            if (!IsEquipped(weaponConfigId))
            {
                return true;
            }

            try
            {
                var request = new WeaponUnequipRequest { WeaponId = weaponConfigId };
                var proto = await WebDataFun.SendProtoPostRequest<WeaponUnequipResponse>(
                    ApiKeys.ApiSpeakerWeaponUnequip, request);
                if (proto?.Speaker == null)
                {
                    return false;
                }

                ApplySpeakerModule(proto.Speaker);
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[WeaponService] UnequipAsync error: {ex.Message}");
                return false;
            }
        }

        public void ApplyWeaponData(IEnumerable<PlayerWeapon> weapons)
        {
            if (weapons == null)
            {
                return;
            }

            foreach (var weapon in weapons)
            {
                if (weapon == null || weapon.Id <= 0)
                {
                    continue;
                }

                Global.PlayerWeapons[weapon.Id] = new PlayerWeapon(weapon);
                OnWeaponDataChanged?.Invoke(weapon.WeaponId);
            }

            NotifyDataChanged();
        }

        private void ApplyWeapon(Cardwar.PlayerWeapon weapon)
        {
            var local = ProtoMappers.MapPlayerWeapon(weapon);
            if (local == null)
            {
                return;
            }

            Global.PlayerWeapons[local.Id] = new PlayerWeapon(local);
            OnWeaponDataChanged?.Invoke(local.WeaponId);
            Global.Publisher.Publish(MessagePublisher.WEAPON_DATA_CHANGED, local.WeaponId);
            NotifyDataChanged();
        }

        private void ApplySpeakerModule(Cardwar.PlayerSpeakerModule speaker)
        {
            ProtoMappers.ApplySpeakerModule(Global.UserPlayerSpeaker, speaker);
            Global.Publisher.Publish(MessagePublisher.SPEAKER_EQUIPPED_WEAPON_CHANGED, speaker.EquippedWeaponId);
            NotifyDataChanged();
        }
    }
}
