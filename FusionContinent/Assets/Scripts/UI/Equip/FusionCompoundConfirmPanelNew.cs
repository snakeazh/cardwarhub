using Cysharp.Threading.Tasks;
using FusionContinent.Model;
using UnityEngine;
using UIFramework;

namespace FusionContinent
{
    /// <summary>
    /// 融合石合成确认面板 - MVVM架构
    /// 复用原 FusionCompoundConfirmPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("FusionCompoundConfirmPanel")]
    public class FusionCompoundConfirmPanelNew : UIPanel<FusionCompoundConfirmPanelViewModel>
    {
        private FusionCompoundConfirmPanel mOldPanel;

        #region 生命周期

        void Awake()
        {
            mOldPanel = GetComponent<FusionCompoundConfirmPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[FusionCompoundConfirmPanelNew] 找不到 FusionCompoundConfirmPanel 组件！");
                return;
            }

            LogTool.Log("[FusionCompoundConfirmPanelNew] 已关联 FusionCompoundConfirmPanel");
        }

        protected override void OnBindViewModel(FusionCompoundConfirmPanelViewModel vm)
        {
            if (mOldPanel == null)
            {
                return;
            }

            mOldPanel.CompoundBtn.onClick.AddListener(OnCompoundButtonClicked);
            mOldPanel.CloseBtn.onClick.AddListener(OnCloseButtonClicked);

            vm.OnCompoundRequested += OnCompoundRequested;
            vm.OnCloseRequested += OnCloseRequested;
        }

        protected override void OnUnbindViewModel(FusionCompoundConfirmPanelViewModel vm)
        {
            if (mOldPanel != null)
            {
                mOldPanel.CompoundBtn.onClick.RemoveListener(OnCompoundButtonClicked);
                mOldPanel.CloseBtn.onClick.RemoveListener(OnCloseButtonClicked);
            }

            vm.OnCompoundRequested -= OnCompoundRequested;
            vm.OnCloseRequested -= OnCloseRequested;
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            RefreshPresentation(VM);
        }

        public override void Show()
        {
            base.Show();
        }

        protected override void OnClose()
        {
        }

        #endregion

        #region 按钮

        private void OnCompoundButtonClicked()
        {
            VM.OnCompoundCommand.Execute();
        }

        private void OnCloseButtonClicked()
        {
            VM.OnCloseCommand.Execute();
        }

        #endregion

        #region 事件处理

        private void OnCompoundRequested(bool success)
        {
            var mainGemId = VM.MainGemId.Value;
            if (mainGemId <= 0)
            {
                return;
            }

            Global.Publisher.Publish(MessagePublisher.FUSION_GEM_WEAR_CHANGED, mainGemId);
            CloseSelf();

            var resultVm = new FusionCompoundResultPanelViewModel();
            resultVm.OnCreate();
            resultVm.Initialize(mainGemId, success);
            UIFramework.UIManager
                .OpenPanelAsync<FusionCompoundResultPanelNew, FusionCompoundResultPanelViewModel>(
                    resultVm, UIFramework.UILevel.PopUI)
                .Forget();
        }

        private void OnCloseRequested()
        {
            CloseSelf();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        public void SetData(int mainGemId, int secondaryGemId)
        {
            VM.Initialize(mainGemId, secondaryGemId);
            RefreshPresentation(VM);
        }

        #endregion

        #region 展示刷新

        private void RefreshPresentation(FusionCompoundConfirmPanelViewModel vm)
        {
            var secondaryGem = Global.PlayerFusionGems[vm.SecondaryGemId.Value];
            
            var mainGem = Global.PlayerFusionGems[vm.MainGemId.Value];

            var mainCfg = mainGem.GetLevelCfg();
            var Probability = mainCfg.SuccessProbability[mainGem.Level - secondaryGem.Level];
            
            var percent = Mathf.RoundToInt(Probability * 100f);
            mOldPanel.ProbabilityText.text = $"{percent}%";
        }

        #endregion
    }
}
