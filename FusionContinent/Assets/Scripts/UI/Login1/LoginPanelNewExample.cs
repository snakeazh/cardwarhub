using Cysharp.Threading.Tasks;
using Game.Login;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 示例：如何使用反射 PathProvider 打开登录面板
    /// </summary>
    public class LoginPanelNewExample : MonoBehaviour
    {
        void Start()
        {
            // ===== 步骤 1: 设置使用反射 PathProvider =====
            // 在游戏启动时（如 GameMainController）设置一次即可
            UIManager.PathProvider = new ReflectionPrefabPathProvider("UI/Prefabs");
            
            // ===== 步骤 2: 初始化 UIManager =====
            UIManager.Initialize();

            // ===== 步骤 3: 打开登录面板 =====
            OpenLoginPanel();
        }

        /// <summary>
        /// 打开登录面板
        /// </summary>
        void OpenLoginPanel()
        {
            // 创建 ViewModel
            var viewModel = new LoginPanel01ViewModel();
            viewModel.OnCreate();

            // 直接使用异步接口
            // 路径会自动从 [PrefabPath("UI/Login/LoginPanel01")] 属性读取
            UIManager.OpenPanelAsync<LoginPanelNew, LoginPanel01ViewModel>(
                viewModel,
                UILevel.Common,
                onComplete: panel =>
                {
                    if (panel != null)
                    {
                        LogTool.Log($"[LoginPanel] 面板打开完成，路径: UI/Login/LoginPanel01");
                    }
                    else
                    {
                        LogTool.LogError("[LoginPanel] 面板打开失败！");
                    }
                }).Forget();
        }
    }
}
