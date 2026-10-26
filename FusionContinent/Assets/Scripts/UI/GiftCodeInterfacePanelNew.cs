using FusionContinent.Animation;
using Game.GiftCode;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("GiftCodeInterfacePanel")]
    public class GiftCodeInterfacePanelNew : UIPanel<GiftCodeInterfacePanelViewModel>
    {
        private GiftCodeInterfacePanel mOldPanel;

        private UnityEngine.Events.UnityAction mShadowHandler;
        private UnityEngine.Events.UnityAction mExchangeHandler;

        void Awake()
        {
            mOldPanel = GetComponent<GiftCodeInterfacePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GiftCodeInterfacePanelNew] 找不到 GiftCodeInterfacePanel 组件");
                return;
            }
            mOldPanel.enabled = false;

            // 关闭 OpenAnimation 节点上的动画组件（UITweenTimer/UITweenAlpha/UITweenScale），
            // 它们与根节点 Animator 的入场动画冲突
            if (mOldPanel.OpenAnimation != null)
            {
                foreach (var anim in mOldPanel.OpenAnimation.GetComponents<Animation.UIAnimation>())
                {
                    anim.enabled = false;
                }
            }
        }

        protected override void OnBindViewModel(GiftCodeInterfacePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mShadowHandler = () =>
            {
                UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, () =>
                {
                    vm.OnCloseCommand.Execute();
                    CloseSelf();
                });
            };
            mOldPanel.Shadow.onClick.AddListener(mShadowHandler);

            mExchangeHandler = () =>
            {
                var code = mOldPanel.CodeField.text.Trim();
                vm.OnExchangeCommand.Execute(code);
            };
            mOldPanel.ReceiveBtn.onClick.AddListener(mExchangeHandler);

            vm.OnShowError += ShowError;
        }

        protected override void OnUnbindViewModel(GiftCodeInterfacePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mOldPanel.Shadow != null && mShadowHandler != null)
                mOldPanel.Shadow.onClick.RemoveListener(mShadowHandler);
            if (mOldPanel.ReceiveBtn != null && mExchangeHandler != null)
                mOldPanel.ReceiveBtn.onClick.RemoveListener(mExchangeHandler);

            mShadowHandler = null;
            mExchangeHandler = null;

            vm.OnShowError -= ShowError;
        }

        private void ShowError(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            FusionContinent.Toast.Show(message);
        }
    }
}
