using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using Game.CombatPower;
using Game.Equip;
using Cysharp.Threading.Tasks;
using UIFramework;
using UIFramework.Services;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 融合石详情面板 ViewModel
    /// </summary>
    public class FusionGemDetailPanelViewModel : ViewModel
    {
        public BindableProperty<int> GemId { get; } = new BindableProperty<int>(0);
        public BindableProperty<Sprite> GemIconSprite { get; } = new BindableProperty<Sprite>(null);
        public BindableProperty<string> GemName { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<string> GemQualityTextKey { get; } = new BindableProperty<string>(string.Empty);
        public BindableProperty<bool> IsWear { get; } = new BindableProperty<bool>(false);
        public BindableProperty<EquipPartType> SelectedPartType { get; } = new BindableProperty<EquipPartType>(EquipPartType.Tops);
        public BindableProperty<List<DisplayAttributeRow>> GemAttributes { get; } = new BindableProperty<List<DisplayAttributeRow>>(new List<DisplayAttributeRow>());

        #region 命令

        public Command OnCloseCommand { get; private set; }
        public Command OnWearCommand { get; private set; }
        public Command OnUnWearCommand { get; private set; }

        #endregion

        #region 事件

        public System.Action OnCloseRequested;

        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            OnCloseCommand = new Command(OnClose);
            OnWearCommand = new Command(OnWear);
            OnUnWearCommand = new Command(OnUnWear);
        }

        public void Initialize(int gemId)
        {
            GemId.Value = gemId;
            RefreshPresentation();
        }

        public void Refresh()
        {
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            if (GemId.Value <= 0)
            {
                GemIconSprite.Value = null;
                GemName.Value = string.Empty;
                GemQualityTextKey.Value = string.Empty;
                IsWear.Value = false;
                SelectedPartType.Value = GetFirstWearablePartType();
                GemAttributes.Value = new List<DisplayAttributeRow>();
                return;
            }

            if (Global.PlayerFusionGems == null || !Global.PlayerFusionGems.TryGetValue(GemId.Value, out var gem) || gem == null)
            {
                GemIconSprite.Value = null;
                GemName.Value = string.Empty;
                GemQualityTextKey.Value = string.Empty;
                IsWear.Value = false;
                SelectedPartType.Value = GetFirstWearablePartType();
                GemAttributes.Value = new List<DisplayAttributeRow>();
                return;
            }

            // 兜底补齐 Config/Quality
            if (gem.Config == null)
            {
                gem.Config = DataConfigManager.Tables.TbGemFusionConfig.GetOrDefault(gem.GemId);
                if (gem.Config != null)
                {
                    gem.Quality = gem.Config.Quality;
                }
            }

            var cfg = gem.Config;
            GemName.Value = cfg?.Name ?? string.Empty;
            GemIconSprite.Value = cfg != null ? Global.FusionGemIconSpriteAtlas?.GetSprite(cfg.Icon) : null;


            GemQualityTextKey.Value = GameServiceManager.Get<FusionGemService>().GetGemLevelText(gem.Level);

            SelectedPartType.Value = GetDefaultSelectedPartType();
            LoadGemAttributes(gem);
            RefreshWearButtonState();
        }

        private void LoadGemAttributes(PlayerFusionGem gem)
        {
            if (gem?.Config == null)
            {
                GemAttributes.Value = new List<DisplayAttributeRow>();
                return;
            }

            var lvCfg = gem.GetLevelCfg();
            var rows = new List<DisplayAttributeRow>();

            // 只读取 Desc，不再解析 Attributes / MainAttributes
            if (!string.IsNullOrEmpty(lvCfg.Desc))
            {
                rows.Add(new DisplayAttributeRow
                {
                    NameKey = lvCfg.Desc
                });
            }

            GemAttributes.Value = rows;
        }

        public void SelectPart(EquipPartType partType)
        {
            if (partType == EquipPartType.Weapon)
            {
                return;
            }

            if (GetEquipId(partType) <= 0)
            {
                Toast.Show("该部位未穿戴装备，无法镶嵌融合石");
                return;
            }

            SelectedPartType.Value = partType;
            RefreshWearButtonState();
        }

        //获取当前选中的这个宝石的基础属性
        public List<AttributeStruct> GetAttributes()
        {
            var gemId = this.GemId.Value;
            if (Global.PlayerFusionGems.TryGetValue(gemId, out var gem))
            {
                return gem.Config.LevelList[gem.Level].Attributes;
            }

            return new List<AttributeStruct>();
        }

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }
        
        

        private void OnWear()
        {
            UnlockGuideManager.Send(ModuleFunctionType.FusionGemSlot,4);
            WearOrUnWearAsync(true).Forget();
        }

        private void OnUnWear()
        {
            WearOrUnWearAsync(false).Forget();
        }

        private async UniTask WearOrUnWearAsync(bool isWear)
        {
            if (GemId.Value <= 0)
            {
                Toast.Error("融合石数据无效");
                return;
            }

            var service = GameServiceManager.Get<EquipService>();
            if (service == null)
            {
                Toast.Error("服务未初始化");
                return;
            }

            var equipId =  isWear? GetSelectedEquipId() : 0;
            // if (isWear && equipId <= 0)
            // {
            //     Toast.Error("当前部位未穿戴装备");
            //     return;
            // }

            CombatPowerChangeHelper.CaptureBaseline();

            var result = await service.WearFusionGemAsync(GemId.Value, equipId);
            if (result != null && result.Success)
            {
                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
                RefreshPresentation();
                Global.Publisher.Publish(MessagePublisher.FUSION_GEM_WEAR_CHANGED, GemId.Value);
                // Toast.Show(result.Message );
            }
            else
            {
                Toast.Error((isWear? "穿戴":"卸下")+result.Message );
            }
        }

        private int GetSelectedEquipId()
        {
            return GetEquipId(SelectedPartType.Value);
        }

        private int GetEquipId(EquipPartType partType)
        {
            var equipService = GameServiceManager.Get<EquipService>();
            var slotData = equipService?.GetWearPartSlot(DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId, partType);
            return slotData?.EquipOnlyId ?? 0;
        }

        private int GetCurrentWearerId()
        {
            if (GemId.Value <= 0 || Global.PlayerFusionGems == null)
            {
                return 0;
            }

            return Global.PlayerFusionGems.TryGetValue(GemId.Value, out var gem) && gem != null ? gem.WearerId : 0;
        }

        private void RefreshWearButtonState()
        {
            int currentWearerId = GetCurrentWearerId();
            int selectedEquipId = GetSelectedEquipId();
            IsWear.Value = selectedEquipId <= 0 || selectedEquipId == currentWearerId;
        }

        private EquipPartType GetDefaultSelectedPartType()
        {
            int currentWearerId = GetCurrentWearerId();
            if (currentWearerId > 0 && Global.PlayerEquips != null)
            {
                if (Global.PlayerEquips.TryGetValue(currentWearerId, out var equip) && equip != null)
                {
                    return equip.PartType;
                }
            }

            return GetFirstEquippedWearablePartType();
        }

        private EquipPartType GetFirstWearablePartType()
        {
            foreach (EquipPartType partType in Enum.GetValues(typeof(EquipPartType)))
            {
                if (partType != EquipPartType.Weapon)
                {
                    return partType;
                }
            }

            return EquipPartType.Tops;
        }

        private EquipPartType GetFirstEquippedWearablePartType()
        {
            foreach (EquipPartType partType in Enum.GetValues(typeof(EquipPartType)))
            {
                if (partType != EquipPartType.Weapon && GetEquipId(partType) > 0)
                {
                    return partType;
                }
            }

            return GetFirstWearablePartType();
        }

    }
}
