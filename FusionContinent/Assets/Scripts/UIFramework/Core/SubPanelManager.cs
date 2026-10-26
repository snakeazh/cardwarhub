using UIFramework;
using UnityEngine;
using System.Collections.Generic;

namespace UIFramework
{
    /// <summary>
    /// 子界面管理基类 - 用于管理多层嵌套子界面
    /// 支持子界面的复用（GetOrOpen模式）
    /// </summary>
    public abstract class SubPanelManager<TParentViewModel> : UIPanel<TParentViewModel> 
        where TParentViewModel : ViewModel
    {
        #region 子界面管理
        
        // 当前显示的子界面
        protected UIPanel mCurrentSubPanel;
        
        // 子界面缓存（用于复用）
        private Dictionary<System.Type, UIPanel> mSubPanelCache = new Dictionary<System.Type, UIPanel>();
        
        // 子界面容器
        [SerializeField]
        protected Transform mSubPanelContainer;
        
        #endregion

        #region 子界面操作

        /// <summary>
        /// 打开或复用子界面（复用 ViewModel）
        /// 如果子界面已存在，复用旧的 ViewModel（保留状态）
        /// 如果子界面不存在，创建新的 ViewModel
        /// </summary>
        /// <typeparam name="TSubPanel">子界面类型</typeparam>
        /// <typeparam name="TSubViewModel">子界面ViewModel类型</typeparam>
        /// <param name="onOpened">打开完成回调</param>
        /// <param name="updateData">更新数据回调（用于更新已存在的 ViewModel）</param>
        /// <returns>子界面实例</returns>
        protected TSubPanel OpenOrReuseSubPanel<TSubPanel, TSubViewModel>(
            System.Action<TSubPanel> onOpened = null,
            System.Action<TSubViewModel> updateData = null)
            where TSubPanel : UIPanel<TSubViewModel>
            where TSubViewModel : ViewModel, new()
        {
            var panelType = typeof(TSubPanel);
            
            // 检查缓存中是否已存在
            if (mSubPanelCache.TryGetValue(panelType, out var existingPanel))
            {
                var typedPanel = (TSubPanel)existingPanel;

                if (mSubPanelContainer != null)
                {
                    existingPanel.transform.SetParent(mSubPanelContainer, false);
                }
                
                // 获取已存在的 ViewModel
                if (existingPanel is IView view && view.ViewModel is TSubViewModel existingVM)
                {
                    // 先更新数据，再显示，确保 Show 生命周期拿到最新状态
                    updateData?.Invoke(existingVM);
                    
                    LogTool.Log($"[SubPanelManager] 复用子界面: {panelType.Name}, ViewModel: {existingVM.GetType().Name}");
                }

                mCurrentSubPanel = existingPanel;
                
                // 如果已隐藏，显示它
                if (!existingPanel.gameObject.activeSelf)
                {
                    existingPanel.Show();
                }
                
                onOpened?.Invoke(typedPanel);
                return typedPanel;
            }
            
            // 检查全局缓存（UIManager中）
            var globalPanel = UIManager.GetPanel<TSubPanel>();
            if (globalPanel != null)
            {
                // 设置父级
                if (mSubPanelContainer != null)
                {
                    globalPanel.transform.SetParent(mSubPanelContainer, false);
                }
                
                // 获取已存在的 ViewModel
                if (globalPanel is IView view && view.ViewModel is TSubViewModel existingVM)
                {
                    updateData?.Invoke(existingVM);
                    
                    LogTool.Log($"[SubPanelManager] 复用全局子界面: {panelType.Name}");
                }

                mSubPanelCache[panelType] = globalPanel;
                mCurrentSubPanel = globalPanel;
                
                if (!globalPanel.gameObject.activeSelf)
                {
                    globalPanel.Show();
                }
                
                onOpened?.Invoke(globalPanel);
                return globalPanel;
            }
            
            // 创建新的子界面（创建新的 ViewModel）
            var newVm = new TSubViewModel();
            newVm.OnCreate();
            CreateSubPanelAsync(newVm, onOpened);
            return null;
        }

        /// <summary>
        /// 打开或复用子界面（指定 ViewModel）
        /// 如果子界面已存在，使用已存在的 ViewModel（忽略传入的 viewModel，保留状态）
        /// 如果子界面不存在，使用传入的 viewModel 创建新的
        /// </summary>
        /// <typeparam name="TSubPanel">子界面类型</typeparam>
        /// <typeparam name="TSubViewModel">子界面ViewModel类型</typeparam>
        /// <param name="viewModel">新创建时使用的 ViewModel</param>
        /// <param name="onOpened">打开完成回调</param>
        /// <param name="updateData">更新数据回调（用于更新已存在的 ViewModel）</param>
        /// <returns>子界面实例</returns>
        protected TSubPanel OpenOrReuseSubPanel<TSubPanel, TSubViewModel>(
            TSubViewModel viewModel, 
            System.Action<TSubPanel> onOpened = null,
            System.Action<TSubViewModel> updateData = null)
            where TSubPanel : UIPanel<TSubViewModel>
            where TSubViewModel : ViewModel
        {
            var panelType = typeof(TSubPanel);
            
            // 检查缓存中是否已存在
            if (mSubPanelCache.TryGetValue(panelType, out var existingPanel))
            {
                var typedPanel = (TSubPanel)existingPanel;

                if (mSubPanelContainer != null)
                {
                    existingPanel.transform.SetParent(mSubPanelContainer, false);
                }
                
                // 获取已存在的 ViewModel（复用旧的，忽略传入的）
                if (existingPanel is IView view && view.ViewModel is TSubViewModel existingVM)
                {
                    // 先更新数据，再显示，确保 Show 生命周期拿到最新状态
                    updateData?.Invoke(existingVM);
                    
                    LogTool.Log($"[SubPanelManager] 复用子界面: {panelType.Name}, 保留 ViewModel 状态");
                }

                mCurrentSubPanel = existingPanel;
                
                // 如果已隐藏，显示它
                if (!existingPanel.gameObject.activeSelf)
                {
                    existingPanel.Show();
                }
                
                onOpened?.Invoke(typedPanel);
                return typedPanel;
            }
            
            // 检查全局缓存（UIManager中）
            var globalPanel = UIManager.GetPanel<TSubPanel>();
            if (globalPanel != null)
            {
                // 设置父级
                if (mSubPanelContainer != null)
                {
                    globalPanel.transform.SetParent(mSubPanelContainer, false);
                }
                
                // 获取已存在的 ViewModel（复用旧的）
                if (globalPanel is IView view && view.ViewModel is TSubViewModel existingVM)
                {
                    updateData?.Invoke(existingVM);
                    
                    LogTool.Log($"[SubPanelManager] 复用全局子界面: {panelType.Name}, 保留 ViewModel 状态");
                }
                
                mSubPanelCache[panelType] = globalPanel;
                mCurrentSubPanel = globalPanel;

                if (!globalPanel.gameObject.activeSelf)
                {
                    globalPanel.Show();
                }
                
                onOpened?.Invoke(globalPanel);
                return globalPanel;
            }
            
            // 不存在，使用传入的 ViewModel 创建新的
            CreateSubPanelAsync(viewModel, onOpened);
            return null;
        }

        /// <summary>
        /// 异步创建子界面
        /// </summary>
        private async void CreateSubPanelAsync<TSubPanel, TSubViewModel>(
            TSubViewModel viewModel,
            System.Action<TSubPanel> onOpened)
            where TSubPanel : UIPanel<TSubViewModel>
            where TSubViewModel : ViewModel
        {
            var panel = await UIManager.OpenPanelAsync<TSubPanel, TSubViewModel>(
                viewModel,
                UILevel.Common);
            
            if (panel != null)
            {
                // 设置父级
                if (mSubPanelContainer != null)
                {
                    panel.transform.SetParent(mSubPanelContainer, false);
                }
                
                // 缓存
                mSubPanelCache[typeof(TSubPanel)] = panel;
                mCurrentSubPanel = panel;
                
                // 监听关闭，从缓存移除
                panel.OnClosed(() =>
                {
                    mSubPanelCache.Remove(typeof(TSubPanel));
                    if (mCurrentSubPanel == panel)
                    {
                        mCurrentSubPanel = null;
                    }
                });
                
                onOpened?.Invoke(panel);
            }
        }

        /// <summary>
        /// 创建本地子界面（使用 UIManager.OpenPanel 接口，但设置为 Unmanaged 模式）
        /// 适用于：StackMode.Unmanaged 的子界面，不参与全局堆栈管理
        /// </summary>
        protected void CreateLocalSubPanel<TSubPanel, TSubViewModel>(
            TSubViewModel viewModel,
            System.Action<TSubPanel> onCreated = null)
            where TSubPanel : UIPanel<TSubViewModel>
            where TSubViewModel : ViewModel
        {
            var panelType = typeof(TSubPanel);
            
            // 检查缓存
            if (mSubPanelCache.TryGetValue(panelType, out var existingPanel))
            {
                var typedPanel = (TSubPanel)existingPanel;
                if (!existingPanel.gameObject.activeSelf)
                {
                    existingPanel.gameObject.SetActive(true);
                }
                mCurrentSubPanel = existingPanel;
                onCreated?.Invoke(typedPanel);
                return;
            }
            
            // 使用 UIManager.OpenPanel 创建子界面
            // 注意：子界面必须是 StackMode.Unmanaged 才不会被 UIManager 全局管理
            CreateLocalSubPanelAsync(viewModel, onCreated);
        }

        /// <summary>
        /// 异步创建本地子界面（使用 UIManager 接口）
        /// </summary>
        private async void CreateLocalSubPanelAsync<TSubPanel, TSubViewModel>(
            TSubViewModel viewModel,
            System.Action<TSubPanel> onCreated)
            where TSubPanel : UIPanel<TSubViewModel>
            where TSubViewModel : ViewModel
        {
            var panel = await UIManager.OpenPanelAsync<TSubPanel, TSubViewModel>(
                viewModel,
                UILevel.Const);
            
            if (panel != null)
            {
                // 设置父级到子界面容器
                if (mSubPanelContainer != null)
                {
                    panel.transform.SetParent(mSubPanelContainer, false);
                }
                
                // 缓存
                mSubPanelCache[typeof(TSubPanel)] = panel;
                mCurrentSubPanel = panel;
                
                // 监听关闭，从缓存移除
                panel.OnClosed(() =>
                {
                    mSubPanelCache.Remove(typeof(TSubPanel));
                    if (mCurrentSubPanel == panel)
                    {
                        mCurrentSubPanel = null;
                    }
                });
                
                LogTool.Log($"[SubPanelManager] 创建本地子界面: {typeof(TSubPanel).Name}");
                onCreated?.Invoke(panel);
            }
        }

        /// <summary>
        /// 隐藏当前子界面（保留在内存中）
        /// </summary>
        protected void HideCurrentSubPanel()
        {
            if (mCurrentSubPanel != null)
            {
                mCurrentSubPanel.Close();
                mCurrentSubPanel = null;
            }
        }

        /// <summary>
        /// 关闭并移除子界面
        /// </summary>
        protected void CloseSubPanel<TSubPanel>() where TSubPanel : UIPanel
        {
            var panelType = typeof(TSubPanel);
            
            // 从缓存中关闭
            if (mSubPanelCache.TryGetValue(panelType, out var panel))
            {
                UIManager.ClosePanel(panel);
                mSubPanelCache.Remove(panelType);
                
                if (mCurrentSubPanel == panel)
                {
                    mCurrentSubPanel = null;
                }
            }
            else
            {
                // 尝试从全局关闭
                UIManager.ClosePanel<TSubPanel>();
            }
        }

        /// <summary>
        /// 关闭所有子界面
        /// </summary>
        protected void CloseAllSubPanels()
        {
            // 先复制缓存列表，避免遍历时修改集合
            var panelsToClose = new List<UIPanel>(mSubPanelCache.Values);
            mSubPanelCache.Clear();
            mCurrentSubPanel = null;

            foreach (var panel in panelsToClose)
            {
                if (panel != null)
                {
                    UIManager.ClosePanel(panel);
                }
            }
        }

        /// <summary>
        /// 获取已缓存的子界面
        /// </summary>
        protected TSubPanel GetCachedSubPanel<TSubPanel>() where TSubPanel : UIPanel
        {
            if (mSubPanelCache.TryGetValue(typeof(TSubPanel), out var panel))
            {
                return panel as TSubPanel;
            }
            return null;
        }

        /// <summary>
        /// 检查子界面是否已缓存
        /// </summary>
        protected bool HasCachedSubPanel<TSubPanel>() where TSubPanel : UIPanel
        {
            return mSubPanelCache.ContainsKey(typeof(TSubPanel));
        }

        #endregion

        #region 生命周期

        protected override void OnHide()
        {
            base.OnHide();
            
            // 隐藏时只隐藏子界面，不销毁（用于复用）
            foreach (var kvp in mSubPanelCache)
            {
                if (kvp.Value != null && kvp.Value.gameObject.activeSelf)
                {
                    kvp.Value.Hide();
                }
            }
            mCurrentSubPanel = null;
        }

        protected override bool ShouldUpdateUserToolOnBecameVisible()
        {
            return false;
        }

        protected override void OnClose()
        {
            base.OnClose();
            
            // 关闭时销毁所有子界面
            CloseAllSubPanels();
        }

        #endregion
    }


}
