using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using Game.HeroSeat;
using Game.RedDot;
using ScrollView;
using UIFramework;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.Events;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    [PrefabPath("HeroSeatPanel")]
    public class HeroSeatPanelNew : UIPanel<HeroSeatPanelViewModel>
    {
        private HeroSeatPanel mOldPanel;
        private SeatSlotItem[] mSeatSlotItems;
        private List<HeroSeatConfig> mWallSkillConfigs = new List<HeroSeatConfig>();

        private UnityAction mCloseBtnHandler;
        private UnityAction mQuitHandler;
        private System.Action mClosePanelHandler;
        private int mSubscribedConsumeItemId;

        void Awake()
        {
            mOldPanel = GetComponent<HeroSeatPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[HeroSeatPanelNew] 找不到 HeroSeatPanel 组件");
            }
        }

        protected override void OnBindViewModel(HeroSeatPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mCloseBtnHandler = () => vm.OnCloseCommand.Execute();
            mOldPanel.CloseBtn.onClick.AddListener(mCloseBtnHandler);
            mQuitHandler = () => vm.OnCloseCommand.Execute();
            mOldPanel.Quit.onClick.AddListener(mQuitHandler);

            mOldPanel.UpGradeBtn.onClick.AddListener(OnUpGradeButtonClicked);
            mOldPanel.UpGradeBtn.SetRedType(RedNodeType.SpeakerHeroSeatWallUpgradeBtn);

            mClosePanelHandler = CloseSelf;
            vm.OnClosePanel += mClosePanelHandler;

            vm.CityWallNameText.OnValueChanged += OnCityWallNameTextChanged;
            mOldPanel.CityWallNameText.text = vm.CityWallNameText.Value;

            vm.ShowWallUpgradeConsume.OnValueChanged += OnShowWallUpgradeConsumeChanged;
            vm.WallUpgradeConsumeItemId.OnValueChanged += OnWallUpgradeConsumeChanged;
            vm.WallUpgradeConsumeCount.OnValueChanged += OnWallUpgradeConsumeChanged;

            Global.Publisher.Subscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnCityWallDataChanged);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnConsumeItemChanged);

            CacheSeatSlotItems();
            CacheWallSkillConfigs();
            BindMechanicSkillScrollView();
            RefreshUpgradeControlsDisplay(vm);
            RefreshSeatSlotList();
            RefreshMechanicSkillList();
            RefreshMechanicStatisticsDisplay();
        }

        private void CacheWallSkillConfigs()
        {
            mWallSkillConfigs = DataConfigManager.Tables.TbHeroSeatConfig.DataList;
        }

        private void BindMechanicSkillScrollView()
        {
            if (mOldPanel?.MechanicSCView == null)
            {
                return;
            }

            mOldPanel.MechanicSCView.SetUpdateFunc(OnUpdateMechanicSkillItem);
        }

        private void RefreshMechanicSkillList(bool refreshVisibleOnly = false)
        {
            if (mOldPanel?.MechanicSCView == null)
            {
                return;
            }

            var count = mWallSkillConfigs?.Count ?? 0;
            if (refreshVisibleOnly)
            {
                mOldPanel.MechanicSCView.Refresh(count);
                return;
            }

            mOldPanel.MechanicSCView.ShowList(count);
        }

        private void OnUpdateMechanicSkillItem(int dataIndex, ScrollItem scrollItem)
        {
            if (dataIndex < 0 || mWallSkillConfigs == null || dataIndex >= mWallSkillConfigs.Count)
            {
                scrollItem.gameObject.SetActive(false);
                return;
            }

            var seatEntity = scrollItem.GetComponent<SeatEntity>();
            if (seatEntity == null)
            {
                scrollItem.gameObject.SetActive(false);
                return;
            }

            scrollItem.gameObject.SetActive(true);
            var skillConfig = mWallSkillConfigs[dataIndex];
            seatEntity.SetData(new SeatEntityData
            {
                SeatSkillId = skillConfig.Id,
                IsSelect = false,
                ShowFrame = true,
                CheckUnlock = true,
            });

            // 可激活 / 可升级时显示红点（可装备不亮）
            SeatService seatService = GameServiceManager.Get<SeatService>();
            bool showRed = seatService != null && seatService.HasSkillActionRedDot(skillConfig.Id);
            ((RectTransform)seatEntity.transform).SetLocalRedType(showRed ? 1 : 0);
        }

        private void CacheSeatSlotItems()
        {
            if (mOldPanel?.MechanicList == null)
            {
                mSeatSlotItems = null;
                return;
            }

            mSeatSlotItems = mOldPanel.MechanicList.GetComponentsInChildren<SeatSlotItem>(true);
        }

        private void RefreshSeatSlotList()
        {
            if (mSeatSlotItems == null || mSeatSlotItems.Length == 0)
            {
                CacheSeatSlotItems();
            }

            if (mSeatSlotItems == null)
            {
                return;
            }

            for (var i = 0; i < mSeatSlotItems.Length; i++)
            {
                mSeatSlotItems[i].SetData(i);
            }
        }

        private void OnCityWallNameTextChanged(string oldValue, string newValue)
        {
            mOldPanel.CityWallNameText.text = newValue;
        }

        private void OnUpGradeButtonClicked()
        {
            VM.OnUpgradeWallCommand.Execute();
        }

        private void OnShowWallUpgradeConsumeChanged(bool oldValue, bool newValue)
        {
            UpdateConsumeItemSubscription(VM.ShowWallUpgradeConsume.Value ? VM.WallUpgradeConsumeItemId.Value : 0);
            RefreshUpgradeControlsDisplay();
        }

        private void OnWallUpgradeConsumeChanged(int oldValue, int newValue)
        {
            UpdateConsumeItemSubscription(VM.ShowWallUpgradeConsume.Value ? VM.WallUpgradeConsumeItemId.Value : 0);
            RefreshUpgradeControlsDisplay();
        }

        private void OnCityWallDataChanged(object _)
        {
            VM.RefreshWallDisplay();
            RefreshSeatSlotList();
            RefreshMechanicSkillList();
            RefreshMechanicStatisticsDisplay();
        }

        private void OnConsumeItemChanged(object _)
        {
            RefreshUpgradeControlsDisplay();
            RefreshMechanicSkillList(true);
        }

        private void RefreshMechanicStatisticsDisplay()
        {
            if (mOldPanel?.MechanicStatisticsText == null)
            {
                return;
            }

            var seatService = GameServiceManager.Get<SeatService>();
            var maxCount = mWallSkillConfigs?.Count ?? 0;
            var activatedCount = 0;
            if (mWallSkillConfigs != null)
            {
                foreach (var config in mWallSkillConfigs)
                {
                    if (seatService.IsSkillActivated(config.Id))
                    {
                        activatedCount++;
                    }
                }
            }

            mOldPanel.MechanicStatisticsText.text = $"{activatedCount}/{maxCount}";
        }

        private void RefreshUpgradeControlsDisplay()
        {
            RefreshUpgradeControlsDisplay(VM);
        }

        private void RefreshUpgradeControlsDisplay(HeroSeatPanelViewModel vm)
        {
            var visible = vm.ShowWallUpgradeConsume.Value;
            mOldPanel.UpGradeBtn.gameObject.SetActive(visible);
            if (mOldPanel.ConsumeNode != null)
            {
                mOldPanel.ConsumeNode.gameObject.SetActive(visible);
            }
            RefreshConsumeDisplay(vm);
        }

        private void RefreshConsumeDisplay()
        {
            RefreshConsumeDisplay(VM);
        }

        private void RefreshConsumeDisplay(HeroSeatPanelViewModel vm)
        {
            var consume = mOldPanel.Consume;
            if (consume == null)
            {
                return;
            }

            var visible = vm.ShowWallUpgradeConsume.Value;
            consume.gameObject.SetActive(visible);
            if (!visible)
            {
                UpdateConsumeItemSubscription(0);
                return;
            }

            var itemId = vm.WallUpgradeConsumeItemId.Value;
            var needCount = vm.WallUpgradeConsumeCount.Value;
            if (itemId <= 0 || needCount <= 0)
            {
                UpdateConsumeItemSubscription(0);
                return;
            }

            consume.SetData(itemId, needCount);
            UpdateConsumeItemSubscription(itemId);
        }

        private void UpdateConsumeItemSubscription(int itemId)
        {
            if (mSubscribedConsumeItemId == itemId)
            {
                return;
            }

            if (mSubscribedConsumeItemId > 0)
            {
                Global.Publisher.Unsubscribe(
                    $"{MessagePublisher.CHANGE_ITEM_NUM}{mSubscribedConsumeItemId}",
                    OnConsumeItemChanged);
            }

            mSubscribedConsumeItemId = itemId;

            if (itemId > 0)
            {
                Global.Publisher.Subscribe(
                    $"{MessagePublisher.CHANGE_ITEM_NUM}{itemId}",
                    OnConsumeItemChanged);
            }
        }

        protected override void OnUnbindViewModel(HeroSeatPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mCloseBtnHandler != null)
            {
                mOldPanel.CloseBtn.onClick.RemoveListener(mCloseBtnHandler);
                mCloseBtnHandler = null;
            }
            if (mQuitHandler != null)
            {
                mOldPanel.Quit.onClick.RemoveListener(mQuitHandler);
                mQuitHandler = null;
            }
            mOldPanel.UpGradeBtn.onClick.RemoveListener(OnUpGradeButtonClicked);
            if (mClosePanelHandler != null)
            {
                vm.OnClosePanel -= mClosePanelHandler;
                mClosePanelHandler = null;
            }

            vm.CityWallNameText.OnValueChanged -= OnCityWallNameTextChanged;
            vm.ShowWallUpgradeConsume.OnValueChanged -= OnShowWallUpgradeConsumeChanged;
            vm.WallUpgradeConsumeItemId.OnValueChanged -= OnWallUpgradeConsumeChanged;
            vm.WallUpgradeConsumeCount.OnValueChanged -= OnWallUpgradeConsumeChanged;

            Global.Publisher.Unsubscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnCityWallDataChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnConsumeItemChanged);
            UpdateConsumeItemSubscription(0);

            if (mOldPanel.MechanicSCView != null)
            {
                mOldPanel.MechanicSCView.SetUpdateFunc(null);
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            VM.RefreshWallDisplay();
            RefreshSeatSlotList();
            RefreshMechanicSkillList();
            RefreshMechanicStatisticsDisplay();
        }

        protected override void OnShow()
        {
            VM.RefreshWallDisplay();
            RefreshSeatSlotList();
            RefreshMechanicSkillList();
            RefreshMechanicStatisticsDisplay();
        }
    }
}
