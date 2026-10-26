using Game.Challenge;
using UIFramework;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("GameLobbyPanel")]
    public class GameLobbyPanelNew : UIPanel<GameLobbyPanelViewModel>
    {
        private GameLobbyPanel mOldPanel;

        void Awake()
        {
            mOldPanel = GetComponent<GameLobbyPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GameLobbyPanelNew] 找不到 GameLobbyPanel 组件");
            }
        }

        protected override void OnBindViewModel(GameLobbyPanelViewModel vm)
        {
            // Mask 关闭（存量 Button，按原类型绑定）
            mOldPanel.Mask.onClick.AddListener(OnMaskClicked);

            // 3 个入口未开放（UIPointerClickAndLongPress，新功能规范）
            mOldPanel.GoldDig.onClick.AddListener(OnNotOpenBtnClicked);
            mOldPanel.JewelMaze.onClick.AddListener(OnNotOpenBtnClicked);
            mOldPanel.SecretAdventure.onClick.AddListener(OnNotOpenBtnClicked);

            vm.OnCloseRequested += OnCloseRequested;
        }

        protected override void OnUnbindViewModel(GameLobbyPanelViewModel vm)
        {
            mOldPanel.Mask.onClick.RemoveListener(OnMaskClicked);
            mOldPanel.GoldDig.onClick.RemoveListener(OnNotOpenBtnClicked);
            mOldPanel.JewelMaze.onClick.RemoveListener(OnNotOpenBtnClicked);
            mOldPanel.SecretAdventure.onClick.RemoveListener(OnNotOpenBtnClicked);
            vm.OnCloseRequested -= OnCloseRequested;
        }

        private void OnMaskClicked()
        {
            VM.RequestClose();
        }

        private void OnCloseRequested()
        {
            CloseSelf();
        }

        private void OnNotOpenBtnClicked()
        {
            Toast.Show("功能未开放");
        }
    }
}
