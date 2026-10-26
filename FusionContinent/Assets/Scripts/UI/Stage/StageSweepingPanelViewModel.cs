using System;
using UIFramework;

namespace FusionContinent
{
    public class StageSweepingPanelViewModel : ViewModel
    {
        public int CurrentOpenId;

        public Command OnCloseCommand { get; private set; }
        public Command OnReceiveAfkRewardsCommand { get; private set; }
        public Command OnHarvestSweepingCommand { get; private set; }

        /// <summary>关闭面板请求（Panel 订阅 → 播放关闭动画）</summary>
        public Action OnCloseRequested;

        /// <summary>领取挂机奖励后触发，通知父界面刷新红点</summary>
        public Action OnRewardsClaimed;

        public override void OnCreate()
        {
            base.OnCreate();
            OnCloseCommand = new Command(() => OnCloseRequested?.Invoke());
            OnReceiveAfkRewardsCommand = new Command(() => OnReceiveAfkRewardsRequested?.Invoke());
            OnHarvestSweepingCommand = new Command(() => OnHarvestSweepingRequested?.Invoke());
        }

        /// <summary>领取挂机奖励请求（Panel 订阅 → 网络请求 + UI 刷新）</summary>
        public Action OnReceiveAfkRewardsRequested;

        /// <summary>快速扫荡请求（Panel 订阅 → 网络请求）</summary>
        public Action OnHarvestSweepingRequested;
    }
}
