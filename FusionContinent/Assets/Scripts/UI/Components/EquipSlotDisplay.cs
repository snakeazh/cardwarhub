using System.Linq;
using Config;
using FusionContinent.Model;
using Game.Equip;
using TMPro;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.UI.Components
{
    /// <summary>
    /// 单个装备槽位展示。在 Inspector 中拖拽绑定 UI 节点；PartType 由外部在显示时通过 SetPartType 指定。
    /// 装备图标与品质背景统一通过 RewardEntity 显示。
    /// </summary>
    public class EquipSlotDisplay : MonoBehaviour
    {
        private EquipPartType mPartType;
        [SerializeField] private Button mSlotButton;
        [SerializeField] private RewardEntity mRewardEntity;
        [SerializeField] private GameObject mIsEmptyObj;
        [SerializeField] private Image mPartIconImage;
        public TMP_Text SlotLevelText;
        [SerializeField] private Image mGemImage;

        public EquipPartType PartType => mPartType;

        /// <summary>
        /// 懒加载获取 RewardEntity；未在 Inspector 绑定时自动在子节点中查找，兼容老预制体。
        /// </summary>
        private RewardEntity RewardEntityRef
        {
            get
            {
                if (mRewardEntity == null)
                    mRewardEntity = GetComponentInChildren<RewardEntity>(true);
                return mRewardEntity;
            }
        }

        public void SetPartType(EquipPartType partType)
        {
            mPartType = partType;
        }

        public void SetEquip(PlayerEquip equip, int wearerId, bool selected = false, System.Action onSelect = null)
        {

            if (equip == null)
            {
                ShowEmpty();
                RefreshGem(null);
                return;
            }

            var equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null)
            {
                ShowEmpty();
                RefreshGem(null);
                return;
            }

            var itemCfg = GameServiceManager.Get<EquipService>().GetSoldierEquipItemByEquipId(equip.EquipId);
            if (itemCfg == null)
            {
                LogTool.LogError("[EquipSlotDisplay] 找不到装备对应道具，equipId=" + equip.EquipId);
                ShowEmpty();
                RefreshGem(null);
                return;
            }

            mIsEmptyObj.SetActive(false);
            RefreshSlotLevel();

            // 背景与图标统一交给 RewardEntity 显示
            var rewardEntity = RewardEntityRef;
            if (rewardEntity != null)
            {
                rewardEntity.gameObject.SetActive(true);
                var equipOnlyId = equip.Id;
                rewardEntity.SetData(new RewardEntityData()
                {
                    ItemId = itemCfg.Id,
                    EquipLevel = equipCfg.EquipLevel,
                    Quality = equipCfg.Quality,
                    IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
                    StyleType = RewardStyleType.Equip,
                    IsEffect = false,
                    Check = selected,
                    OnClick = onSelect ?? (() => GlobalFunc.OpenEquipDetailsPanel(equipOnlyId, wearerId)),
                });
            }

            var fusionGem = Global.PlayerFusionGems.Values
                .FirstOrDefault(v => v != null && v.WearerId == equip.Id);
            RefreshGem(fusionGem);
        }

        public void SetSelected(bool selected)
        {
            var rewardEntity = RewardEntityRef;
            if (rewardEntity != null && rewardEntity.frameCheck != null)
                rewardEntity.frameCheck.gameObject.SetActive(selected);
        }

        private void ShowEmpty()
        {
            // 隐藏装备展示，显示空槽位占位
            var rewardEntity = RewardEntityRef;
            if (rewardEntity != null) rewardEntity.gameObject.SetActive(false);

            mIsEmptyObj.SetActive(true);
            mPartIconImage.sprite = Global.EquipPartIconAtlas.GetSprite(mPartType.ToString());
            RefreshSlotLevel();

        }

        /// <summary>
        /// 刷新槽位锻造等级文本（等级取自部位槽位数据，与是否穿戴装备无关）
        /// </summary>
        private void RefreshSlotLevel()
        {
            if (SlotLevelText == null) return;
            var slot = GameServiceManager.Get<EquipService>().GetEquipSlot(mPartType);
            SlotLevelText.text = $"+{slot.Level}";
        }



        private void RefreshGem(PlayerFusionGem fusionGem)
        {
            if (fusionGem != null && fusionGem.Config != null)
            {
                mGemImage.sprite = Global.FusionGemStoreAtlas.GetSprite(
                    "InlayStone" + fusionGem.Config.Quality.ToString());
            }
            else
            {
                mGemImage.sprite = Global.FusionGemStoreAtlas.GetSprite("UnsetState");
            }
        }
    }
}
