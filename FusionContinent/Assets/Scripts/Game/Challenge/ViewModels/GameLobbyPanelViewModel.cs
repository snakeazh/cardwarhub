using UIFramework;

namespace Game.Challenge
{
    /// <summary>
    /// 游戏大厅面板 ViewModel（入口集合，目前仅承载关闭事件）。
    /// </summary>
    public class GameLobbyPanelViewModel : ViewModel
    {
        #region 事件
        public System.Action OnCloseRequested;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public void RequestClose()
        {
            OnCloseRequested?.Invoke();
        }
    }
}
