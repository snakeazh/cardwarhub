using FusionContinent.Animation;
using Game.Login;
using UnityEngine;
using UnityEngine.UI;
using UIFramework;
using UIFramework.Services;

namespace FusionContinent
{
    /// <summary>
    /// 注销账号面板 - 新框架版本 (MVVM)
    /// 复用原 CancelAccountPanel 的 UI 组件
    /// </summary>
    [PrefabPath("CancelAccountPanel")]
    public class CancelAccountPanelNew : UIPanel<CancelAccountViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private CancelAccountPanel mOldPanel;

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<CancelAccountPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[CancelAccountPanelNew] 找不到 CancelAccountPanel 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            LogTool.Log("[CancelAccountPanelNew] 已关联 CancelAccountPanel");
        }

        protected override void OnBindViewModel(CancelAccountViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定 ViewModel 事件
            vm.OnClosePanel += OnClosePanel;
            vm.OnShowMessage += OnShowMessage;

            // 点击背景遮罩退出（播放关闭动画后关闭）
            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.AddListener(OnExitBgClicked);
            }

            // 绑定注销按钮（UICommonBtn 通过 SetData.OnClick 绑定）
            if (mOldPanel.CancelBtn != null)
            {
                mOldPanel.CancelBtn.SetData(new ButtonData
                {
                    buttonStyle = ButtonStyleType.ButtonRed,
                    ContentMode = ButtonContentMode.TextOnly,
                    TextStyle = ButtonTextStyle.Default,
                    ButtonDesc = "注销",
                    OnClick = OnCancelBtnClicked,
                });
            }
        }

        protected override void OnUnbindViewModel(CancelAccountViewModel vm)
        {
            // 取消事件订阅
            vm.OnClosePanel -= OnClosePanel;
            vm.OnShowMessage -= OnShowMessage;

            if (mOldPanel == null) return;

            // 清理按钮监听（只移除本类添加的）
            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(OnExitBgClicked);
            }

            if (mOldPanel.CancelBtn != null)
            {
                mOldPanel.CancelBtn.SetData(new ButtonData
                {
                    buttonStyle = ButtonStyleType.ButtonRed,
                    ContentMode = ButtonContentMode.TextOnly,
                    TextStyle = ButtonTextStyle.Default,
                    ButtonDesc = "注销",
                    OnClick = null,
                });
            }
        }

        private void OnExitBgClicked()
        {
            VM.OnCloseCommand.Execute();
        }

        private void OnCancelBtnClicked()
        {
            VM.OnConfirmCancelCommand.Execute();
        }

        private void OnClosePanel()
        {
            CloseSelf();
            // 播放关闭动画后关闭面板
            // PlayCloseAnimation(() => CloseSelf());
        }

        private void OnShowMessage(string message)
        {
            FusionContinent.Toast.Show(message);
        }

        #region 动画

        private void PlayOpenAnimation()
        {
            if (mOldPanel.OpenAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject, null);
            }
        }

        private void PlayCloseAnimation(System.Action onComplete)
        {
            if (mOldPanel.CloseAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, () => onComplete());
            }
            else
            {
                onComplete();
            }
        }

        #endregion

        protected override void OnOpen(IUIData uiData = null)
        {
            // 播放打开动画
            PlayOpenAnimation();
        }

        protected override void OnShow()
        {
            // 面板显示时的逻辑
        }

        protected override void OnHide()
        {
            // 面板隐藏时的逻辑
        }
    }
}
