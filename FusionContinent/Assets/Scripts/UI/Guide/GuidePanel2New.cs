using DG.Tweening;
using FusionContinent.Guide;
using Game.Guide;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    /// <summary>
    /// 引导面板（MVVM）。
    /// 复用旧 GuidePanel2 的 UI 引用，通过转发属性向各 GuideMethod 暴露节点
    /// （GuideContext.Panel 已切换为本类型，GuideMethod 不需要改动）。
    /// 打开方式：UIManager.OpenPanelAsync（UIScene）；战斗场景内实例由
    /// GuideManager.SetInWarPanel 手动 Init 绑定 ViewModel。
    /// </summary>
    [PrefabPath("GuidePanel2")]
    public class GuidePanel2New : UIPanel<GuidePanel2ViewModel>
    {
        private GuidePanel2 mOldPanel;

        #region 节点转发（供 GuideMethod 使用）

        public RectTransform Finger => mOldPanel.Finger;
        public RectTransform Maskabled => mOldPanel.Maskabled;
        public RectTransform Maskabled2 => mOldPanel.Maskabled2;
        public Image Mask => mOldPanel.Mask;
        public RectTransform MaskCard => mOldPanel.MaskCard;
        public RectTransform NextBtn => mOldPanel.NextBtn;
        public RectTransform Dialoge => mOldPanel.Dialoge;
        public TextMeshProUGUI Text => mOldPanel.Text;
        public RectTransform Arrow1 => mOldPanel.Arrow1;
        public RectTransform Arrow2 => mOldPanel.Arrow2;
        public RectTransform GuideTipsMessage1 => mOldPanel.GuideTipsMessage1;
        public RectTransform GuideTipsMessage2 => mOldPanel.GuideTipsMessage2;

        #endregion

        protected override void OnInit(IUIData uiData)
        {
            mOldPanel = GetComponent<GuidePanel2>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GuidePanel2New] 缺少 GuidePanel2 组件");
            }

            base.OnInit(uiData);
        }

        protected override void OnBindViewModel(GuidePanel2ViewModel vm)
        {
        }

        protected override void OnUnbindViewModel(GuidePanel2ViewModel vm)
        {
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            mOldPanel.GuideTipsMessage1.gameObject.SetActive(false);
            mOldPanel.GuideTipsMessage2.gameObject.SetActive(false);

            VM.Current.Init(new GuideContext()
            {
                Panel = this,
            }, VM.Step);
        }

        protected override void OnShow()
        {
            PlayAnimation();
        }

        /// <summary>引导面板不驱动 UserTool（战斗内场景实例不走 UIManager 层级，避免误隐藏）</summary>
        protected override bool ShouldUpdateUserToolOnBecameVisible()
        {
            return false;
        }

        public void PlayAnimation()
        {
            mOldPanel.Dialoge.DOKill(true);
            mOldPanel.Dialoge.localScale = Vector3.zero;
            mOldPanel.Dialoge.DOScale(Vector3.one, 0.3f).SetUpdate(true);
        }
    }
}
