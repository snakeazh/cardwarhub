
using System;
using FusionContinent;
using UnityEngine.UI;
using Game.Loading;
using QFramework;
using Cysharp.Threading.Tasks;
using UILevel = UIFramework.UILevel;
using UIManager = UIFramework.UIManager;

namespace Manager
{
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class SceneLoader
    {
        public static string TargetScene { get; private set; }
        private static FusionContinent.LoadingPanelNew loadingPanel;
        private static LoadingPanelViewModel loadingViewModel;
        
        public static void Load(string target)
        {
            if (loadingPanel == null)
            {
                // 创建 ViewModel
                loadingViewModel = new LoadingPanelViewModel();
                loadingViewModel.OnCreate();
                
                OpenLoadingPanelAsync(target).Forget();
            }
            else
            {
                // 检查 ViewModel 是否已绑定
                if (loadingViewModel == null || loadingPanel.VM == null)
                {
                    // 需要重新创建 ViewModel
                    loadingViewModel = new LoadingPanelViewModel();
                    loadingViewModel.OnCreate();
                    
                    // 关闭旧的面板，重新打开
                    UIFramework.UIManager.ClosePanel(loadingPanel);
                    loadingPanel = null;
                    
                    // 重新打开面板
                    Load(target);
                    return;
                }
                
                loadingPanel.Show();
                TargetScene = target;
                loadingPanel.StartLoad(target);
                UIManager.ClosePanelsByLevel(UIFramework.UILevel.Common);
                UIManager.ClosePanelsByLevel(UIFramework.UILevel.Panel);
                UIManager.ClosePanelsByLevel(UIFramework.UILevel.PopUI);
                UIManager.ClosePanelsByLevel(UIFramework.UILevel.Guide);
                UIManager.ClosePanel<GameMainPanelNew>();
                UIManager.ClosePanel<UserToolPanelNew>();
                UIKit.CloseAllPanel(false);
                
            }
        }

        private static async UniTaskVoid OpenLoadingPanelAsync(string target)
        {
            await UIFramework.UIManager.OpenPanelAsync<FusionContinent.LoadingPanelNew, LoadingPanelViewModel>(
                loadingViewModel,
                UILevel.Loading,
                onComplete: panel =>
                {
                    loadingPanel = panel;
                    if (loadingPanel != null)
                    {
                        TargetScene = target;
                        loadingPanel.StartLoad(target);
                        UIManager.ClosePanelsByLevel(UIFramework.UILevel.Common);
                        UIManager.ClosePanelsByLevel(UIFramework.UILevel.Panel);
                        UIManager.ClosePanelsByLevel(UIFramework.UILevel.PopUI);
                        UIManager.ClosePanelsByLevel(UIFramework.UILevel.Guide);
                        UIManager.ClosePanel<GameMainPanelNew>();
                        UIManager.ClosePanel<UserToolPanelNew>();
                        UIKit.CloseAllPanel(false);
                    }
                });
        }
    }
}
