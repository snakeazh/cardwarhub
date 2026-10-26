using Game.CombatPower;
using Game.Equip;
using UnityEngine;
using UIFramework;
using UIFramework.Services;
using FusionContinent.Model;
using FusionContinent.UI;
using System.Collections.Generic;
using Config;
using ScrollView;
using TMPro;

namespace FusionContinent
{
    /// <summary>
    /// 装备分解界面 - 新框架版本 (MVVM)
    /// 复用原 EquipFragmentPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("EquipFragmentPanel")]
    public class EquipFragmentPanelNew : UIFramework.UIPanel<EquipFragmentPanelViewModel>
    {
        private EquipFragmentPanel mOldPanel;

        void Awake()
        {
            mOldPanel = GetComponent<EquipFragmentPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[EquipFragmentPanelNew] 找不到 EquipFragmentPanel 组件！");
                return;
            }

            LogTool.Log("[EquipFragmentPanelNew] 已关联 EquipFragmentPanel");
        }

        protected override void OnBindViewModel(EquipFragmentPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            BindEquipScrollView();
            BindProperties(vm);

            vm.OnCloseRequested += OnCloseRequested;
            vm.OnFragmentSuccess += OnFragmentSuccess;

            InitButtons();
            InitPartDropdown();
            InitQualityDropdown();
        }

        protected override void OnUnbindViewModel(EquipFragmentPanelViewModel vm)
        {
            UnbindButtons();
            UnbindPartDropdown();
            UnbindQualityDropdown();
            UnbindProperties(vm);
            ReleaseEquipScrollView();

            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnFragmentSuccess -= OnFragmentSuccess;
        }

        #region ScrollView

        private void BindEquipScrollView()
        {
            mOldPanel.EquipList.SetUpdateFunc(OnUpdateEquipItem);
        }

        private void ReleaseEquipScrollView()
        {
            mOldPanel.EquipList.SetUpdateFunc(null);
            mOldPanel.EquipList.ReleaseScrollView();
        }

        private void ShowEquipList()
        {
            var list = VM.EquipList.Value;
            var count = list != null ? list.Count : 0;
            mOldPanel.EquipList.ShowList(0);
            mOldPanel.EquipList.ShowList(count);
        }

        private void RefreshEquipListSelection()
        {
            var list = VM.EquipList.Value;
            var count = list != null ? list.Count : 0;
            if (count <= 0)
            {
                return;
            }

            mOldPanel.EquipList.ShowList(count);
        }

        private void OnUpdateEquipItem(int dataIndex, ScrollItem scrollItem)
        {
            var list = VM.EquipList.Value;
            if (list == null || dataIndex < 0 || dataIndex >= list.Count)
            {
                return;
            }

            var equip = list[dataIndex];
            var equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null)
            {
                return;
            }

            var rewardEntity = scrollItem.GetComponent<RewardEntity>();
            if (rewardEntity == null)
            {
                return;
            }

            var itemCfg = GameServiceManager.Get<EquipService>().GetSoldierEquipItemByEquipId(equip.EquipId);
            if (itemCfg == null)
            {
                LogTool.LogError("[EquipFragmentPanelNew] 找不到装备对应道具，equipId=" + equip.EquipId);
                return;
            }

            var selectedIds = VM.SelectedEquipIds.Value;
            var isSelected = selectedIds != null && selectedIds.Contains(equip.Id);
            var equipId = equip.Id;

            rewardEntity.SetData(new RewardEntityData
            {
                Num = 1,
                ItemId = itemCfg.Id,
                EquipLevel = equipCfg.EquipLevel,
                Quality = equipCfg.Quality,
                IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
                StyleType = RewardStyleType.Equip,
                Check = isSelected,
                OnClick = () => VM.OnToggleEquipCommand.Execute(equipId),
            });
        }

        #endregion

        #region 属性绑定

        private void BindProperties(EquipFragmentPanelViewModel vm)
        {
            vm.EquipList.OnValueChanged += OnEquipListChanged;
            vm.SelectedEquipIds.OnValueChanged += OnSelectedEquipIdsChanged;
            vm.FragmentRewards.OnValueChanged += OnFragmentRewardsChanged;
            vm.CanFragment.OnValueChanged += OnCanFragmentChanged;
        }

        private void UnbindProperties(EquipFragmentPanelViewModel vm)
        {
            vm.EquipList.OnValueChanged -= OnEquipListChanged;
            vm.SelectedEquipIds.OnValueChanged -= OnSelectedEquipIdsChanged;
            vm.FragmentRewards.OnValueChanged -= OnFragmentRewardsChanged;
            vm.CanFragment.OnValueChanged -= OnCanFragmentChanged;
        }

        private void OnEquipListChanged(List<PlayerEquip> oldVal, List<PlayerEquip> newVal)
        {
            ShowEquipList();
        }

        private void OnSelectedEquipIdsChanged(List<int> oldVal, List<int> newVal)
        {
            RefreshEquipListSelection();
            UpdateRewardDisplay(VM.FragmentRewards.Value);
        }

        private void OnFragmentRewardsChanged(Dictionary<int, int> oldVal, Dictionary<int, int> newVal)
        {
            UpdateRewardDisplay(newVal);
        }

        private void OnCanFragmentChanged(bool oldVal, bool newVal)
        {
            mOldPanel.FragmentBtn.interactable = newVal;
        }

        #endregion

        #region 初始化按钮

        private void InitButtons()
        {
            mOldPanel.Mask.onClick.AddListener(OnQuitBtnClicked);
            mOldPanel.FragmentBtn.onClick.AddListener(OnFragmentBtnClicked);
        }

        private void UnbindButtons()
        {
            mOldPanel.Mask.onClick.RemoveListener(OnQuitBtnClicked);
            mOldPanel.FragmentBtn.onClick.RemoveListener(OnFragmentBtnClicked);
        }

        private void OnQuitBtnClicked()
        {
            VM.OnCloseCommand.Execute();
        }

        private void OnFragmentBtnClicked()
        {
            GameMainPanel.Default.ShowConfirm("确认分解", "", () =>
            {
                VM.OnFragmentCommand.Execute();
            });
        }

        #endregion

        #region 部位下拉框

        private void InitPartDropdown()
        {
            var dropdown = mOldPanel.PartDropdown;
            var partNameList = DataConfigManager.Tables.TbGameBattleConst.EquipPartTypeList;
            var dataList = new List<int> { -1 };
            var captionMap = new Dictionary<int, string> { { -1, "全部" } };

            for (int i = 0; i < partNameList.Count; i++)
            {
                if ((EquipPartType)i == EquipPartType.Weapon)
                {
                    continue;
                }

                dataList.Add(i);
                captionMap[i] = partNameList[i];
            }

            dropdown.OnBindItem += OnPartDropdownBindItem;
            dropdown.onValueChanged.AddListener(OnPartDropdownChanged);
            dropdown.SetCaptionMap(captionMap, false);
            dropdown.SetDataList(dataList, 0);
        }

        private void UnbindPartDropdown()
        {
            var dropdown = mOldPanel.PartDropdown;
            dropdown.onValueChanged.RemoveListener(OnPartDropdownChanged);
            dropdown.OnBindItem -= OnPartDropdownBindItem;
        }

        private void OnPartDropdownChanged(int index)
        {
            VM.OnFilterPartCommand.Execute(mOldPanel.PartDropdown.CurrentData);
        }

        private void OnPartDropdownBindItem(int index, int partValue, GameObject itemObject)
        {
            string caption = partValue < 0
                ? "全部"
                : DataConfigManager.Tables.TbGameBattleConst.EquipPartTypeList[partValue];

            var itemView = itemObject.GetComponent<DropDownItemView>();
            if (itemView != null)
            {
                itemView.SetText(caption);
                return;
            }

            var label = itemObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = caption;
            }
        }

        #endregion

        #region 品质下拉框

        private void InitQualityDropdown()
        {
            var dropdown = mOldPanel.QualityDropdown;
            var qualityNameList = DataConfigManager.Tables.TbGameBattleConst.QualityTypeList;
            var dataList = new List<int>();
            var captionMap = new Dictionary<int, string>();

            dataList.Add((int)QualityType.Default);
            captionMap[(int)QualityType.Default] = "全部";

            for (int i = (int)QualityType.Ordinary; i <= (int)QualityType.Mythology; i++)
            {
                if (i == (int)QualityType.Ultimate || i == (int)QualityType.Mythology)
                {
                    continue;
                }

                dataList.Add(i);
                captionMap[i] = qualityNameList[i];
            }

            dropdown.OnBindItem += OnQualityDropdownBindItem;
            dropdown.onValueChanged.AddListener(OnQualityDropdownChanged);
            dropdown.SetCaptionMap(captionMap, false);
            dropdown.SetDataList(dataList, 0);
        }

        private void UnbindQualityDropdown()
        {
            var dropdown = mOldPanel.QualityDropdown;
            dropdown.onValueChanged.RemoveListener(OnQualityDropdownChanged);
            dropdown.OnBindItem -= OnQualityDropdownBindItem;
        }

        private void OnQualityDropdownChanged(int index)
        {
            VM.OnFilterQualityCommand.Execute((QualityType)mOldPanel.QualityDropdown.CurrentData);
        }

        private void OnQualityDropdownBindItem(int index, int qualityValue, GameObject itemObject)
        {
            string caption = qualityValue == (int)QualityType.Default
                ? "全部"
                : DataConfigManager.Tables.TbGameBattleConst.QualityTypeList[qualityValue];

            var itemView = itemObject.GetComponent<DropDownItemView>();
            if (itemView != null)
            {
                itemView.SetText(caption);
                return;
            }

            var label = itemObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = caption;
            }
        }

        #endregion

        #region UI 初始化

        /// <summary>
        /// 更新奖励显示
        /// </summary>
        private void UpdateRewardDisplay(Dictionary<int, int> rewards)
        {
            var cardScroll = mOldPanel.RewardScroll.transform.Find("Viewport/Content");
            if (cardScroll == null)
            {
                LogTool.LogError("[EquipFragmentPanelNew] RewardScroll 缺少 Viewport/Content");
                return;
            }

            for (int i = 0; i < cardScroll.childCount; i++)
            {
                cardScroll.GetChild(i).gameObject.SetActive(false);
            }

            if (rewards == null) return;

            int idx = 0;
            foreach (var item in rewards)
            {
                GameObject rewObj;
                if (idx >= cardScroll.childCount)
                {
                    var template = cardScroll.Find("RewardEntity");
                    if (template == null)
                    {
                        LogTool.LogError("[EquipFragmentPanelNew] RewardScroll 缺少 RewardEntity 模板");
                        return;
                    }

                    rewObj = Instantiate(template.gameObject, cardScroll, true);
                }
                else
                {
                    rewObj = cardScroll.GetChild(idx).gameObject;
                }

                rewObj.SetActive(true);
                rewObj.GetComponent<RewardEntity>().SetData(item.Key, item.Value);
                idx++;
            }
        }

        #endregion

        #region 事件处理

        private void OnCloseRequested()
        {
            UIFramework.UIManager.ClosePanel(this);
        }

        private void OnFragmentSuccess(List<RewardItem> rewards)
        {
            GlobalFunc.ShowRewardTips(rewards);
            CombatPowerChangeHelper.RecalculateSilent();
            CombatTipController.Instance.SetCombat();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        public void SetData(int soldierId)
        {
            VM.Initialize(soldierId);
            mOldPanel.PartDropdown.SetValue(0, false);
            mOldPanel.QualityDropdown.SetValue(0, false);
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            ShowEquipList();
            UpdateRewardDisplay(VM.FragmentRewards.Value);
            mOldPanel.FragmentBtn.interactable = VM.CanFragment.Value;
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }

        #endregion
    }
}
