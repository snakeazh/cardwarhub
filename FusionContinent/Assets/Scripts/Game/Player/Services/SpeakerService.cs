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
using PlayerCycleSpeakerSkin = FusionContinent.Model.PlayerCycleSpeakerSkin;

namespace Game.Player
{
    /// <summary>
    /// 轮语者皮肤服务：激活、升级、穿戴/卸下，以及属性聚合。
    /// </summary>
    public class SpeakerService : GameServiceBase
    {
        /// <summary>
        /// 穿戴皮肤变更事件（参数：新 SkinId 配置 id）
        /// </summary>
        public System.Action<int> OnWearSkinChanged;

        /// <summary>
        /// 皮肤数据变更（激活/升级成功后，参数：skinId 配置 id）
        /// </summary>
        public System.Action<int> OnSkinDataChanged;

        private static AttributeAggregation _attributeAggregation = new AttributeAggregation();

        /// <summary>
        /// 当前穿戴皮肤对应星级的基础属性加成（不含 Module 类规则属性）。
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
        /// 当前穿戴皮肤 ExtraAttributes 中的 Module 类属性（含 LearnSkillRule），战斗开局走 AttributeManager。
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

            var skinConfigId = GetEquippedSkinConfigId();
            var level = GetSkinLevelByConfigId(skinConfigId);

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

                var stackSourceKey = BuildSkinStackSourceKey(skinConfigId, level, (int)attr.Value);
                entries.Add(new EquipModuleAttributeEntry
                {
                    Attribute = new AttributeData(attr),
                    StackSourceKey = stackSourceKey,
                });
            }

            return entries;
        }

        public static string BuildSkinStackSourceKey(int skinConfigId, int level, int ruleId)
        {
            return "SpeakerSkin_" + skinConfigId + "_" + level + "_" + ruleId;
        }

        private bool TryGetEquippedLevelConfig(out PlayerFigureConfig cfg, out ImageStarListStruct levelCfg)
        {
            cfg = null;
            levelCfg = null;

            var skinConfigId = GetEquippedSkinConfigId();
            if (skinConfigId <= 0 || !IsActivated(skinConfigId))
            {
                return false;
            }

            cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(skinConfigId);
            if (cfg == null)
            {
                return false;
            }

            var level = GetSkinLevelByConfigId(skinConfigId);
            levelCfg = cfg.ImageStarList.FirstOrDefault(v => v.Level == level)
                       ?? cfg.ImageStarList.FirstOrDefault();
            return levelCfg != null;
        }

        /// <summary>
        /// 根据皮肤配置 Id 从 Global.PlayerCycleSpeakerSkins 查找玩家皮肤实例。
        /// </summary>
        public bool TryGetPlayerSkinByConfigId(int skinConfigId, out PlayerCycleSpeakerSkin skin)
        {
            skin = null;
            if (skinConfigId <= 0)
            {
                return false;
            }

            foreach (var kv in Global.PlayerCycleSpeakerSkins)
            {
                if (kv.Value != null && kv.Value.SkinId == skinConfigId)
                {
                    skin = kv.Value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>未激活返回 0。</summary>
        public int GetSkinLevelByConfigId(int skinConfigId)
        {
            if (TryGetPlayerSkinByConfigId(skinConfigId, out var skin))
            {
                return skin.Level;
            }

            return 0;
        }

        public bool IsActivated(int skinConfigId)
        {
            return TryGetPlayerSkinByConfigId(skinConfigId, out _);
        }

        public bool IsEquipped(int skinConfigId)
        {
            return skinConfigId > 0 && Global.UserPlayerSpeaker.UseSkinId == skinConfigId;
        }

        public bool CanUpgrade(int skinConfigId)
        {
            if (!IsActivated(skinConfigId))
            {
                return false;
            }

            var cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(skinConfigId);
            if (cfg == null || !TryGetPlayerSkinByConfigId(skinConfigId, out var skin))
            {
                return false;
            }

            var maxLevel = cfg.ImageStarList.Count > 0
                ? cfg.ImageStarList.Max(v => v.Level)
                : 0;
            return skin.Level < maxLevel;
        }

        public bool CanAffordActivate(int skinConfigId)
        {
            var cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(skinConfigId);
            if (cfg == null)
            {
                return false;
            }

            return CanAffordNeedItems(cfg);
        }

        /// <summary>
        /// 待激活：尚未激活且激活材料足够。
        /// </summary>
        public bool CanActivate(int skinConfigId)
        {
            return !IsActivated(skinConfigId) && CanAffordActivate(skinConfigId);
        }

        /// <summary>
        /// 统计当前可激活的皮肤数量（用于全局红点）。
        /// </summary>
        public int CountActivatableSkins()
        {
            var list = DataConfigManager.Tables.TbPlayerFigureConfig.DataList;
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

        public bool CanAffordUpgrade(int skinConfigId)
        {
            if (!CanUpgrade(skinConfigId))
            {
                return false;
            }

            var cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(skinConfigId);
            if (cfg == null)
            {
                return false;
            }

            return CanAffordNeedItems(cfg);
        }

        private static bool CanAffordNeedItems(PlayerFigureConfig cfg)
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
        public bool TryGetNextUnlockConsume(int skinConfigId, out int itemId, out int needCount)
        {
            itemId = 0;
            needCount = 0;

            var cfg = DataConfigManager.Tables.TbPlayerFigureConfig.GetOrDefault(skinConfigId);
            if (cfg == null || cfg.NeedItems == null || cfg.NeedItems.Count == 0)
            {
                return false;
            }

            if (!IsActivated(skinConfigId))
            {
                itemId = cfg.NeedItems[0].Id;
                needCount = cfg.NeedItems[0].Num;
                return true;
            }

            if (!CanUpgrade(skinConfigId))
            {
                return false;
            }

            itemId = cfg.NeedItems[0].Id;
            needCount = cfg.NeedItems[0].Num;
            return true;
        }

        /// <summary>当前穿戴的皮肤配置 Id，未穿戴返回 0。</summary>
        public int GetEquippedSkinConfigId()
        {
            return Global.UserPlayerSpeaker.UseSkinId;
        }

        /// <summary>已拥有皮肤中的第一个配置 Id，无则返回 0。</summary>
        public int GetFirstOwnedSkinConfigId()
        {
            foreach (var kv in Global.PlayerCycleSpeakerSkins)
            {
                if (kv.Value != null && kv.Value.SkinId > 0)
                {
                    return kv.Value.SkinId;
                }
            }

            return 0;
        }

        public async UniTask<bool> ActivateAsync(int skinConfigId)
        {
            if (skinConfigId <= 0 || !CanAffordActivate(skinConfigId))
            {
                return false;
            }

            try
            {
                var request = new SkinActivateRequest { SkinId = skinConfigId };
                var proto = await WebDataFun.SendProtoPostRequest<SkinActivateResponse>(
                    ApiKeys.ApiSpeakerSkinActivate, request);
                if (proto?.Skin == null)
                {
                    return false;
                }

                ApplySkin(proto.Skin);
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[SpeakerService] ActivateAsync error: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> UpgradeAsync(int skinConfigId)
        {
            if (skinConfigId <= 0 || !CanAffordUpgrade(skinConfigId))
            {
                return false;
            }

            try
            {
                var request = new SkinUpgradeRequest { SkinId = skinConfigId };
                var proto = await WebDataFun.SendProtoPostRequest<SkinUpgradeResponse>(
                    ApiKeys.ApiSpeakerSkinUpgrade, request);
                if (proto?.Skin == null)
                {
                    return false;
                }

                ApplySkin(proto.Skin);
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"[SpeakerService] UpgradeAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 异步穿戴指定皮肤（请求服务器成功后再更新本地）
        /// </summary>
        public async UniTask<SetSkinResult> TryWearSkinAsync(int skinId)
        {
            if (skinId <= 0)
            {
                return SetSkinResult.Fail(skinId, "InvalidSkinId");
            }

            if (!IsActivated(skinId))
            {
                return SetSkinResult.Fail(skinId, "NotOwned");
            }

            if (IsEquipped(skinId))
            {
                return new SetSkinResult { Success = true, SkinId = skinId, Reason = "AlreadyWorn" };
            }

            try
            {
                var request = new SetCycleSpeakerSkinRequest { SkinId = skinId };
                var proto = await WebDataFun.SendProtoPostRequest<SetCycleSpeakerSkinResponse>(
                    ApiKeys.ApiSpeakerSetSkin, request);
                if (proto?.Speaker == null)
                {
                    return SetSkinResult.Fail(skinId, "EmptyResponse");
                }

                ApplySpeakerModule(proto.Speaker);
                if (proto.Items != null)
                {
                    Items.Update(proto.Items);
                }
                var serverSkinId = proto.Speaker.UseSkinId > 0 ? proto.Speaker.UseSkinId : skinId;
                return new SetSkinResult { Success = true, SkinId = serverSkinId };
            }
            catch (System.Exception ex)
            {
                return SetSkinResult.Fail(skinId, ex.Message);
            }
        }

        public async UniTask<bool> UnequipAsync(int skinConfigId)
        {
            if (skinConfigId <= 0)
            {
                return false;
            }

            if (!IsEquipped(skinConfigId))
            {
                return true;
            }

            try
            {
                var request = new SkinUnequipRequest { SkinId = skinConfigId };
                var proto = await WebDataFun.SendProtoPostRequest<SkinUnequipResponse>(
                    ApiKeys.ApiSpeakerSkinUnequip, request);
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
                LogTool.LogError($"[SpeakerService] UnequipAsync error: {ex.Message}");
                return false;
            }
        }

        public void ApplySkinData(IEnumerable<PlayerCycleSpeakerSkin> skins)
        {
            if (skins == null)
            {
                return;
            }

            foreach (var skin in skins)
            {
                if (skin == null || skin.Id <= 0)
                {
                    continue;
                }

                Global.PlayerCycleSpeakerSkins[skin.Id] = new PlayerCycleSpeakerSkin(skin);
                OnSkinDataChanged?.Invoke(skin.SkinId);
            }

            NotifyDataChanged();
        }

        private void ApplySkin(Cardwar.PlayerCycleSpeakerSkin skin)
        {
            var local = ProtoMappers.MapPlayerCycleSpeakerSkin(skin);
            if (local == null)
            {
                return;
            }

            Global.PlayerCycleSpeakerSkins[local.Id] = new PlayerCycleSpeakerSkin(local);
            OnSkinDataChanged?.Invoke(local.SkinId);
            Global.Publisher.Publish(MessagePublisher.SPEAKER_SKIN_DATA_CHANGED, local.SkinId);
            NotifyDataChanged();
        }

        private void ApplySpeakerModule(Cardwar.PlayerSpeakerModule speaker)
        {
            ProtoMappers.ApplySpeakerModule(Global.UserPlayerSpeaker, speaker);
            var skinId = speaker.UseSkinId;
            OnWearSkinChanged?.Invoke(skinId);
            Global.Publisher.Publish(MessagePublisher.SPEAKER_EQUIPPED_SKIN_CHANGED, skinId);
            NotifyDataChanged();
        }
    }

    public class SetSkinResult
    {
        public bool Success { get; set; }
        public int SkinId { get; set; }
        public string Reason { get; set; }

        public static SetSkinResult Fail(int skinId, string reason)
        {
            return new SetSkinResult
            {
                Success = false,
                SkinId = skinId,
                Reason = reason
            };
        }
    }
}
