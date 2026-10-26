using System.Collections.Generic;
using Assets.Scripts.Localization;
using Config;
using FusionContinent.Model;
using Game.HeroSeat;
using Manager;
using TMPro;
using UIFramework;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    [PrefabPath("MechanicUpgradePanel")]
    public class MechanicUpgradePanelNew : UIPanel<MechanicUpgradePanelViewModel>
    {
        private const float BgHeightNotActivated = 1300f;
        private const float BgHeightActivated = 1572f;
        private const int LevelStageSize = 5;

        private MechanicUpgradePanel mOldPanel;
        private SeatEntity mSeatEntity;
        private EffectItem mEffectItemTemplate;
        private readonly List<EffectItem> mEffectItemInstances = new List<EffectItem>();

        private UnityAction mMaskHandler;
        private UnityAction mCloseBtnHandler;
        private UnityAction mUpGradeBtnHandler;
        private UnityAction mWearBtnHandler;
        private UnityAction mUnWearBtnHandler;
        private System.Action mClosePanelHandler;
        private System.Action<string, string> mMechanicNameTextHandler;
        private System.Action<bool, bool> mShowUpGradeBtnHandler;
        private System.Action<bool, bool> mShowWearBtnHandler;
        private System.Action<bool, bool> mShowUnWearBtnHandler;

        void Awake()
        {
            mOldPanel = GetComponent<MechanicUpgradePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[MechanicUpgradePanelNew] 找不到 MechanicUpgradePanel 组件");
            }
        }

        protected override void OnBindViewModel(MechanicUpgradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mMaskHandler = () => vm.OnCloseCommand.Execute();
            mOldPanel.Mask.onClick.AddListener(mMaskHandler);
            mCloseBtnHandler = () => vm.OnCloseCommand.Execute();
            mOldPanel.CloseBtn.onClick.AddListener(mCloseBtnHandler);

            mUpGradeBtnHandler = () => vm.OnUpgradeCommand.Execute();
            mOldPanel.UpGradeBtn.onClick.AddListener(mUpGradeBtnHandler);
            mWearBtnHandler = () => vm.OnWearCommand.Execute();
            mOldPanel.WearBtn.onClick.AddListener(mWearBtnHandler);
            mUnWearBtnHandler = () => vm.OnUnWearCommand.Execute();
            mOldPanel.UnWearBtn.onClick.AddListener(mUnWearBtnHandler);

            mClosePanelHandler = CloseSelf;
            vm.OnClosePanel += mClosePanelHandler;

            CacheSeatEntity();
            CacheMechanicEffItemTemplate();
            mMechanicNameTextHandler = OnMechanicNameTextChanged;
            vm.MechanicNameText.OnValueChanged += mMechanicNameTextHandler;
            mShowUpGradeBtnHandler = (_, visible) => SetButtonVisible(mOldPanel.UpGradeBtn, visible);
            vm.ShowUpGradeBtn.OnValueChanged += mShowUpGradeBtnHandler;
            mShowWearBtnHandler = (_, visible) => SetButtonVisible(mOldPanel.WearBtn, visible);
            vm.ShowWearBtn.OnValueChanged += mShowWearBtnHandler;
            mShowUnWearBtnHandler = (_, visible) => SetButtonVisible(mOldPanel.UnWearBtn, visible);
            vm.ShowUnWearBtn.OnValueChanged += mShowUnWearBtnHandler;

            if (mOldPanel.MechanicName != null)
            {
                mOldPanel.MechanicName.text = vm.MechanicNameText.Value;
            }

            SetButtonVisible(mOldPanel.UpGradeBtn, vm.ShowUpGradeBtn.Value);
            SetButtonVisible(mOldPanel.WearBtn, vm.ShowWearBtn.Value);
            SetButtonVisible(mOldPanel.UnWearBtn, vm.ShowUnWearBtn.Value);

            Global.Publisher.Subscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnCityWallDataChanged);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnConsumeItemChanged);

            RefreshLevelDisplay(vm);
        }

        private void CacheSeatEntity()
        {
            mSeatEntity = null;
            if (mOldPanel?.SeatEntity == null)
            {
                return;
            }

            mSeatEntity = mOldPanel.SeatEntity.GetComponent<SeatEntity>();
            if (mSeatEntity == null)
            {
                mSeatEntity = mOldPanel.SeatEntity.GetComponentInChildren<SeatEntity>(true);
            }
        }

        private void CacheMechanicEffItemTemplate()
        {
            mEffectItemTemplate = null;
            var content = mOldPanel?.MechanicEffSCView?.content;
            if (content == null || content.childCount == 0)
            {
                return;
            }

            mEffectItemTemplate = content.GetChild(0).GetComponent<EffectItem>();
            if (mEffectItemTemplate != null)
            {
                mEffectItemTemplate.gameObject.SetActive(false);
            }

            for (var i = 1; i < content.childCount; i++)
            {
                content.GetChild(i).gameObject.SetActive(false);
            }
        }

        private static void SetButtonVisible(
            FusionContinent.UI.Components.UIPointerClickAndLongPress button,
            bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private void OnMechanicNameTextChanged(string oldValue, string newValue)
        {
            if (mOldPanel?.MechanicName != null)
            {
                mOldPanel.MechanicName.text = newValue;
            }
        }

        private void OnCityWallDataChanged(object _)
        {
            VM.RefreshDisplay();
            RefreshLevelDisplay();
        }

        private void OnConsumeItemChanged(object _)
        {
            VM.RefreshDisplay();
            RefreshConsumeListDisplay(VM.SkillId, GameServiceManager.Get<SeatService>());
        }

        private void RefreshLevelDisplay()
        {
            RefreshLevelDisplay(VM);
        }

        private void RefreshLevelDisplay(MechanicUpgradePanelViewModel vm)
        {
            if (vm.SkillId <= 0)
            {
                if (mOldPanel?.ConsumeList != null)
                {
                    mOldPanel.ConsumeList.gameObject.SetActive(false);
                }

                if (mOldPanel?.MainAttrName != null)
                {
                    mOldPanel.MainAttrName.text = string.Empty;
                }

                if (mOldPanel?.CurNum != null)
                {
                    mOldPanel.CurNum.text = string.Empty;
                }

                if (mOldPanel?.NextNum != null)
                {
                    mOldPanel.NextNum.text = string.Empty;
                }

                SetBgHeight(mOldPanel?.Bg, BgHeightNotActivated);
                ClearEffectItems();
                return;
            }

            var seatService = GameServiceManager.Get<SeatService>();
            var isActivated = seatService.IsSkillActivated(vm.SkillId);
            var currentLevel = seatService.GetSkill(vm.SkillId)?.Level ?? 0;
            var levelCfg = seatService.GetSkillLevelCfg(vm.SkillId);
            if (mOldPanel?.MechanicDetail != null)
            {
                mOldPanel.MechanicDetail.text = levelCfg?.Desc2 ?? string.Empty;
            }

            RefreshConsumeListDisplay(vm.SkillId, seatService);

            SetBgHeight(mOldPanel?.Bg, isActivated ? BgHeightActivated : BgHeightNotActivated);
            var displayLevel = isActivated ? currentLevel : 1;
            RefreshLevelStageDisplay(displayLevel, vm.SkillId, seatService);
            RefreshMechanicEffDisplay(vm.SkillId, seatService);

            if (mSeatEntity != null)
            {
                mSeatEntity.SetData(new SeatEntityData
                {
                    SeatSkillId = vm.SkillId,
                    IsSelect = false,
                    ShowFrame = false,
                    EnableOpenUpgradePanelOnClick = false,
                    CheckUnlock = false,
                });
            }
        }

        private void RefreshConsumeListDisplay(int skillId, SeatService seatService)
        {
            if (mOldPanel?.ConsumeList == null)
            {
                return;
            }

            var isActivated = seatService.IsSkillActivated(skillId);
            var isMaxLevel = seatService.IsSkillMaxLevel(skillId);
            var visible = isActivated && !isMaxLevel;
            mOldPanel.ConsumeList.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var levelCfg = seatService.GetSkillLevelCfg(skillId);
            SetConsumes(mOldPanel.ConsumeList, levelCfg?.LevelUpItems);
        }

        private static void SetConsumes(RectTransform consumeRoot, List<RewardAndNum> consumeItems)
        {
            for (var i = 0; i < consumeRoot.childCount; i++)
            {
                var consumeTran = consumeRoot.GetChild(i);
                var show = consumeItems != null && i < consumeItems.Count;
                consumeTran.gameObject.SetActive(show);
                if (!show)
                {
                    continue;
                }

                consumeTran.GetComponent<RewardEntity>()?.SetData(consumeItems[i], RewardStyleType.Consume);
            }
        }

        private void RefreshMechanicEffDisplay(int skillId, SeatService seatService)
        {
            if (mEffectItemTemplate == null)
            {
                return;
            }

            ClearEffectItems();

            var config = seatService.GetSkillConfig(skillId);
            if (config?.SeatLevelList == null)
            {
                return;
            }

            var currentLevel = seatService.GetSkill(skillId)?.Level ?? 0;
            var content = mOldPanel.MechanicEffSCView.content;
            var index = 0;
            foreach (var levelCfg in config.SeatLevelList)
            {
                if (string.IsNullOrEmpty(levelCfg.Desc3))
                {
                    continue;
                }

                var effectItem = Instantiate(mEffectItemTemplate, content, false);
                effectItem.gameObject.name = $"EffectItem_{index}";
                effectItem.gameObject.SetActive(true);
                effectItem.SetData(new EffectItemData
                {
                    skillLevel = levelCfg.Level,
                    skillDes = levelCfg.Desc3,
                    isLock = levelCfg.Level > currentLevel,
                });
                mEffectItemInstances.Add(effectItem);
                index++;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private void RefreshLevelStageDisplay(int displayLevel, int skillId, SeatService seatService)
        {
            if (mOldPanel?.LevelText != null)
            {
                mOldPanel.LevelText.text = $"Lv. {displayLevel}";
            }

            RefreshMainAttrDisplay(skillId, displayLevel, seatService);

            GetLevelStageRange(displayLevel, out var stageStart, out _);

            if (mOldPanel?.LevelList != null)
            {
                var slotCount = Mathf.Min(mOldPanel.LevelList.childCount, LevelStageSize);
                for (var i = 0; i < slotCount; i++)
                {
                    var slot = mOldPanel.LevelList.GetChild(i);
                    var level = stageStart + i;

                    var isCurrent = level == displayLevel;
                    var isReached = level < displayLevel;
                    var isLocked = level > displayLevel;

                    SetChildActive(slot, "Current", isCurrent);
                    SetChildActive(slot, "Unlock", isReached);
                    SetChildActive(slot, "Lock", isLocked);

                    var unlockLevelText = slot.Find("UnlockLevelText")?.GetComponent<TextMeshProUGUI>();
                    if (unlockLevelText != null)
                    {
                        unlockLevelText.text = level.ToString();
                        unlockLevelText.gameObject.SetActive(!isLocked);
                    }

                    var lockLevelText = slot.Find("LockLevelText")?.GetComponent<TextMeshProUGUI>();
                    if (lockLevelText != null)
                    {
                        lockLevelText.text = level.ToString();
                        lockLevelText.gameObject.SetActive(isLocked);
                    }
                }
            }

            if (mOldPanel?.LevelSlider != null)
            {
                mOldPanel.LevelSlider.value = Mathf.Clamp01((displayLevel - stageStart) / (float)(LevelStageSize - 1));
            }
        }

        private void RefreshMainAttrDisplay(int skillId, int displayLevel, SeatService seatService)
        {
            var currentLevelCfg = seatService.GetSkillLevelCfg(skillId, displayLevel);
            var nextLevelCfg = seatService.GetSkillLevelCfg(skillId, displayLevel + 1);
            var currentAttr = GetFirstBaseAttribute(currentLevelCfg);
            var nextAttr = GetFirstBaseAttribute(nextLevelCfg);

            if (mOldPanel?.MainAttrName != null)
            {
                mOldPanel.MainAttrName.text = BuildBaseAttributeName(currentAttr);
            }

            if (mOldPanel?.CurNum != null)
            {
                mOldPanel.CurNum.text = FormatAttributeValue(currentAttr);
            }

            if (mOldPanel?.NextNum != null)
            {
                mOldPanel.NextNum.text = FormatAttributeValue(nextAttr);
            }
        }

        private static AttributeStruct GetFirstBaseAttribute(SeatLevelListStruct levelCfg)
        {
            if (levelCfg?.BaseAttributes == null || levelCfg.BaseAttributes.Count == 0)
            {
                return null;
            }

            return levelCfg.BaseAttributes[0];
        }

        private static string BuildBaseAttributeName(AttributeStruct attribute)
        {
            if (attribute == null)
            {
                return string.Empty;
            }

            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attribute.Type);
            if (attrCfg == null)
            {
                return string.Empty;
            }

            var name = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, attrCfg.Name);
            return string.Format(name, string.Empty);
        }

        private static string FormatAttributeValue(AttributeStruct attribute)
        {
            if (attribute == null)
            {
                return string.Empty;
            }

            var value = AttributeManager.SumValuePercentage(attribute.Type, attribute.Value, attribute.ValueType);
            if (attribute.ValueType == ValueType.Percentage)
            {
                return $"{value}%";
            }

            return Mathf.Approximately(value % 1, 0) ? ((int)value).ToString() : value.ToString("0.##");
        }

        private static void GetLevelStageRange(int displayLevel, out int stageStart, out int stageEnd)
        {
            stageStart = ((displayLevel - 1) / LevelStageSize) * LevelStageSize + 1;
            stageEnd = stageStart + LevelStageSize - 1;
        }

        private static void SetChildActive(Transform parent, string childName, bool active)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private void ClearEffectItems()
        {
            foreach (var instance in mEffectItemInstances)
            {
                if (instance != null)
                {
                    Destroy(instance.gameObject);
                }
            }

            mEffectItemInstances.Clear();
        }

        private static void SetBgHeight(RectTransform bg, float height)
        {
            if (bg == null)
            {
                return;
            }

            var sizeDelta = bg.sizeDelta;
            sizeDelta.y = height;
            bg.sizeDelta = sizeDelta;
        }

        protected override void OnUnbindViewModel(MechanicUpgradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mMaskHandler != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(mMaskHandler);
                mMaskHandler = null;
            }
            if (mCloseBtnHandler != null)
            {
                mOldPanel.CloseBtn.onClick.RemoveListener(mCloseBtnHandler);
                mCloseBtnHandler = null;
            }
            if (mUpGradeBtnHandler != null)
            {
                mOldPanel.UpGradeBtn.onClick.RemoveListener(mUpGradeBtnHandler);
                mUpGradeBtnHandler = null;
            }
            if (mWearBtnHandler != null)
            {
                mOldPanel.WearBtn.onClick.RemoveListener(mWearBtnHandler);
                mWearBtnHandler = null;
            }
            if (mUnWearBtnHandler != null)
            {
                mOldPanel.UnWearBtn.onClick.RemoveListener(mUnWearBtnHandler);
                mUnWearBtnHandler = null;
            }
            if (mClosePanelHandler != null)
            {
                vm.OnClosePanel -= mClosePanelHandler;
                mClosePanelHandler = null;
            }

            if (mMechanicNameTextHandler != null)
            {
                vm.MechanicNameText.OnValueChanged -= mMechanicNameTextHandler;
                mMechanicNameTextHandler = null;
            }
            if (mShowUpGradeBtnHandler != null)
            {
                vm.ShowUpGradeBtn.OnValueChanged -= mShowUpGradeBtnHandler;
                mShowUpGradeBtnHandler = null;
            }
            if (mShowWearBtnHandler != null)
            {
                vm.ShowWearBtn.OnValueChanged -= mShowWearBtnHandler;
                mShowWearBtnHandler = null;
            }
            if (mShowUnWearBtnHandler != null)
            {
                vm.ShowUnWearBtn.OnValueChanged -= mShowUnWearBtnHandler;
                mShowUnWearBtnHandler = null;
            }

            Global.Publisher.Unsubscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnCityWallDataChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnConsumeItemChanged);
            ClearEffectItems();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            VM.RefreshDisplay();
            RefreshLevelDisplay();
        }
    }
}
