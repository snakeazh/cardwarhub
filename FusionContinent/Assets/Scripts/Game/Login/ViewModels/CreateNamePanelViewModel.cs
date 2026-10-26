using UIFramework;
using UIFramework.Services;

namespace Game.Login
{
    /// <summary>
    /// 创建角色名面板 ViewModel
    /// </summary>
    public class CreateNamePanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<string> UserName { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> IsBusy { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> IsStartEnabled { get; } = new BindableProperty<bool>(true);
        #endregion

        #region 命令
        public Command OnClearNameCommand { get; private set; }
        public Command<string> OnNameChangedCommand { get; private set; }
        public Command OnConfirmCommand { get; private set; }
        #endregion

        #region 事件
        public System.Action<string, float, bool> OnShowTip;
        public System.Action OnCloseRequested;
        #endregion

        #region 依赖
        private LoginService mLoginService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            mLoginService = GameServiceManager.Get<LoginService>();

            OnClearNameCommand = new Command(OnClearName);
            OnNameChangedCommand = new Command<string>(OnNameChanged);
            OnConfirmCommand = new Command(OnEditName, CanStartGame);

            IsBusy.OnValueChanged += (_, __) =>
            {
                UpdateStartButtonState();
                OnConfirmCommand.RaiseCanExecuteChanged();
            };

            UserName.OnValueChanged += (_, __) =>
            {
                OnConfirmCommand.RaiseCanExecuteChanged();
            };

            mLoginService.InitDefaultFigureData();
            // UserName.Value = mLoginService.GenerateRandomUserName();
            UpdateStartButtonState();
        }

        private bool CanStartGame()
        {
            return !IsBusy.Value;
        }

        private void UpdateStartButtonState()
        {
            IsStartEnabled.Value = !IsBusy.Value;
        }

        private void OnClearName()
        {
            UserName.Value = "";
        }

        private void OnNameChanged(string input)
        {
            var normalized = mLoginService.FilterUserName(input);
            UserName.Value = normalized;

            if (mLoginService.IsUserNameTooLong(input, 10))
            {
                OnShowTip?.Invoke("名称不能超过10个字符", 2f, false);
            }
        }

        private async void OnEditName()
        {
            // if (IsBusy.Value) return;

            // IsBusy.Value = true;
            // OnShowTip?.Invoke("操作中....", 5f, false);

            try
            {
                var result = await mLoginService.SavePlayerAndEnterGameAsync(UserName.Value);
                OnShowTip?.Invoke(result.ErrorMessage, result.TipDuration, result.IsShowTag);
                if (result.Success)
                {
                    if (OnCloseRequested != null)
                    {
                        OnCloseRequested.Invoke();
                    }
                }
            }
            finally
            {
                IsBusy.Value = false;
            }
        }
    }
}
