using UIFramework;
using UnityEngine;
using UIFramework.Services;

namespace Game.Login
{
    /// <summary>
    /// 注销账号面板 ViewModel
    /// </summary>
    public class CancelAccountViewModel : ViewModel
    {
        #region 属性
        // 无特殊属性，主要是操作
        #endregion

        #region 命令
        public Command OnConfirmCancelCommand { get; private set; }
        public Command OnCloseCommand { get; private set; }
        #endregion

        #region 依赖
        private LoginService mLoginService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 获取Service
            mLoginService = GameServiceManager.Get<LoginService>();

            // 初始化命令
            OnConfirmCancelCommand = new Command(OnConfirmCancel);
            OnCloseCommand = new Command(OnClose);

            LogTool.Log("[CancelAccountViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[CancelAccountViewModel] 销毁");
        }

        #region 命令实现

        private void OnConfirmCancel()
        {
            // 清除登录数据
            ClearAccountData();
            
            // 触发注销成功事件
            OnCancelSuccess?.Invoke();
            
            // 关闭面板
            OnClosePanel?.Invoke();
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        #endregion

        #region 公共方法

        public void ClearAccountData()
        {
            // 清除PlayerPrefs中的账号数据
            PlayerPrefs.DeleteKey("Account");
            PlayerPrefs.DeleteKey("Password");
            PlayerPrefs.Save();
            
            LogTool.Log("[CancelAccountViewModel] 已清除账号数据");
            
            // 触发提示
            OnShowMessage?.Invoke("已退出登录");
        }

        #endregion

        #region 事件

        public System.Action OnCancelSuccess;
        public System.Action OnClosePanel;
        public System.Action<string> OnShowMessage;

        #endregion
    }
}
