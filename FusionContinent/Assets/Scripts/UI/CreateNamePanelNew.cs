using Game.Login;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 创建角色名面板 - 使用新 UIFramework (MVVM版本)
    /// 复用原 CreateNamePanel 的 UI 组件
    /// </summary>
    [PrefabPath("CreateNamePanel")]
    public class CreateNamePanelNew : UIPanel<CreateNamePanelViewModel>
    {
        private CreateNamePanel mOldPanel;

        private System.Action<string, string> mUserNameChangedHandler;
        private System.Action<bool, bool> mIsBusyChangedHandler;
        private System.Action<bool, bool> mIsStartEnabledChangedHandler;

        void Awake()
        {
            mOldPanel = GetComponent<CreateNamePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[CreateNamePanelNew] 找不到 CreateNamePanel 组件！请确保预制体上同时挂载了两个脚本");
            }
        }

        protected override void OnBindViewModel(CreateNamePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mUserNameChangedHandler = (_, newVal) =>
            {
                if (mOldPanel.Inputnametxt != null && mOldPanel.Inputnametxt.text != newVal)
                {
                    mOldPanel.Inputnametxt.text = newVal;
                }
            };

            mIsBusyChangedHandler = (_, __) => UpdateStartButtonState(vm);
            mIsStartEnabledChangedHandler = (_, __) => UpdateStartButtonState(vm);

            vm.UserName.OnValueChanged += mUserNameChangedHandler;
            vm.IsBusy.OnValueChanged += mIsBusyChangedHandler;
            vm.IsStartEnabled.OnValueChanged += mIsStartEnabledChangedHandler;
            vm.OnShowTip += OnShowTip;
            vm.OnCloseRequested += OnCloseRequested;

            if (mOldPanel.Inputnametxt != null)
            {
                mOldPanel.Inputnametxt.onValueChanged.AddListener(value => vm.OnNameChangedCommand.Execute(value));
            }

            if (mOldPanel.CleanNameBtn != null)
            {
                mOldPanel.CleanNameBtn.onClick.AddListener(() => vm.OnClearNameCommand.Execute());
            }

            if (mOldPanel.ConfirmBtn != null)
            {
                mOldPanel.ConfirmBtn.onClick.AddListener(() => vm.OnConfirmCommand.Execute());
            }

            if (mOldPanel.Inputnametxt != null)
            {
                mOldPanel.Inputnametxt.text = vm.UserName.Value;
            }
            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.AddListener(() => CloseSelf());
            }

            UpdateStartButtonState(vm);
        }

        protected override void OnUnbindViewModel(CreateNamePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mUserNameChangedHandler != null)
            {
                vm.UserName.OnValueChanged -= mUserNameChangedHandler;
                mUserNameChangedHandler = null;
            }

            if (mIsBusyChangedHandler != null)
            {
                vm.IsBusy.OnValueChanged -= mIsBusyChangedHandler;
                mIsBusyChangedHandler = null;
            }

            if (mIsStartEnabledChangedHandler != null)
            {
                vm.IsStartEnabled.OnValueChanged -= mIsStartEnabledChangedHandler;
                mIsStartEnabledChangedHandler = null;
            }

            vm.OnShowTip -= OnShowTip;
            vm.OnCloseRequested -= OnCloseRequested;

            if (mOldPanel.Inputnametxt != null) mOldPanel.Inputnametxt.onValueChanged.RemoveAllListeners();
            if (mOldPanel.CleanNameBtn != null) mOldPanel.CleanNameBtn.onClick.RemoveAllListeners();
            if (mOldPanel.ConfirmBtn != null) mOldPanel.ConfirmBtn.onClick.RemoveAllListeners();
            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.RemoveAllListeners();
            }
        }

        private void UpdateStartButtonState(CreateNamePanelViewModel vm)
        {
            if (mOldPanel?.ConfirmBtn == null) return;
            mOldPanel.ConfirmBtn.interactable = vm.IsStartEnabled.Value && !vm.IsBusy.Value;
        }

        private void OnShowTip(string message, float duration, bool useAnimation)
        {
            if (string.IsNullOrEmpty(message)) return;
            FusionContinent.Toast.Show(message);
        }

        private void OnCloseRequested()
        {
            CloseSelf();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }
    }
}
