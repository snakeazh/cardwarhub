using UIFramework;

namespace FusionContinent
{
    /// <summary>
    /// 融合石合成结果面板 ViewModel
    /// </summary>
    public class FusionCompoundResultPanelViewModel : ViewModel
    {
        /// <summary>
        /// 主宝石流水 id（PlayerFusionGem.Id）
        /// </summary>
        public BindableProperty<int> MainGemId { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 次宝石对应等级配置的 SuccessProbability（合成前快照，用于结果展示）
        /// </summary>
        public BindableProperty<float> SuccessProbability { get; } = new BindableProperty<float>(0f);

        /// <summary>
        /// 合成是否成功
        /// </summary>
        public BindableProperty<bool> IsSuccess { get; } = new BindableProperty<bool>(false);

        #region 命令

        public Command OnCloseCommand { get; private set; }

        #endregion

        #region 事件

        public System.Action OnCloseRequested;

        /// <summary>
        /// 结果展示统一刷新
        /// </summary>
        public System.Action OnPresentationRefreshRequested;

        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            OnCloseCommand = new Command(OnClose);
        }

        public void Initialize(int mainGemId, bool isSuccess)
        {
            MainGemId.Value = mainGemId;
            IsSuccess.Value = isSuccess;
            RefreshPresentation();
        }

        public void RefreshPresentation()
        {
            OnPresentationRefreshRequested?.Invoke();
        }

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }
    }
}
