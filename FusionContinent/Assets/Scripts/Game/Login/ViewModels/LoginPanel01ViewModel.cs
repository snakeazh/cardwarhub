using UIFramework;
using UIFramework.Services;
using UnityEngine;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Login
{
    /// <summary>
    /// 登录界面 ViewModel
    /// 单机化：无选服、无账号密码输入，账号为空时本地随机生成
    /// </summary>
    public class LoginPanel01ViewModel : ViewModel
    {
        #region 属性（从Service或本地数据同步）
        public BindableProperty<string> Account { get; } = new BindableProperty<string>("");
        public BindableProperty<string> Password { get; } = new BindableProperty<string>("");
        public BindableProperty<string> UserName { get; } = new BindableProperty<string>("");
        public BindableProperty<string> Version { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> IsToggleOn { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> IsEnterGameBtnEnabled { get; } = new BindableProperty<bool>(true);
        public BindableProperty<string> ErrorMessage { get; } = new BindableProperty<string>("");
        #endregion

        #region 命令
        public Command OnEnterGameCommand { get; private set; }
        public Command OnLanguageChangeCommand { get; private set; }
        public Command OnCancelAccountCommand { get; private set; }
        public Command<bool> OnToggleChangeCommand { get; private set; }
        #endregion

        #region 依赖
        private LoginService mLoginService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 获取Service
            mLoginService = GameServiceManager.Get<LoginService>();

            // 订阅全局登录成功事件
            mLoginService.OnLoginSuccess += OnLoginSuccessHandler;

            // 初始化命令
            OnEnterGameCommand = new Command(OnEnterGame, () => IsEnterGameBtnEnabled.Value);
            OnLanguageChangeCommand = new Command(OnLanguageChange);
            OnCancelAccountCommand = new Command(OnCancelAccount);
            OnToggleChangeCommand = new Command<bool>(OnToggleChange);

            // 初始化数据
            InitializeData();
            
            LogTool.Log("[LoginPanel01ViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            // 取消订阅全局登录成功事件
            if (mLoginService != null)
            {
                mLoginService.OnLoginSuccess -= OnLoginSuccessHandler;
            }
            
            LogTool.Log("[LoginPanel01ViewModel] 销毁");
        }

        /// <summary>
        /// 处理全局登录成功事件
        /// </summary>
        private void OnLoginSuccessHandler(LoginPlayer loginPlayer)
        {
            LogTool.Log("[LoginPanel01ViewModel] 收到登录成功事件，触发界面跳转");
            
            // 触发自己的登录成功事件，让Panel处理界面跳转
            OnLoginSuccess?.Invoke(loginPlayer);
        }

        private void InitializeData()
        {
            // 从 PlayerPrefs 读取数据，没有则随机生成
            Account.Value = PlayerPrefs.GetString("Account", GenerateRandomAccount(6));
            Password.Value = PlayerPrefs.GetString("Password", GenerateRandomAccount(6));
            UserName.Value = PlayerPrefs.GetString("UserName", GenerateRandomAccount(6));
            Version.Value = Application.version;
            IsToggleOn.Value = PlayerPrefs.GetInt("ToggleState", 0) == 1;

            // 保存默认值
            PlayerPrefs.SetString("Account", Account.Value);
            PlayerPrefs.SetString("Password", Password.Value);
            PlayerPrefs.SetString("UserName", UserName.Value);
        }

        #region 命令实现

        private async void OnEnterGame()
        {
            if (!IsToggleOn.Value)
            {
                ErrorMessage.Value = "请同意用户协议";
                return;
            }

            // 单机化：账号为空时直接本地生成随机账号密码，不再弹账号输入窗
            if (string.IsNullOrEmpty(Account.Value) || string.IsNullOrEmpty(Password.Value))
            {
                Account.Value = GenerateRandomAccount(6);
                Password.Value = GenerateRandomAccount(6);
                PlayerPrefs.SetString("Account", Account.Value);
                PlayerPrefs.SetString("Password", Password.Value);
            }

            IsEnterGameBtnEnabled.Value = false;
            // ErrorMessage.Value = "正在登录，请稍候...";

            try
            {
                var loginPlayer = await mLoginService.TryLoginAsync(Account.Value, Password.Value);
                
                if (loginPlayer != null)
                {
                    // 保存协议同意状态
                    PlayerPrefs.SetInt("ToggleState", IsToggleOn.Value ? 1 : 0);
                    
                    // 登录成功事件由 LoginService.OnLoginSuccess 统一触发
                    // 不需要在这里手动触发，避免重复
                }
                else
                {
                    // ErrorMessage.Value = "登录失败，请检查账号密码";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage.Value = $"登录失败：{ex.Message}";
                // LogTool.LogError($"[LoginPanel01ViewModel] 登录异常: {ex}");
            }
            finally
            {
                IsEnterGameBtnEnabled.Value = true;
            }
        }

        private void OnLanguageChange()
        {
            OnOpenLanguagePanel?.Invoke();
        }

        private void OnCancelAccount()
        {
            OnOpenCancelPanel?.Invoke();
        }

        private void OnToggleChange(bool isOn)
        {
            IsToggleOn.Value = isOn;
        }

        #endregion

        #region 事件

        public System.Action<LoginPlayer> OnLoginSuccess;
        public System.Action OnOpenLanguagePanel;
        public System.Action OnOpenCancelPanel;

        #endregion

        #region 工具方法

        private string GenerateRandomAccount(int length = 6)
        {
            string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            string charSetWithNumbers = charSet + "1234567890";

            string result = "";
            for (int i = 0; i < length; i++)
            {
                if (i == 0)
                {
                    // 第一位必须是字母
                    int idx = Random.Range(0, charSet.Length);
                    result += charSet[idx];
                }
                else
                {
                    int idx = Random.Range(0, charSetWithNumbers.Length);
                    result += charSetWithNumbers[idx];
                }
            }

            return result;
        }

        #endregion
    }
}
