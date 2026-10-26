using System;
using Assets.Scripts.Localization;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UIFramework;
using UIFramework.Services;

namespace Game.HeroSeat
{
    /// <summary>
    /// 机关升级面板 ViewModel（UIFramework MVVM）。
    /// </summary>
    public class MechanicUpgradePanelViewModel : ViewModel
    {
        public int SkillId { get; private set; }
        public BindableProperty<string> MechanicNameText { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> ShowUpGradeBtn { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> ShowWearBtn { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> ShowUnWearBtn { get; } = new BindableProperty<bool>(false);

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnUpgradeCommand { get; private set; }
        public Command OnWearCommand { get; private set; }
        public Command OnUnWearCommand { get; private set; }
        #endregion

        private SeatService mSeatService;
        private bool mIsProcessing;

        public override void OnCreate()
        {
            base.OnCreate();
            mSeatService = GameServiceManager.Get<SeatService>();
            OnCloseCommand = new Command(OnClose);
            OnUpgradeCommand = new Command(OnUpgrade, CanUpgrade);
            OnWearCommand = new Command(OnWear, CanWear);
            OnUnWearCommand = new Command(OnUnWear, CanUnWear);
        }

        public void Setup(int skillId)
        {
            SkillId = skillId;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            var config = mSeatService.GetSkillConfig(SkillId);
            MechanicNameText.Value = config?.Name ?? string.Empty;
            RefreshButtonStates();
        }

        private void RefreshButtonStates()
        {
            var isActivated = mSeatService.IsSkillActivated(SkillId);
            var isEquipped = mSeatService.IsSkillEquipped(SkillId);
            var isMaxLevel = mSeatService.IsSkillMaxLevel(SkillId);

            ShowUpGradeBtn.Value = isActivated && !isMaxLevel;
            ShowWearBtn.Value = isActivated && !isEquipped && mSeatService.HasEmptyUnlockedSlot();
            ShowUnWearBtn.Value = isActivated && isEquipped;

            OnUpgradeCommand.RaiseCanExecuteChanged();
            OnWearCommand.RaiseCanExecuteChanged();
            OnUnWearCommand.RaiseCanExecuteChanged();
        }

        private bool CanUpgrade()
        {
            return !mIsProcessing && SkillId > 0 && ShowUpGradeBtn.Value;
        }

        private bool CanWear()
        {
            return !mIsProcessing && ShowWearBtn.Value;
        }

        private bool CanUnWear()
        {
            return !mIsProcessing && ShowUnWearBtn.Value;
        }

        private async void OnUpgrade()
        {
            if (SkillId <= 0)
            {
                return;
            }

            int itemId;
            int needCount;

            if (!mSeatService.IsSkillActivated(SkillId))
            {
                if (!mSeatService.TryGetSkillActivateConsume(SkillId, out itemId, out needCount))
                {
                    return;
                }

                if (Global.GetItemNumById(itemId) < needCount)
                {
                    Toast.Show(LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_TIPS_TALENT_ITEM_INSUFFICIENT"));
                    return;
                }

                await RunProcessingAsync(() => mSeatService.ActivateSkillAsync(SkillId));
                return;
            }

            if (mSeatService.IsSkillMaxLevel(SkillId))
            {
                return;
            }

            if (!mSeatService.TryGetSkillUpgradeConsume(SkillId, out itemId, out needCount))
            {
                return;
            }

            if (Global.GetItemNumById(itemId) < needCount)
            {
                Toast.Show(LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_TIPS_TALENT_ITEM_INSUFFICIENT"));
                return;
            }

            await RunProcessingAsync(() => mSeatService.UpgradeSkillAsync(SkillId));
        }

        private async void OnWear()
        {
            if (SkillId <= 0)
            {
                return;
            }

            await RunProcessingAsync(() => mSeatService.WearSkillAsync(SkillId));
        }

        private async void OnUnWear()
        {
            if (SkillId <= 0)
            {
                return;
            }

            await RunProcessingAsync(() => mSeatService.UnWearSkillAsync(SkillId));
        }

        private async UniTask RunProcessingAsync(Func<UniTask<bool>> action)
        {
            mIsProcessing = true;
            RaiseCommandStates();
            try
            {
                var success = await action();
                if (success)
                {
                    RefreshDisplay();
                }
            }
            finally
            {
                mIsProcessing = false;
                RaiseCommandStates();
            }
        }

        private void RaiseCommandStates()
        {
            OnUpgradeCommand.RaiseCanExecuteChanged();
            OnWearCommand.RaiseCanExecuteChanged();
            OnUnWearCommand.RaiseCanExecuteChanged();
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
