using UnityEngine;
using UIFramework;
using UnityEngine.UI;
using Assets.Scripts.Localization;
using Config;
using System;
using System.Collections.Generic;
using FusionContinent.Model;
using Cysharp.Threading.Tasks;
using Game.Equip;
using Game.Equip.RedDot;
using ScrollView;

namespace FusionContinent
{
    /// <summary>
    /// 融合石详情面板 - MVVM架构
    /// 复用原 FusionGemDetailPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("FusionGemDetailPanel")]
    public class FusionGemDetailPanelNew : UIPanel<FusionGemDetailPanelViewModel>
    {
        private FusionGemDetailPanel mOldPanel;
        private UIVerticalScrollViewLock mPropertyScrollView;

        private System.Action<Sprite, Sprite> mIconChangedHandler;
        private System.Action<string, string> mNameChangedHandler;
        private System.Action<string, string> mQualityKeyChangedHandler;
        private System.Action<bool, bool> mIsWearChangedHandler;
        private System.Action<EquipPartType, EquipPartType> mSelectedPartTypeChangedHandler;
        private System.Action<object> mFusionGemWearChangedHandler;
        private System.Action<List<DisplayAttributeRow>, List<DisplayAttributeRow>> mAttributesChangedHandler;

        private UnityEngine.Events.UnityAction mWearClickHandler;
        private UnityEngine.Events.UnityAction mUnWearClickHandler;
        private readonly List<GemWearSlot> mWearSlots = new List<GemWearSlot>();
        private readonly List<Button> mWearSlotButtons = new List<Button>();
        private readonly List<UnityEngine.Events.UnityAction> mWearSlotClickHandlers = new List<UnityEngine.Events.UnityAction>();

        #region 生命周期

        void Awake()
        {
            mOldPanel = GetComponent<FusionGemDetailPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[FusionGemDetailPanelNew] 找不到 FusionGemDetailPanel 组件！");
                return;
            }

            LogTool.Log("[FusionGemDetailPanelNew] 已关联 FusionGemDetailPanel");
        }

        protected override void OnBindViewModel(FusionGemDetailPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // Icon
            mIconChangedHandler = (_, newVal) =>
            {
                if (mOldPanel.GemIcon != null)
                {
                    mOldPanel.GemIcon.sprite = newVal;
                    mOldPanel.GemIcon.enabled = newVal != null;
                }
            };
            vm.GemIconSprite.OnValueChanged += mIconChangedHandler;
            mIconChangedHandler(null, vm.GemIconSprite.Value);

            // Name
            mNameChangedHandler = (_, newVal) =>
            {
                if (mOldPanel.GemNameText != null)
                {
                    mOldPanel.GemNameText.text = newVal ?? string.Empty;
                }
            };
            vm.GemName.OnValueChanged += mNameChangedHandler;
            mNameChangedHandler(null, vm.GemName.Value);

            // Quality（vm 存的是本地化key）
            mQualityKeyChangedHandler = (_, newKey) =>
            {
                if (mOldPanel.GemQualityText == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(newKey))
                {
                    mOldPanel.GemQualityText.text = string.Empty;
                    return;
                }

                mOldPanel.GemQualityText.text = newKey;
            };
            vm.GemQualityTextKey.OnValueChanged += mQualityKeyChangedHandler;
            mQualityKeyChangedHandler(null, vm.GemQualityTextKey.Value);

            // Wear state -> 节点显隐 + 按钮显隐
            mIsWearChangedHandler = (_, isWear) =>
            {
                if (mOldPanel.GemIsWear != null)
                {
                    mOldPanel.GemIsWear.gameObject.SetActive(isWear);
                }

                if (mOldPanel.WearBtn != null) mOldPanel.WearBtn.gameObject.SetActive(!isWear);
                if (mOldPanel.UnWearBtn != null) mOldPanel.UnWearBtn.gameObject.SetActive(isWear);
            };
            vm.IsWear.OnValueChanged += mIsWearChangedHandler;
            mIsWearChangedHandler(false, vm.IsWear.Value);

            InitPartSlots(vm);
            mSelectedPartTypeChangedHandler = (_, newVal) => RefreshPartSlotSelection(newVal);
            vm.SelectedPartType.OnValueChanged += mSelectedPartTypeChangedHandler;
            RefreshPartSlotSelection(vm.SelectedPartType.Value);

            mFusionGemWearChangedHandler = _ =>
            {
                RefreshPartSlots(vm);
                RefreshWashBtnRedDot();
            };
            Global.Publisher.Subscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, mFusionGemWearChangedHandler);

            // PropertyScrollView - 属性列表
            BindPropertyScrollView();

            mAttributesChangedHandler = (_, newVal) => RefreshPropertyScrollView(newVal);
            vm.GemAttributes.OnValueChanged += mAttributesChangedHandler;
            RefreshPropertyScrollView(vm.GemAttributes.Value);

            // Buttons（事件先空，但链路先接好）
            if (mOldPanel.WearBtn != null)
            {
                mWearClickHandler = () => vm.OnWearCommand?.Execute();
                mOldPanel.WearBtn.onClick.AddListener(mWearClickHandler);
            }

            if (mOldPanel.UnWearBtn != null)
            {
                mUnWearClickHandler = () => vm.OnUnWearCommand?.Execute();
                mOldPanel.UnWearBtn.onClick.AddListener(mUnWearClickHandler);
            }

            if (mOldPanel.WashBtn != null)
            {
                mOldPanel.WashBtn.onClick.AddListener(OnWashBtnClicked);
            }
            
            mOldPanel.Close.onClick.AddListener(() =>
            {
                CloseSelf();
            });

            mOldPanel.BottomMask.onClick.AddListener(() =>
            {
                CloseSelf();
            });
            // 兜底：确保打开瞬间 UI 状态与 VM 一致
            ApplyFromVM(vm);
        }

        protected override void OnUnbindViewModel(FusionGemDetailPanelViewModel vm)
        {
            if (mIconChangedHandler != null)
            {
                vm.GemIconSprite.OnValueChanged -= mIconChangedHandler;
                mIconChangedHandler = null;
            }

            if (mNameChangedHandler != null)
            {
                vm.GemName.OnValueChanged -= mNameChangedHandler;
                mNameChangedHandler = null;
            }

            if (mQualityKeyChangedHandler != null)
            {
                vm.GemQualityTextKey.OnValueChanged -= mQualityKeyChangedHandler;
                mQualityKeyChangedHandler = null;
            }

            if (mIsWearChangedHandler != null)
            {
                vm.IsWear.OnValueChanged -= mIsWearChangedHandler;
                mIsWearChangedHandler = null;
            }

            if (mSelectedPartTypeChangedHandler != null)
            {
                vm.SelectedPartType.OnValueChanged -= mSelectedPartTypeChangedHandler;
                mSelectedPartTypeChangedHandler = null;
            }


            Global.Publisher.Unsubscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, mFusionGemWearChangedHandler);
            if (mFusionGemWearChangedHandler != null)
            {
                mFusionGemWearChangedHandler = null;
            }

            if (mAttributesChangedHandler != null)
            {
                vm.GemAttributes.OnValueChanged -= mAttributesChangedHandler;
                mAttributesChangedHandler = null;
            }

            ReleasePropertyScrollView();
            ClearPartSlotClickHandlers();

            if (mOldPanel.WearBtn != null && mWearClickHandler != null)
            {
                mOldPanel.WearBtn.onClick.RemoveListener(mWearClickHandler);
                mWearClickHandler = null;
            }

            if (mOldPanel.UnWearBtn != null && mUnWearClickHandler != null)
            {
                mOldPanel.UnWearBtn.onClick.RemoveListener(mUnWearClickHandler);
                mUnWearClickHandler = null;
            }

            if (mOldPanel.WashBtn != null)
            {
                mOldPanel.WashBtn.onClick.RemoveListener(OnWashBtnClicked);
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            
            // 打开时强制刷新一次穿戴状态（避免外部穿戴变化导致显示不一致）
            VM?.Refresh();
            ApplyFromVM(VM);
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.FusionGemSlot,4,mOldPanel.WearBtn.GetComponent<RectTransform>());
            mOldPanel.WashBtn.gameObject.SetActive(!Global.PlayerFusionGems[this.VM.GemId.Value].IsMaxLevel());
            RefreshWashBtnRedDot();
        }

        public override void Show()
        {
            base.Show();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        public void SetData(int gemId)
        {
            if (VM != null)
            {
                VM.Initialize(gemId);
                // 兜底：如果 Initialize 发生在打开后，强制同步一次按钮显隐
                ApplyFromVM(VM);
                if (mOldPanel.WashBtn != null && Global.PlayerFusionGems.TryGetValue(gemId, out var gem))
                {
                    mOldPanel.WashBtn.gameObject.SetActive(!gem.IsMaxLevel());
                }
                RefreshWashBtnRedDot();
            }
        }

        #endregion
        
        private void ApplyFromVM(FusionGemDetailPanelViewModel vm)
        {
            if (vm == null || mOldPanel == null)
            {
                return;
            }

            mIconChangedHandler?.Invoke(null, vm.GemIconSprite.Value);
            mNameChangedHandler?.Invoke(null, vm.GemName.Value);
            mQualityKeyChangedHandler?.Invoke(null, vm.GemQualityTextKey.Value);
            mIsWearChangedHandler?.Invoke(false, vm.IsWear.Value);
            RefreshPartSlots(vm);
            RefreshPartSlotSelection(vm.SelectedPartType.Value);
            RefreshWashBtnRedDot();
        }

        private void RefreshWashBtnRedDot()
        {
            if (mOldPanel?.WashBtn == null || VM == null)
            {
                return;
            }

            int show = FusionGemBreakthroughRedDotHelper.CanBreakthrough(VM.GemId.Value) ? 1 : 0;
            ((RectTransform)mOldPanel.WashBtn.transform).SetLocalRedType(show);
        }

        private void InitPartSlots(FusionGemDetailPanelViewModel vm)
        {
            if (mOldPanel.PartContent == null)
            {
                return;
            }

            ClearPartSlotClickHandlers();
            mWearSlots.Clear();
            mWearSlotButtons.Clear();

            var partTypes = GetWearablePartTypes();
            if (partTypes.Count == 0)
            {
                return;
            }

            Transform template = mOldPanel.PartContent.childCount > 0 ? mOldPanel.PartContent.GetChild(0) : null;
            if (template == null)
            {
                return;
            }

            while (mOldPanel.PartContent.childCount < partTypes.Count)
            {
                Instantiate(template.gameObject, mOldPanel.PartContent);
            }

            for (int i = 0; i < mOldPanel.PartContent.childCount; i++)
            {
                var child = mOldPanel.PartContent.GetChild(i);
                bool active = i < partTypes.Count;
                child.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var slot = child.GetComponent<GemWearSlot>();
                if (slot == null)
                {
                    slot = child.GetComponentInChildren<GemWearSlot>(true);
                }

                if (slot == null)
                {
                    continue;
                }

                var partType = partTypes[i];
                slot.SetData(partType, vm.GemId.Value);
                mWearSlots.Add(slot);

                var button = child.GetComponent<Button>();
                if (button == null)
                {
                    button = child.GetComponentInChildren<Button>(true);
                }

                if (button != null)
                {
                    UnityEngine.Events.UnityAction clickHandler = () => vm.SelectPart(partType);
                    button.onClick.AddListener(clickHandler);
                    mWearSlotButtons.Add(button);
                    mWearSlotClickHandlers.Add(clickHandler);
                }
            }
        }

        private void RefreshPartSlots(FusionGemDetailPanelViewModel vm)
        {
            if (vm == null)
            {
                return;
            }

            if (mWearSlots.Count == 0)
            {
                InitPartSlots(vm);
                return;
            }

            var partTypes = GetWearablePartTypes();
            for (int i = 0; i < mWearSlots.Count && i < partTypes.Count; i++)
            {
                mWearSlots[i].SetData(partTypes[i], vm.GemId.Value);
            }
        }

        private void RefreshPartSlotSelection(EquipPartType selectedPartType)
        {
            for (int i = 0; i < mWearSlots.Count; i++)
            {
                mWearSlots[i].SetSelected(mWearSlots[i].PartType == selectedPartType);
            }
        }

        private void ClearPartSlotClickHandlers()
        {
            for (int i = 0; i < mWearSlotButtons.Count && i < mWearSlotClickHandlers.Count; i++)
            {
                if (mWearSlotButtons[i] != null && mWearSlotClickHandlers[i] != null)
                {
                    mWearSlotButtons[i].onClick.RemoveListener(mWearSlotClickHandlers[i]);
                }
            }

            mWearSlotButtons.Clear();
            mWearSlotClickHandlers.Clear();
        }

        private List<EquipPartType> GetWearablePartTypes()
        {
            var partTypes = new List<EquipPartType>();
            foreach (EquipPartType partType in Enum.GetValues(typeof(EquipPartType)))
            {
                if (partType != EquipPartType.Weapon)
                {
                    partTypes.Add(partType);
                }
            }

            return partTypes;
        }

        #region PropertyScrollView

        private void BindPropertyScrollView()
        {
            if (mOldPanel?.PropertyScrollView == null)
            {
                return;
            }

            mPropertyScrollView = mOldPanel.PropertyScrollView;
            mPropertyScrollView.SetUpdateFunc(OnUpdatePropertyItem);
        }

        private void ReleasePropertyScrollView()
        {
            if (mPropertyScrollView != null)
            {
                mPropertyScrollView.ReleaseScrollView();
                mPropertyScrollView = null;
            }
        }

        private void RefreshPropertyScrollView(List<DisplayAttributeRow> rows)
        {
            if (mPropertyScrollView == null)
            {
                return;
            }

            mPropertyScrollView.ShowList(rows?.Count ?? 0);
        }

        private void OnUpdatePropertyItem(int dataIndex, ScrollItem scrollItem)
        {
            var rows = VM?.GemAttributes.Value;
            if (rows == null || dataIndex < 0 || dataIndex >= rows.Count)
            {
                return;
            }

            var row = rows[dataIndex];
            var locText = scrollItem.GetComponentInChildren<LocalizationText>(true);
            if (locText != null && locText.TextUGUI != null)
            {
                // 不走多语言，直接显示 Desc 文本内容
                locText.TextUGUI.text = row.NameKey;
            }
        }

        #endregion

        private void OnWashBtnClicked()
        {
            var mainGemId = VM?.GemId.Value ?? 0;
            if (mainGemId <= 0)
            {
                Toast.Error("融合石数据无效");
                return;
            }

            if (Global.PlayerFusionGems == null || !Global.PlayerFusionGems.ContainsKey(mainGemId))
            {
                Toast.Error("融合石数据不存在");
                return;
            }

            CloseSelf();
            var compoundVm = new FusionGemCompoundPanelViewModel();
            compoundVm.OnCreate();
            compoundVm.Initialize(mainGemId);
            UIFramework.UIManager
                .OpenPanelAsync<FusionGemCompoundPanelNew, FusionGemCompoundPanelViewModel>(
                    compoundVm, UIFramework.UILevel.Panel)
                .Forget();
        }
    }
}
