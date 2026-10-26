using System;
using Assets.Scripts.Localization;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using UIFramework;
using UIFramework.Services;

namespace Game.HeroSeat
{
    /// <summary>
    /// 英灵座次面板 ViewModel（UIFramework MVVM）。
    /// </summary>
    public class HeroSeatPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<string> CityWallNameText { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> ShowWallUpgradeConsume { get; } = new BindableProperty<bool>(false);
        public BindableProperty<int> WallUpgradeConsumeItemId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> WallUpgradeConsumeCount { get; } = new BindableProperty<int>(0);
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnUpgradeWallCommand { get; private set; }
        #endregion

        private SeatService mSeatService;
        private bool mIsUpgrading;

        public override void OnCreate()
        {
            base.OnCreate();
            mSeatService = GameServiceManager.Get<SeatService>();
            OnCloseCommand = new Command(OnClose);
            OnUpgradeWallCommand = new Command(OnUpgradeWall, CanUpgradeWall);
            RefreshCityWallName();
            RefreshWallUpgradeConsume();
        }

        public void RefreshCityWallName()
        {
            var seatService = GameServiceManager.Get<SeatService>();
            CityWallNameText.Value = $"Lv{seatService.Archive.Wall.Level} 城墙";
        }

        public void RefreshWallUpgradeConsume()
        {
            var seatService = GameServiceManager.Get<SeatService>();
            if (seatService.TryGetWallUpgradeConsume(out var itemId, out var needCount))
            {
                ShowWallUpgradeConsume.Value = true;
                WallUpgradeConsumeItemId.Value = itemId;
                WallUpgradeConsumeCount.Value = needCount;
                return;
            }

            ShowWallUpgradeConsume.Value = false;
            WallUpgradeConsumeItemId.Value = 0;
            WallUpgradeConsumeCount.Value = 0;
        }

        public void RefreshWallDisplay()
        {
            RefreshCityWallName();
            RefreshWallUpgradeConsume();
        }

        private bool CanUpgradeWall()
        {
            return !mIsUpgrading && !mSeatService.IsWallMaxLevel();
        }

        private async void OnUpgradeWall()
        {
            if (mSeatService.IsWallMaxLevel())
            {
                return;
            }

            if (!mSeatService.TryGetWallUpgradeConsume(out var itemId, out var needCount))
            {
                return;
            }

            if (Global.GetItemNumById(itemId) < needCount)
            {
                Toast.Show(LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_TIPS_TALENT_ITEM_INSUFFICIENT"));
                return;
            }

            mIsUpgrading = true;
            OnUpgradeWallCommand.RaiseCanExecuteChanged();
            try
            {
                await mSeatService.UpgradeWallAsync();
            }
            finally
            {
                mIsUpgrading = false;
                OnUpgradeWallCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        #region 事件
        /// <summary>关闭面板，由 View 订阅后执行 CloseSelf</summary>
        public Action OnClosePanel;
        #endregion
    }
}
