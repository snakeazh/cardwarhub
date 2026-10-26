using System;
using FusionContinent.Animation;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    [PrefabPath("ConfirmDialogPanel")]
    public class ConfirmDialogPanelNew : UIPanel<ConfirmDialogPanelViewModel>
    {
        private ConfirmDialogPanel mOldPanel;

        private UnityEngine.Events.UnityAction mConfirmHandler;
        private UnityEngine.Events.UnityAction mCancelHandler;
        private UnityEngine.Events.UnityAction mOptionHandler;
        private UnityEngine.Events.UnityAction mMaskHandler;
        private bool _isClosing;

        void Awake()
        {
            mOldPanel = GetComponent<ConfirmDialogPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[ConfirmDialogPanelNew] 找不到 ConfirmDialogPanel 组件");
                return;
            }

            // 禁用旧 Panel 组件，避免潜在的生命周期冲突
            mOldPanel.enabled = false;
        }

        protected override void OnBindViewModel(ConfirmDialogPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mConfirmHandler = () => vm.OnConfirmCommand.Execute();
            mCancelHandler = () => vm.OnCancelCommand.Execute();
            mOptionHandler = () =>
            {
                // 即时切换 Select 勾选视觉（与旧代码行为一致）
                var selectNode = mOldPanel.OptionBtn.transform.Find("Select");
                if (selectNode != null)
                {
                    selectNode.gameObject.SetActive(!selectNode.gameObject.activeSelf);
                }
                // VM 命令同步更新 Global.ConfirmDialogPanelStatus
                vm.OnToggleOptionCommand.Execute();
            };
            mMaskHandler = () => vm.OnCancelCommand.Execute();

            mOldPanel.ConfirmBtn.onClick.RemoveAllListeners();
            mOldPanel.CancelBtn.onClick.RemoveAllListeners();
            mOldPanel.OptionBtn.onClick.RemoveAllListeners();
            mOldPanel.Mask.onClick.RemoveAllListeners();

            mOldPanel.ConfirmBtn.onClick.AddListener(mConfirmHandler);
            mOldPanel.CancelBtn.onClick.AddListener(mCancelHandler);
            mOldPanel.OptionBtn.onClick.AddListener(mOptionHandler);
            mOldPanel.Mask.onClick.AddListener(mMaskHandler);
        }

        protected override void OnUnbindViewModel(ConfirmDialogPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mConfirmHandler != null)
            {
                mOldPanel.ConfirmBtn.onClick.RemoveListener(mConfirmHandler);
                mConfirmHandler = null;
            }
            if (mCancelHandler != null)
            {
                mOldPanel.CancelBtn.onClick.RemoveListener(mCancelHandler);
                mCancelHandler = null;
            }
            if (mOptionHandler != null)
            {
                mOldPanel.OptionBtn.onClick.RemoveListener(mOptionHandler);
                mOptionHandler = null;
            }
            if (mMaskHandler != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(mMaskHandler);
                mMaskHandler = null;
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (VM == null || mOldPanel == null) return;



            _isClosing = false;

            // 打开动画
            if (mOldPanel.OpenAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject);
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            var vm = VM;
            var hasMessage = !string.IsNullOrEmpty(vm.Message);

            mOldPanel.TitleText.text = vm.Title;
            mOldPanel.Message.text = vm.Message;
            mOldPanel.DetailBox.gameObject.SetActive(hasMessage);
            mOldPanel.BasePlate.Find("Bg").gameObject.SetActive(hasMessage);
            mOldPanel.BasePlate.Find("TitleBg").gameObject.SetActive(!hasMessage);
            mOldPanel.transform.Find("TweenTarget/Foot").transform.localPosition = hasMessage
                ? new Vector3(0, -143f, 0)
                : new Vector3(0, 38f, 0);

            mOldPanel.CancelBtn.interactable = !vm.NoCancel;

            // 同步 Option 勾选视觉状态
            var selectNode = mOldPanel.OptionBtn.transform.Find("Select");
            if (selectNode != null)
            {
                selectNode.gameObject.SetActive(Global.ConfirmDialogPanelStatus == 2);
            }

            // 确认/取消回调后关闭面板
            vm.OnConfirmRequested += CloseSelf;
            vm.OnCancelRequested += CloseSelf;
        }

        private void CloseSelf()
        {
            if (_isClosing) return;

            if (VM != null)
            {
                VM.OnConfirmRequested -= CloseSelf;
                VM.OnCancelRequested -= CloseSelf;
            }

            base.CloseSelf();
            // if (mOldPanel.CloseAnimation != null)
            // {
            //     _isClosing = true;
            //     UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, () => base.CloseSelf());
            // }
            // else
            // {
            //     base.CloseSelf();
            // }
        }
    }
}
