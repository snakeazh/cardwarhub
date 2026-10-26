using System.Collections.Generic;
using Config;
using Game.Setting;
using ScrollView;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace FusionContinent
{
    [PrefabPath("ChangeHeadPanel")]
    public class ChangeHeadPanelNew : UIPanel<ChangeHeadPanelViewModel>
    {
        private ChangeHeadPanel mOldPanel;
        private PlayerHead mPlayerHead;

        private UnityAction mMaskHandler;
        private UnityAction mCloseBtnHandler;
        private UnityAction mWearBtnHandler;
        private UnityAction<bool> mHeadToggleHandler;
        private UnityAction<bool> mHeadFrameToggleHandler;
        private System.Action<bool, bool> mIsHeadTabChangedHandler;
        private System.Action<int> mWearHeadCompletedHandler;
        private System.Action<int> mWearHeadFrameCompletedHandler;
        private System.Action<string, float> mShowTipHandler;
        private System.Action<string, string> mWearBtnTextChangedHandler;

        private TextMeshProUGUI mWearBtnText;
        private List<HeadItemData> mHeadItems = new List<HeadItemData>();
        private List<HeadItemData> mHeadFrameItems = new List<HeadItemData>();
        private bool mIsHeadTab = true;

        void Awake()
        {
            mOldPanel = GetComponent<ChangeHeadPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[ChangeHeadPanelNew] 找不到 ChangeHeadPanel 组件");
                return;
            }

            mPlayerHead = mOldPanel.PlayerHead != null
                ? mOldPanel.PlayerHead.GetComponent<PlayerHead>()
                : null;
            if (mPlayerHead == null)
            {
                LogTool.LogError("[ChangeHeadPanelNew] PlayerHead 节点上找不到 PlayerHead 组件");
            }

            mWearBtnText = mOldPanel?.WearBtn?.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        }

        protected override void OnBindViewModel(ChangeHeadPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mHeadItems = vm.HeadItems.Value ?? new List<HeadItemData>();
            mHeadFrameItems = vm.HeadFrameItems.Value ?? new List<HeadItemData>();

            mMaskHandler = OnCloseButtonClicked;
            mOldPanel.Mask.onClick.AddListener(mMaskHandler);
            mCloseBtnHandler = OnCloseButtonClicked;
            mOldPanel.CloseBtn.onClick.AddListener(mCloseBtnHandler);

            mWearBtnHandler = () => vm.OnWearCommand.Execute();
            mOldPanel.WearBtn.onClick.AddListener(mWearBtnHandler);

            mHeadToggleHandler = isOn =>
            {
                if (isOn && !mIsHeadTab)
                {
                    OnTabSwitched(true);
                    vm.OnSwitchTabCommand.Execute(true);
                }
            };
            mHeadFrameToggleHandler = isOn =>
            {
                if (isOn && mIsHeadTab)
                {
                    OnTabSwitched(false);
                    vm.OnSwitchTabCommand.Execute(false);
                }
            };
            mOldPanel.Head.onValueChanged.AddListener(mHeadToggleHandler);
            mOldPanel.HeadFrame.onValueChanged.AddListener(mHeadFrameToggleHandler);

            mOldPanel.HeadSCView.SetUpdateFunc(OnUpdateHeadItem);
            mOldPanel.HeadSCView.SetUpdateSelectFunc(OnUpdateHeadSelect);
            mOldPanel.HeadFrameSCView.SetUpdateFunc(OnUpdateHeadFrameItem);
            mOldPanel.HeadFrameSCView.SetUpdateSelectFunc(OnUpdateHeadFrameSelect);

            mIsHeadTabChangedHandler = (o, n) =>
            {
                if (n != mIsHeadTab)
                {
                    OnTabSwitched(n);
                }
            };
            vm.IsHeadTab.OnValueChanged += mIsHeadTabChangedHandler;

            mWearHeadCompletedHandler = OnWearHeadCompleted;
            vm.OnWearHeadCompleted += mWearHeadCompletedHandler;
            mWearHeadFrameCompletedHandler = OnWearHeadFrameCompleted;
            vm.OnWearHeadFrameCompleted += mWearHeadFrameCompletedHandler;
            mShowTipHandler = OnShowTip;
            vm.OnShowTip += mShowTipHandler;
            mWearBtnTextChangedHandler = (_, __) => RefreshWearBtn(vm);
            vm.WearBtnText.OnValueChanged += mWearBtnTextChangedHandler;

            OnTabSwitched(vm.IsHeadTab.Value);
            RefreshWearBtn(vm);
            RefreshHeadList();
            RefreshHeadFrameList();
            RefreshPlayerHeadPreview();
        }

        protected override void OnUnbindViewModel(ChangeHeadPanelViewModel vm)
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
            if (mWearBtnHandler != null)
            {
                mOldPanel.WearBtn.onClick.RemoveListener(mWearBtnHandler);
                mWearBtnHandler = null;
            }
            if (mHeadToggleHandler != null)
            {
                mOldPanel.Head.onValueChanged.RemoveListener(mHeadToggleHandler);
                mHeadToggleHandler = null;
            }
            if (mHeadFrameToggleHandler != null)
            {
                mOldPanel.HeadFrame.onValueChanged.RemoveListener(mHeadFrameToggleHandler);
                mHeadFrameToggleHandler = null;
            }

            if (mIsHeadTabChangedHandler != null)
            {
                vm.IsHeadTab.OnValueChanged -= mIsHeadTabChangedHandler;
                mIsHeadTabChangedHandler = null;
            }
            if (mWearHeadCompletedHandler != null)
            {
                vm.OnWearHeadCompleted -= mWearHeadCompletedHandler;
                mWearHeadCompletedHandler = null;
            }
            if (mWearHeadFrameCompletedHandler != null)
            {
                vm.OnWearHeadFrameCompleted -= mWearHeadFrameCompletedHandler;
                mWearHeadFrameCompletedHandler = null;
            }
            if (mShowTipHandler != null)
            {
                vm.OnShowTip -= mShowTipHandler;
                mShowTipHandler = null;
            }
            if (mWearBtnTextChangedHandler != null)
            {
                vm.WearBtnText.OnValueChanged -= mWearBtnTextChangedHandler;
                mWearBtnTextChangedHandler = null;
            }

            ReleaseScrollViews();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (mOldPanel == null || VM == null) return;
        }

        private void OnUpdateHeadItem(int dataIndex, ScrollItem scrollItem)
        {
            if (dataIndex < 0 || dataIndex >= mHeadItems.Count) return;
            var item = mHeadItems[dataIndex];

            var entity = scrollItem.GetComponent<HeadEntity>();
            if (entity == null) return;

            entity.SetData(new HeadEntityData
            {
                Id = item.Id,
                HeadType = HeadType.Avatar,
                IsWear = item.IsWear,
                IsSelected = item.Id == VM.SelectedHeadId.Value,
                IsUnlocked = item.IsUnlocked,
                OnClick = data => OnHeadItemClicked(data.Id),
            });
        }

        private void OnUpdateHeadSelect(int dataIndex, ScrollItem scrollItem)
        {
            if (dataIndex < 0 || dataIndex >= mHeadItems.Count) return;
            var entity = scrollItem.GetComponent<HeadEntity>();
            if (entity == null) return;

            bool selected = mHeadItems[dataIndex].Id == VM.SelectedHeadId.Value;
            if (entity.IsSelected != null)
            {
                entity.IsSelected.SetActive(selected);
            }
        }

        private void OnUpdateHeadFrameItem(int dataIndex, ScrollItem scrollItem)
        {
            if (dataIndex < 0 || dataIndex >= mHeadFrameItems.Count) return;
            var item = mHeadFrameItems[dataIndex];

            var entity = scrollItem.GetComponent<HeadEntity>();
            if (entity == null) return;

            entity.SetData(new HeadEntityData
            {
                Id = item.Id,
                HeadType = HeadType.AvatarFrame,
                IsWear = item.IsWear,
                IsSelected = item.Id == VM.SelectedHeadFrameId.Value,
                IsUnlocked = item.IsUnlocked,
                OnClick = data => OnHeadFrameItemClicked(data.Id),
            });
        }

        private void OnUpdateHeadFrameSelect(int dataIndex, ScrollItem scrollItem)
        {
            if (dataIndex < 0 || dataIndex >= mHeadFrameItems.Count) return;
            var entity = scrollItem.GetComponent<HeadEntity>();
            if (entity == null) return;

            bool selected = mHeadFrameItems[dataIndex].Id == VM.SelectedHeadFrameId.Value;
            if (entity.IsSelected != null)
            {
                entity.IsSelected.SetActive(selected);
            }
        }

        private void OnHeadItemClicked(int headId)
        {
            VM.OnSelectHeadCommand.Execute(headId);
            mOldPanel.HeadSCView.RefreshSelect();
            RefreshWearBtn(VM);
        }

        private void OnHeadFrameItemClicked(int frameId)
        {
            VM.OnSelectHeadFrameCommand.Execute(frameId);
            mOldPanel.HeadFrameSCView.RefreshSelect();
            RefreshWearBtn(VM);
        }

        private void RefreshWearBtn(ChangeHeadPanelViewModel vm)
        {
            if (mOldPanel?.WearBtn == null || vm == null) return;

            HeadItemData selected = null;
            if (mIsHeadTab)
            {
                foreach (var item in mHeadItems)
                {
                    if (item.Id == vm.SelectedHeadId.Value)
                    {
                        selected = item;
                        break;
                    }
                }
            }
            else
            {
                foreach (var item in mHeadFrameItems)
                {
                    if (item.Id == vm.SelectedHeadFrameId.Value)
                    {
                        selected = item;
                        break;
                    }
                }
            }

            var canWear = selected != null && selected.IsUnlocked && !selected.IsWear;
            mOldPanel.WearBtn.interactable = canWear;

            if (mWearBtnText != null)
            {
                mWearBtnText.text = vm.WearBtnText.Value;
            }
        }

        private void OnTabSwitched(bool isHeadTab)
        {
            mIsHeadTab = isHeadTab;
            mOldPanel.HeadSCView.gameObject.SetActive(isHeadTab);
            mOldPanel.HeadFrameSCView.gameObject.SetActive(!isHeadTab);

            if (mOldPanel.Head != null && mOldPanel.Head.isOn != isHeadTab)
            {
                mOldPanel.Head.isOn = isHeadTab;
            }
            if (mOldPanel.HeadFrame != null && mOldPanel.HeadFrame.isOn == isHeadTab)
            {
                mOldPanel.HeadFrame.isOn = !isHeadTab;
            }

            RefreshWearBtn(VM);
        }

        private void OnCloseButtonClicked()
        {
            CloseSelf();
        }

        private void OnWearHeadCompleted(int headId)
        {
            RefreshHeadList();
            RefreshPlayerHeadPreview();
            RefreshWearBtn(VM);
        }

        private void OnWearHeadFrameCompleted(int frameId)
        {
            RefreshHeadFrameList();
            RefreshPlayerHeadPreview();
            RefreshWearBtn(VM);
        }

        private void OnShowTip(string message, float duration)
        {
            Toast.Show(message, duration);
        }

        private void RefreshHeadList()
        {
            if (mOldPanel.HeadSCView == null) return;
            mOldPanel.HeadSCView.ShowList(0);
            mOldPanel.HeadSCView.ShowList(mHeadItems.Count);
            mOldPanel.HeadSCView.RefreshSelect();
        }

        private void RefreshHeadFrameList()
        {
            if (mOldPanel.HeadFrameSCView == null) return;
            mOldPanel.HeadFrameSCView.ShowList(0);
            mOldPanel.HeadFrameSCView.ShowList(mHeadFrameItems.Count);
            mOldPanel.HeadFrameSCView.RefreshSelect();
        }

        private void RefreshPlayerHeadPreview()
        {
            if (mPlayerHead == null) return;
            mPlayerHead.SetData(Global.User);
        }

        private void ReleaseScrollViews()
        {
            if (mOldPanel.HeadSCView != null)
            {
                mOldPanel.HeadSCView.ShowList(0);
                mOldPanel.HeadSCView.SetUpdateFunc(null);
                mOldPanel.HeadSCView.SetUpdateSelectFunc(null);
            }
            if (mOldPanel.HeadFrameSCView != null)
            {
                mOldPanel.HeadFrameSCView.ShowList(0);
                mOldPanel.HeadFrameSCView.SetUpdateSelectFunc(null);
                mOldPanel.HeadFrameSCView.SetUpdateFunc(null);
            }
        }
    }
}
