using UIFramework;

namespace Game.Common
{
    /// <summary>
    /// 通用详情小弹窗 ViewModel（UIFramework MVVM）。
    /// 由外部调用 Setup(title, detail) 注入要展示的文案。
    /// </summary>
    public class CommonDetailPopPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<string> Title { get; } = new BindableProperty<string>("");
        public BindableProperty<string> Detail { get; } = new BindableProperty<string>("");
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        #endregion

        #region 事件
        public System.Action OnClosePanel;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            OnCloseCommand = new Command(OnClose);
        }

        public void Setup(string title, string detail)
        {
            Title.Value = title;
            Detail.Value = detail;
        }

        private void OnClose()
        {
            if (OnClosePanel != null)
            {
                OnClosePanel.Invoke();
            }
        }
    }
}
