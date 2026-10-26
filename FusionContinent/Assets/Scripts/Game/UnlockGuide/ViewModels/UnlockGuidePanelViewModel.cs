using UIFramework;

namespace Game.UnlockGuide
{
    /// <summary>
    /// 解锁引导面板 ViewModel。
    /// 持有当前步骤号（UnlockGuideManager 读写），按钮意图通过 Command 表达。
    /// </summary>
    public class UnlockGuidePanelViewModel : ViewModel
    {
        #region 属性
        /// <summary>当前引导步骤 StepId</summary>
        public BindableProperty<int> CurrentStep { get; } = new BindableProperty<int>(1);
        #endregion

        #region 命令
        public Command OnSkipCommand { get; private set; }
        public Command OnNextCommand { get; private set; }
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            OnSkipCommand = new Command(OnSkip);
            OnNextCommand = new Command(OnNext);
        }

        private void OnSkip()
        {
            FusionContinent.UnlockGuideManager.Instance.ForceCompleteGuide();
        }

        private void OnNext()
        {
            FusionContinent.UnlockGuideManager.Send(
                FusionContinent.UnlockGuideManager.Instance.CurrentModuleFunction, CurrentStep.Value);
        }
    }
}
