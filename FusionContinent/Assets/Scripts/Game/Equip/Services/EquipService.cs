using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cardwar;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using Newtonsoft.Json;
using Config;
using PlayerEquip = FusionContinent.Model.PlayerEquip;
using PlayerEquipSlot = FusionContinent.Model.PlayerEquipSlot;
using PlayerFusionGem = FusionContinent.Model.PlayerFusionGem;


/// <summary>
/// 装备槽位数据结构（与原界面 EquipCardSlotData 一致）
/// </summary>
public class EquipCardSlotData
{
    public GameObject SlotObj { get; set; }
    public int EquipOnlyId { get; set; }
    public EquipPartType PartType { get; set; }
    public PlayerFusionGem FusionGem { get; set; }
    public bool LockFusionGem { get; set; }
}
namespace Game.Equip
{
    /// <summary>
    /// 装备服务 - 管理装备和融合石数据
    /// </summary>
    public class EquipService : GameServiceBase
    {
        #region 属性
        public bool IsInitialized { get; private set; }
        #endregion

        #region 配置数据
        private TbEquipConfig mEquipCardConfig;
        private TbGemFusionConfig mGemFusionConfig;
        /// <summary>装备配置 Id → 士兵装备道具配置（Item.EffectValue == Equip.Id）</summary>
        private Dictionary<int, ItemConfig> mEquipIdToSoldierEquipItem;
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            
            mEquipCardConfig = DataConfigManager.Tables.TbEquipConfig;
            mGemFusionConfig = DataConfigManager.Tables.TbGemFusionConfig;
            BuildSoldierEquipItemMap();
            
            IsInitialized = true;
            LogTool.Log("[EquipService] 初始化完成");
        }

        private void BuildSoldierEquipItemMap()
        {
            mEquipIdToSoldierEquipItem = new Dictionary<int, ItemConfig>();
            var itemList = DataConfigManager.Tables.TbItemConfig.DataList;
            for (int i = 0; i < itemList.Count; i++)
            {
                var itemCfg = itemList[i];
                if (itemCfg.Type != ItemType.SoldierEquip || itemCfg.EffectValue <= 0)
                {
                    continue;
                }

                mEquipIdToSoldierEquipItem[itemCfg.EffectValue] = itemCfg;
            }
        }

        /// <summary>
        /// 用装备配置 Id 查对应士兵装备道具（Item.EffectValue == Equip.Id）。
        /// </summary>
        public ItemConfig GetSoldierEquipItemByEquipId(int equipId)
        {
            if (mEquipIdToSoldierEquipItem == null)
            {
                return null;
            }

            ItemConfig itemCfg;
            if (mEquipIdToSoldierEquipItem.TryGetValue(equipId, out itemCfg))
            {
                return itemCfg;
            }

            return null;
        }

        #region 装备操作

        /// <summary>
        /// 穿戴装备
        /// </summary>
        public async UniTask<EquipResult> WearEquipAsync(int soldierId, EquipPartType partType)
        {
            try
            {
                // 该接口走 Proto：WearEquipRequest/WearEquipResponse
                // 注意：服务端字段为 Id / WearerId；此处沿用原接口语义（SoldierId + PartType）
                var request = new WearEquipRequest { Id = soldierId, WearerId = (int)partType };
                var proto = await WebDataFun.SendProtoPostRequest<WearEquipResponse>(ApiKeys.ApiEquipWear, request);
                if (proto != null)
                {
                    var equips = new List<PlayerEquip>();
                    if (proto.Equips != null)
                    {
                        foreach (var e in proto.Equips)
                        {
                            var model = ProtoMappers.MapPlayerEquip(e);
                            if (model != null)
                            {
                                equips.Add(new PlayerEquip(model));
                            }
                        }
                    }

                    RefreshEquipData(equips);
                    Items.Update(proto.Items);
                    return new EquipResult
                    {
                        Success = true,
                        Message = "穿戴成功"
                    };
                }

                return new EquipResult { Success = false, Message = "穿戴失败" };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipService] 穿戴装备失败: {ex.Message}");
                return new EquipResult { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 装备详情穿戴/替换：ApiEquipWear 使用 Id + WearerId。
        /// </summary>
        public async UniTask<bool> WearEquipByDetailApiAsync(int equipOnlyId, int wearerId)
        {
            try
            {
                var request = new WearEquipRequest { Id = equipOnlyId, WearerId = wearerId };
                var proto = await WebDataFun.SendProtoPostRequest<WearEquipResponse>(ApiKeys.ApiEquipWear, request);
                if (proto == null)
                {
                    LogTool.LogWarning("[EquipService] WearEquipByDetailApiAsync 收到空响应");
                    return false;
                }

                if (proto.Equips != null)
                {
                    var equips = new List<PlayerEquip>();
                    foreach (var e in proto.Equips)
                    {
                        var model = ProtoMappers.MapPlayerEquip(e);
                        if (model != null)
                        {
                            equips.Add(new PlayerEquip(model));
                        }
                    }

                    RefreshEquipData(equips);
                }
                Items.Update(proto.Items);
                Global.Publisher.Publish(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, wearerId);
                return true;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipService] WearEquipByDetailApiAsync 失败: {ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
                return false;
            }
        }

        
        #endregion

        #region 融合石操作

        public async UniTask<EquipResult> WearFusionGemAsync(int gemId, int equipId)
        {
            try
            {
                var request = new WearFusionGemV2Request { Id = gemId, WearerId = equipId };
                var proto = await WebDataFun.SendProtoPostRequest<WearFusionGemV2Response>(ApiKeys.ApiFusionGemWear, request);
                if (proto != null)
                {
                    var gems = new List<PlayerFusionGem>();
                    if (proto.Gems != null)
                    {
                        foreach (var g in proto.Gems)
                        {
                            gems.Add(new PlayerFusionGem(new PlayerFusionGem
                            {
                                Id = g.Id,
                                GemId = g.GemId,
                                WearerId = g.WearerId,
                                Level = g.Level
                            }));
                        }
                    }

                    RefreshFusionGemData(gems);
                    Items.Update(proto.Items);
                    return new EquipResult
                    {
                        Success = true,
                        Message = "成功",
                        PlayerFusionGems = gems
                    };
                }

                return new EquipResult { Success = false, Message = "失败" };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipService] 穿戴融合石失败: {ex.Message}");
                return new EquipResult { Success = false, Message = ex.Message };
            }
        }


        #endregion

        #region 装备分解

        /// <summary>
        /// 分解装备
        /// </summary>
        public async UniTask<FragmentEquipResult> FragmentEquipAsync(List<int> equipIds)
        {
            try
            {
                var request = new FragmentEquipRequest();
                request.Ids.AddRange(equipIds);

                var proto = await WebDataFun.SendProtoPostRequest<FragmentEquipResponse>(ApiKeys.ApiEquipFragment, request);
                if (proto == null)
                {
                    return new FragmentEquipResult { Success = false, FragmentIds = new List<int>() };
                }

                var fragmentIds = new List<int>();
                if (proto.FragmentIds != null)
                {
                    fragmentIds.AddRange(proto.FragmentIds);
                }

                var rewards = new List<RewardItem>();
                if (proto.Rewards != null)
                {
                    foreach (var rew in proto.Rewards)
                    {
                        rewards.Add(new RewardItem(rew.Id, rew.Num));
                    }
                }

                foreach (var eid in fragmentIds)
                {
                    Global.PlayerEquips.Remove(eid);
                }

                Items.Update(proto.Items);

                Global.Publisher.Publish(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, null);

                return new FragmentEquipResult
                {
                    Success = true,
                    FragmentIds = fragmentIds,
                    Items = proto.Items == null ? null : Items.ToItemGroup(proto.Items),
                    Rewards = rewards
                };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipService] 分解装备失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取指定英灵指定部位的装备槽位数据
        /// </summary>
        public EquipCardSlotData GetWearPartSlot(int soldierId, EquipPartType partType)
        {
            if (soldierId == 0)
            {
                return null;
            }

            var slotData = new EquipCardSlotData
            {
                PartType = partType,
                LockFusionGem = !ModuleFunction.CheckOpen(ModuleFunctionType.FusionGemSlot, false)
            };

            var equip = Global.PlayerEquips.Values.FirstOrDefault(v => v.WearerId == soldierId && v.PartType == partType);
            if (equip != null)
            {
                slotData.EquipOnlyId = equip.Id;
                slotData.FusionGem = Global.PlayerFusionGems?.Values.FirstOrDefault(v => v != null && v.WearerId == equip.Id);
            }

            return slotData;
        }

        public Dictionary<EquipPartType, EquipCardSlotData> GetWearPartSlots(int soldierId)
        {
            var wearPartSlots = new Dictionary<EquipPartType, EquipCardSlotData>();
            if (soldierId == 0)
            {
                return wearPartSlots;
            }

            var partNameList = DataConfigManager.Tables.TbGameBattleConst.EquipPartTypeList;
            for (int i = 0; i < partNameList.Count; i++)
            {
                var partType = (EquipPartType)i;
                wearPartSlots[partType] = GetWearPartSlot(soldierId, partType);
            }

            return wearPartSlots;
        }

        #endregion

        #region 数据刷新

        /// <summary>
        /// 刷新装备数据
        /// </summary>
        private void RefreshEquipData(List<PlayerEquip> playerEquips)
        {
            if (playerEquips == null) return;
            
            foreach (var equip in playerEquips)
            {
                Global.PlayerEquips[equip.Id] = equip;
            }
        }

        private void RefreshFusionGemData(List<PlayerFusionGem> playerFusionGems)
        {
            if (playerFusionGems == null) return;
            
            foreach (var gem in playerFusionGems)
            {
                Global.PlayerFusionGems[gem.Id] = new PlayerFusionGem(gem);
            }
        }

        #endregion

        #region 装备升级

        /// <summary>
        /// 升级装备槽位等级
        /// </summary>
        public async UniTask<EquipSlotUpgradeResult> UpgradeEquipSlotAsync(EquipPartType partType)
        {
            try
            {
                var request = new EquipSlotUpgradeRequest { Slot = (int)partType };
                var proto = await WebDataFun.SendProtoPostRequest<EquipSlotUpgradeResponse>(
                    ApiKeys.ApiEquipSlotUpgrade, request);
                if (proto == null || proto.EquipSlot == null)
                {
                    return null;
                }

                var local = ProtoMappers.MapPlayerEquipSlot(proto.EquipSlot);
                var model = new PlayerEquipSlot(local);
                Global.PlayerEquipSlots[partType] = model;

                return new EquipSlotUpgradeResult
                {
                    EquipSlot = model,
                    Items = proto.Items == null ? null : Items.ToItemGroup(proto.Items),
                };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipService] 装备槽位升级失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取指定部位的槽位数据
        /// </summary>
        public PlayerEquipSlot GetEquipSlot(EquipPartType partType)
        {
            if (Global.PlayerEquipSlots.TryGetValue(partType, out var slot))
            {
                return slot;
            }

            return PlayerEquipSlot.CreateDefault(partType);
        }

        /// <summary>
        /// 升级装备阶段（旧接口，锻造已迁移至 UpgradeEquipSlotAsync）
        /// </summary>
        public async UniTask<UpPhaseResult> UpPhaseEquipAsync(List<int> equipIds)
        {
            try
            {
                var request = new UpPhaseEquipRequest();
                request.Ids.AddRange(equipIds);

                var proto = await WebDataFun.SendProtoPostRequest<UpPhaseEquipResponse>(ApiKeys.ApiEquipUpPhase, request);
                if (proto == null)
                {
                    return null;
                }

                var equips = new List<PlayerEquipModel>();
                if (proto.Equips != null)
                {
                    foreach (var e in proto.Equips)
                    {
                        var model = ProtoMappers.MapPlayerEquip(e);
                        if (model != null)
                        {
                            equips.Add(model);
                        }
                    }
                }

                return new UpPhaseResult
                {
                    Equips = equips,
                    Items = proto.Items == null ? null : Items.ToItemGroup(proto.Items)
                };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipService] 装备升级失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 词条洗炼

        /// <summary>
        /// 洗炼装备词条
        /// </summary>
        /// <param name="equipId">装备实例 id</param>
        /// <param name="lockedEffects">锁定的特效词条 id 列表（对应 proto locked_effects）</param>
        public async UniTask<RefreshEntryResult> RefreshEntryAsync(int equipId, List<int> lockedEffects)
        {
            try
            {
                var request = new RefreshEntryEquipRequest { Id = equipId };
                if (lockedEffects != null)
                {
                    request.LockedEffects.AddRange(lockedEffects);
                }

                var proto = await WebDataFun.SendProtoPostRequest<RefreshEntryEquipResponse>(ApiKeys.ApiEquipRefreshEntry, request);
                if (proto == null || proto.Equip == null)
                {
                    return null;
                }

                return new RefreshEntryResult
                {
                    Equip = ProtoMappers.MapPlayerEquip(proto.Equip),
                    Items = proto.Items == null ? null : Items.ToItemGroup(proto.Items)
                };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipService] 词条洗炼失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 应用洗炼结果（将临时词条替换为正式词条）
        /// </summary>
        /// <param name="equipId">装备实例 id</param>
        public async UniTask<UseRefreshEntryResult> UseRefreshEntryAsync(int equipId)
        {
            try
            {
                var request = new UseRefreshEntryEquipRequest { Id = equipId };
                var proto = await WebDataFun.SendProtoPostRequest<UseRefreshEntryEquipResponse>(
                    ApiKeys.ApiEquipUseRefreshEntry, request);
                if (proto == null || proto.Equip == null)
                {
                    return null;
                }
                Items.Update(proto.Items);
                return new UseRefreshEntryResult
                {
                    Equip = ProtoMappers.MapPlayerEquip(proto.Equip)
                };
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipService] 应用洗炼词条失败: {ex.Message}");
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// 装备操作结果
    /// </summary>
    public class EquipResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<PlayerEquip> PlayerEquips { get; set; }
        public List<PlayerFusionGem> PlayerFusionGems { get; set; }
    }
}
