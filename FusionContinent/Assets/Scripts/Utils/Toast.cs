using Game.UI;
using UIFramework.Services;

namespace FusionContinent
{
    /// <summary>
    /// Toast 提示工具类 - 全局访问接口
    /// </summary>
    public static class Toast
    {
        private static ToastService mService;

        /// <summary>
        /// 获取 ToastService
        /// </summary>
        private static ToastService Service
        {
            get
            {
                if (mService == null)
                {
                    mService = GameServiceManager.Get<ToastService>();
                }
                return mService;
            }
        }

        /// <summary>
        /// 显示普通提示
        /// </summary>
        public static void Show(string message, float duration = 2f)
        {
            Service?.Show(message, duration);
        }


        /// <summary>
        /// 显示成功提示
        /// </summary>
        public static void Success(string message, float duration = 2f)
        {
            Service?.ShowSuccess(message, duration);
        }

        /// <summary>
        /// 显示警告提示
        /// </summary>
        public static void Warning(string message, float duration = 2f)
        {
            Service?.ShowWarning(message, duration);
        }

        /// <summary>
        /// 显示错误提示
        /// </summary>
        public static void Error(string message, float duration = 2f)
        {
            Service?.ShowError(message, duration);
        }

        /// <summary>
        /// 清空所有提示
        /// </summary>
        public static void Clear()
        {
            Service?.ClearAllToasts();
        }
    }
}
