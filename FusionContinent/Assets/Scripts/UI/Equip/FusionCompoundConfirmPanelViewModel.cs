using System;
using Game.Equip;
using UIFramework;
using UIFramework.Services;

namespace FusionContinent
{
    /// <summary>
    /// 融合石合成确认面板 ViewModel
    /// </summary>
    public class FusionCompoundConfirmPanelViewModel : ViewModel
    {
        /// <summary>
        /// 主宝石流水 id（PlayerFusionGem.Id）
        /// </summary>
        public BindableProperty<int> MainGemId { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 次宝石流水 id（PlayerFusionGem.Id）
        /// </summary>
        public BindableProperty<int> SecondaryGemId { get; } = new BindableProperty<int>(0);

        #region 命令

        public Command OnCompoundCommand { get; private set; }
        public Command OnCloseCommand { get; private set; }

        #endregion

        #region 事件

        public System.Action<bool> OnCompoundRequested;
        public System.Action OnCloseRequested;

        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            OnCompoundCommand = new Command(OnCompound);
            OnCloseCommand = new Command(OnClose);
        }

        // /// <summary>
        // /// 本次合成所用次宝石等级 SuccessProbability（合成请求前快照）
        // /// </summary>
        // public float SecondaryGemSuccessProbability { get; private set; }

        public void Initialize(int mainGemId, int secondaryGemId)
        {
            MainGemId.Value = mainGemId;
            SecondaryGemId.Value = secondaryGemId;
        }

        private async void OnCompound()
        {
            try
            {
                // var secondaryGem = Global.PlayerFusionGems[SecondaryGemId.Value];
                // SecondaryGemSuccessProbability = secondaryGem.GetLevelCfg().SuccessProbability[0];

                var result = await GameServiceManager.GetOrCreate<FusionGemService>()
                    .FusionGemCompound(MainGemId.Value, SecondaryGemId.Value);
                var success = result != null && result.Success;
                OnCompoundRequested?.Invoke(success);
            }
            catch (Exception e)
            {
                LogTool.LogException(e);
                Toast.Show(e.Message);
                // OnCompoundRequested?.Invoke(false);
            }
        }

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }
    }
}
