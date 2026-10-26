using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using FusionContinent;
using Game.UserTool;
using UnityEngine.UI;

namespace UIFramework
{
    /// <summary>
    /// 视图接口
    /// </summary>
    public interface IView
    {
        ViewModel ViewModel { get; }
        void Bind(ViewModel viewModel);
    }

    /// <summary>
    /// Panel信息
    /// </summary>
    public class PanelInfo
    {
        public Type PanelType { get; set; }
        public Type ViewModelType { get; set; }
        public UILevel Level { get; set; }
        public string Name { get; set; }
        public DateTime OpenTime { get; set; }
    }

    /// <summary>
    /// 预加载的Panel实例信息
    /// </summary>
    public class PreloadedPanel
    {
        public GameObject Instance { get; set; }
        public UILevel Level { get; set; }
    }

    /// <summary>
    /// 节点绑定上下文
    /// </summary>
    public class NodeBindingContext
    {
        private readonly List<UnityEngine.Object> mBoundNodes = new List<UnityEngine.Object>();
        
        public void AddNode(UnityEngine.Object node)
        {
            if (node != null)
            {
                mBoundNodes.Add(node);
            }
        }
        
        public bool AreAllNodesValid()
        {
            foreach (var node in mBoundNodes)
            {
                if (node == null)
                {
                    return false;
                }
            }
            return true;
        }
        
        public void Clear()
        {
            mBoundNodes.Clear();
        }
    }

    /// <summary>
    /// Panel打开构建器
    /// </summary>
    public class PanelOpenBuilder<TView, TViewModel>
        where TView : UIPanel<TViewModel>
        where TViewModel : ViewModel
    {
        private readonly TViewModel mViewModel;
        private readonly TView mExistingPanel;
        private UILevel mLevel = UILevel.Common;
        private Action<TViewModel> mOnSetData;
        private Action<TView> mOnComplete;
        private readonly NodeBindingContext mNodeContext = new NodeBindingContext();
        
        public PanelOpenBuilder(TViewModel viewModel)
        {
            mViewModel = viewModel;
            mExistingPanel = null;
        }
        
        /// <summary>
        /// 传入已存在的Panel（用于复用模式）
        /// </summary>
        public PanelOpenBuilder(TViewModel viewModel, TView existingPanel)
        {
            mViewModel = viewModel;
            mExistingPanel = existingPanel;
        }
        
        public PanelOpenBuilder<TView, TViewModel> BindNode(UnityEngine.Object node)
        {
            if (node != null)
            {
                mNodeContext.AddNode(node);
            }
            return this;
        }
        
        public PanelOpenBuilder<TView, TViewModel> SetLevel(UILevel level)
        {
            mLevel = level;
            return this;
        }
        
        public PanelOpenBuilder<TView, TViewModel> SetData(Action<TViewModel> onSetData)
        {
            mOnSetData = onSetData;
            return this;
        }
        
        public PanelOpenBuilder<TView, TViewModel> OnComplete(Action<TView> onComplete)
        {
            mOnComplete = onComplete;
            return this;
        }
        
        public UniTask<TView> OpenAsync()
        {
            mOnSetData?.Invoke(mViewModel);
            
            // 如果已有存在的Panel，走统一的堆栈恢复/重排逻辑
            if (mExistingPanel != null)
            {
                UIManager.HandleExistingPanelOpen(mExistingPanel, mViewModel, mLevel, mOnComplete);
                return UniTask.FromResult(mExistingPanel);
            }
            
            if (!mNodeContext.AreAllNodesValid())
            {
                LogTool.LogWarning($"[UIFramework] 绑定的关键节点已被销毁，取消打开{typeof(TView).Name}");
                mNodeContext.Clear();
                return UniTask.FromResult<TView>(null);
            }
            
            return UIManager.OpenPanelInternalAsync<TView, TViewModel>(
                mViewModel,
                mNodeContext,
                mLevel,
                mOnComplete
            );
        }
        
        public static implicit operator UniTask<TView>(PanelOpenBuilder<TView, TViewModel> builder)
        {
            return builder.OpenAsync();
        }
    }

    /// <summary>
    /// UIFramework界面管理器（完全独立，不依赖QFramework）
    /// </summary>
    public static class UIManager
    {

        private static readonly Dictionary<Type, UIPanel> mPanels = new Dictionary<Type, UIPanel>();
        private static readonly Dictionary<Type, PanelInfo> mPanelInfos = new Dictionary<Type, PanelInfo>();
        private static readonly Dictionary<UILevel, List<Type>> mPanelsByLevel = new Dictionary<UILevel, List<Type>>();
        private static readonly Stack<Type> mPanelStack = new Stack<Type>();

        /// <summary>
        /// 预加载的隐藏实例缓存（仅全屏界面）
        /// </summary>
        private static readonly Dictionary<Type, PreloadedPanel> mPreloadedPanels = new Dictionary<Type, PreloadedPanel>();

        /// <summary>
        /// 正在预加载中的界面及其完成回调
        /// </summary>
        private static readonly Dictionary<Type, Action<bool>> mPreloadingPanels = new Dictionary<Type, Action<bool>>();

        /// <summary>
        /// 预加载代次：ReleaseAllPreloads 时递增,进行中的预加载协程完成后比对代次,不一致则丢弃实例
        /// </summary>
        private static int mPreloadGeneration;
        
        /// <summary>
        /// 资源加载器，可以设置为 ResourcesAssetLoader 或 YooAssetsLoader
        /// 默认使用 ResourcesAssetLoader
        /// </summary>
        public static IAssetLoader AssetLoader { get; set; } = new YooAssetsLoader();
        
        /// <summary>
        /// 预制体路径提供器
        /// </summary>
        public static IPrefabPathProvider PathProvider { get; set; } = new ReflectionPrefabPathProvider("");

        /// <summary>
        /// 是否在打开界面时输出内存占用日志（用于排查界面内存问题）
        /// </summary>
        public static bool EnableMemoryLog { get; set; } = true;
        
        /// <summary>
        /// 初始化（在游戏启动时调用一次）
        /// 必须在场景中配置好UIRoot和UIRootConfig
        /// </summary>
        public static void Initialize()
        {
            // 确保UIRoot单例已初始化（会查找场景中的UIRoot物体）
            var uiRoot = UIRoot.Instance;
            if (uiRoot == null)
            {
                LogTool.LogError("[UIManager] 场景中找不到UIRoot！请在场景中创建UIRoot物体并挂载UIRootConfig组件进行配置。");
                return;
            }
            
            if (uiRoot.Config == null)
            {
                LogTool.LogError("[UIManager] UIRoot上缺少UIRootConfig组件！请在UIRoot上挂载UIRootConfig脚本并在Inspector中配置层级。");
                return;
            }
            
            LogTool.Log("[UIManager] 使用UIRootConfig配置");
            
            // 初始化资源加载器
            AssetLoader?.Initialize();
        }
        
        /// <summary>
        /// 获取层级Transform
        /// </summary>
        private static Transform GetLevelRoot(UILevel level)
        {
            if (UIRoot.Instance?.Config == null)
            {
                LogTool.LogError("[UIManager] UIRootConfig未初始化，请先调用Initialize()");
                return null;
            }
            return UIRoot.Instance.Config.GetLevelTransform(level);
        }
        
        #region 公共API
        
        public static bool ContainsPanel<T>() where T : UIPanel
        {
            return mPanels.ContainsKey(typeof(T));
        }

        public static bool ContainsPanel(Type panelType)
        {
            return panelType != null && mPanels.ContainsKey(panelType);
        }
        
        /// <summary>
        /// 获取Panel信息
        /// </summary>
        public static PanelInfo GetPanelInfo<T>() where T : UIPanel
        {
            return mPanelInfos.TryGetValue(typeof(T), out var info) ? info : null;
        }
        
        public static IEnumerable<Type> GetPanelsByLevel(UILevel level)
        {
            if (mPanelsByLevel.TryGetValue(level, out var types))
            {
                return types;
            }
            return new List<Type>();
        }
        
        public static TViewModel GetViewModel<TViewModel>() where TViewModel : ViewModel
        {
            foreach (var kvp in mPanels)
            {
                if (kvp.Value is IView view && view.ViewModel is TViewModel vm)
                {
                    return vm;
                }
            }
            return null;
        }
        
        [System.Obsolete("Use OpenPanelAsync<TView, TViewModel>(...) instead. Do not use the builder API in business code.", false)]
        public static PanelOpenBuilder<TView, TViewModel> OpenPanel<TView, TViewModel>(TViewModel viewModel)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel
        {
            return OpenPanelBuilder<TView, TViewModel>(viewModel);
        }

        [System.Obsolete("Use OpenPanelAsync<TView, TViewModel>(...) instead. Builder is kept only for compatibility.", false)]
        public static PanelOpenBuilder<TView, TViewModel> OpenPanelBuilder<TView, TViewModel>(TViewModel viewModel)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel
        {
            return new PanelOpenBuilder<TView, TViewModel>(viewModel);
        }

        public static UniTask<TView> OpenPanelAsync<TView, TViewModel>(
            TViewModel viewModel,
            UILevel level = UILevel.Common,
            Action<TViewModel> onSetData = null,
            Action<TView> onComplete = null,
            params UnityEngine.Object[] bindNodes)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel
        {
            var builder = OpenPanelBuilder<TView, TViewModel>(viewModel)
                .SetLevel(level);

            if (onSetData != null)
            {
                builder.SetData(onSetData);
            }

            if (onComplete != null)
            {
                builder.OnComplete(onComplete);
            }

            if (bindNodes != null)
            {
                foreach (var bindNode in bindNodes)
                {
                    if (bindNode != null)
                    {
                        builder.BindNode(bindNode);
                    }
                }
            }

            return builder.OpenAsync();
        }
        
        public static PanelOpenBuilder<TView, TViewModel> OpenPanel<TView, TViewModel>()
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel, new()
        {
            var vm = new TViewModel();
            vm.OnCreate();
            return new PanelOpenBuilder<TView, TViewModel>(vm);
        }
        
        /// <summary>
        /// 显示已存在的Panel（如果已隐藏则显示，如果不存在则返回null）
        /// </summary>
        public static bool TryShowExistingPanel<T>() where T : UIPanel
        {
            if (mPanels.TryGetValue(typeof(T), out var panel))
            {
                if (!panel.gameObject.activeSelf)
                {
                    panel.Show();
                    return true;
                }
                return true; // 已经在显示中
            }
            return false;
        }
        
        /// <summary>
        /// 获取或打开Panel（如果已存在则显示，如果不存在则异步创建新的）
        /// </summary>
        public static UniTask<TView> GetOrOpenPanelAsync<TView, TViewModel>(
            TViewModel viewModel,
            UILevel level = UILevel.Common,
            Action<TViewModel> onSetData = null,
            Action<TView> onComplete = null,
            params UnityEngine.Object[] bindNodes)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel
        {
            Type viewType = typeof(TView);

            if (mPanels.TryGetValue(viewType, out var existingPanel))
            {
                if (!existingPanel.gameObject.activeSelf)
                {
                    existingPanel.Show();
                }

                onSetData?.Invoke(viewModel);
                onComplete?.Invoke((TView)existingPanel);
                return UniTask.FromResult((TView)existingPanel);
            }

            return OpenPanelAsync<TView, TViewModel>(viewModel, level, onSetData, onComplete, bindNodes);
        }
        
        /// <summary>
        /// 获取或打开Panel（自动创建ViewModel）
        /// </summary>
        public static UniTask<TView> GetOrOpenPanelAsync<TView, TViewModel>(
            UILevel level = UILevel.Common,
            Action<TViewModel> onSetData = null,
            Action<TView> onComplete = null,
            params UnityEngine.Object[] bindNodes)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel, new()
        {
            Type viewType = typeof(TView);

            if (mPanels.TryGetValue(viewType, out var existingPanel))
            {
                if (!existingPanel.gameObject.activeSelf)
                {
                    existingPanel.Show();
                }

                var vm = existingPanel is IView view ? view.ViewModel as TViewModel : null;
                if (vm == null)
                {
                    vm = new TViewModel();
                    vm.OnCreate();
                }

                onSetData?.Invoke(vm);
                onComplete?.Invoke((TView)existingPanel);
                return UniTask.FromResult((TView)existingPanel);
            }

            var newVm = new TViewModel();
            newVm.OnCreate();
            return OpenPanelAsync<TView, TViewModel>(newVm, level, onSetData, onComplete, bindNodes);
        }
        
        public static void ClosePanel<T>() where T : UIPanel
        {
            if (mPanels.TryGetValue(typeof(T), out var panel))
            {
                panel.Close();
            }
        }
        
        public static void ClosePanel(UIPanel panel)
        {
            if (panel != null)
            {
                panel.Close();
            }
        }

        public static void ClosePanel(Type panelType)
        {
            var panel = GetPanel(panelType);
            if (panel != null)
            {
                panel.Close();
            }
        }
        
        /// <summary>
        /// 获取已打开的Panel
        /// </summary>
        public static T GetPanel<T>() where T : UIPanel
        {
            if (mPanels.TryGetValue(typeof(T), out var panel))
            {
                return panel as T;
            }
            return null;
        }

        public static UIPanel GetPanel(Type panelType)
        {
            if (panelType != null && mPanels.TryGetValue(panelType, out var panel))
            {
                return panel;
            }
            return null;
        }
        
        /// <summary>
        /// 尝试获取已打开的Panel
        /// </summary>
        public static bool TryGetPanel<T>(out T panel) where T : UIPanel
        {
            if (mPanels.TryGetValue(typeof(T), out var existingPanel))
            {
                panel = existingPanel as T;
                return panel != null;
            }
            panel = null;
            return false;
        }
        
        /// <summary>
        /// 隐藏指定类型的Panel（不销毁，只是隐藏）
        /// </summary>
        public static void HidePanel<T>() where T : UIPanel
        {
            if (mPanels.TryGetValue(typeof(T), out var panel))
            {
                panel.Hide();
            }
        }
        
        /// <summary>
        /// 隐藏指定Panel（不销毁，只是隐藏）
        /// </summary>
        public static void HidePanel(UIPanel panel)
        {
            if (panel != null)
            {
                panel.Hide();
            }
        }

        public static void HidePanel(Type panelType)
        {
            var panel = GetPanel(panelType);
            if (panel != null)
            {
                panel.Hide();
            }
        }
        
        /// <summary>
        /// 显示指定类型的Panel（如果已隐藏）
        /// </summary>
        public static void ShowPanel<T>() where T : UIPanel
        {
            if (mPanels.TryGetValue(typeof(T), out var panel))
            {
                panel.Show();
            }
        }
        
        /// <summary>
        /// 显示指定Panel（如果已隐藏）
        /// </summary>
        public static void ShowPanel(UIPanel panel)
        {
            if (panel != null)
            {
                panel.Show();
            }
        }

        public static void ShowPanel(Type panelType)
        {
            var panel = GetPanel(panelType);
            if (panel != null)
            {
                panel.Show();
            }
        }
        
        /// <summary>
        /// 切换指定类型Panel的显示/隐藏状态
        /// </summary>
        public static void TogglePanel<T>() where T : UIPanel
        {
            if (mPanels.TryGetValue(typeof(T), out var panel))
            {
                if (panel.gameObject.activeSelf)
                {
                    panel.Hide();
                }
                else
                {
                    panel.Show();
                }
            }
        }
        
        public static void ClosePanelsByLevel(UILevel level)
        {
            if (mPanelsByLevel.TryGetValue(level, out var types))
            {
                var typesCopy = new List<Type>(types);
                foreach (var type in typesCopy)
                {
                    if (mPanels.TryGetValue(type, out var panel))
                    {
                        if (panel is UserToolPanelNew)
                        {
                            continue;
                        }
                        panel.Close();
                    }
                }
            }
        }
        
        /// <summary>
        /// 关闭所有界面
        /// </summary>
        public static void CloseAllPanels()
        {
            var panels = new List<UIPanel>(mPanels.Values);
            foreach (var panel in panels)
            {
                panel.Close();
            }
        }

        /// <summary>
        /// 刷新当前显示的全屏界面（调用其 Refresh()）
        /// 典型场景：PopUI 层最后一个弹窗关闭后自动回调
        /// </summary>
        public static void RefreshCurrentFullScreenPanel()
        {
            var panelType = PanelStackManager.GetCurrentFullScreenPanelType();
            if (panelType == null)
            {
                return;
            }

            var panel = GetPanel(panelType);
            if (panel != null && panel.gameObject.activeSelf)
            {
                panel.Refresh();
            }
        }

        public static void ApplyUserToolConfig(UserToolData config, string source = null)
        {
            var reason = string.IsNullOrEmpty(source) ? "Unknown" : source;

            if (config == null)
            {
                HideUserToolPanel($"NullConfig:{reason}");
                LogTool.Log($"[UIManager] UserTool 配置为空，隐藏节点: {reason}");
                return;
            }

            EnsureUserToolPanelOpen(userToolPanel =>
            {
                LogTool.Log($"[UIManager] ApplyUserToolConfig 回调: panel={userToolPanel != null}, source={reason}");
                if (userToolPanel != null && userToolPanel.VM != null)
                {
                    ShowUserToolPanel($"ApplyConfig:{reason}");
                    userToolPanel.VM.CurrentConfig.Value = config;
                    BringPanelNodeToTop(userToolPanel, $"ApplyUserToolConfig:{reason}");
                    LogTool.Log($"[UIManager] 应用 UserTool 配置成功: {reason}");
                }
            });
        }

        internal static void UpdateUserToolForPanel(UIPanel panel)
        {
            if (panel == null)
            {
                return;
            }

            if (panel is UserToolPanelNew)
            {
                return;
            }

            // 只有在指定层级的界面成为可见时，才驱动 UserTool 的显示/隐藏
            // - Common / Panel：显示并根据当前界面层级摆放（避免被更高层级界面遮挡）
            // - PopUI/Guide/Tips/Loading 等：隐藏（避免遮挡弹窗/引导/加载）
            if (panel.Level != UILevel.Common && panel.Level != UILevel.Panel)
            {
                // HideUserToolPanel($"LevelNotSupported:{panel.GetType().Name}:{panel.Level}");
                return;
            }

            if (panel is IView view && view.ViewModel is Game.UserTool.IUserToolConfigurable configurable)
            {
                var desiredLevel = panel.Level;
                EnsureUserToolPanelOpen(userToolPanel =>
                {
                    if (userToolPanel == null)
                    {
                        return;
                    }

                    EnsurePanelAtLevel(userToolPanel, desiredLevel, $"UpdateUserToolForPanel:{panel.GetType().Name}");
                    ApplyUserToolConfig(configurable.GetUserToolConfig(), panel.GetType().Name);
                }, desiredLevel);
            }
            else
            {
                HideUserToolPanel($"NoConfig:{panel.GetType().Name}");
            }
        }
        
        internal static void PushToStack<T>() where T : UIPanel
        {
            mPanelStack.Push(typeof(T));
        }
        
        internal static void PopFromStack()
        {
            if (mPanelStack.Count > 0)
            {
                var type = mPanelStack.Pop();
                if (mPanels.TryGetValue(type, out var panel))
                {
                    panel.Close();
                }
            }
        }
        
        public static void Back()
        {
            PanelStackManager.Back();
        }

        internal static void HandleExistingPanelOpen<TView, TViewModel>(
            TView existingPanel,
            TViewModel viewModel,
            UILevel level,
            Action<TView> onComplete)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel
        {
            if (existingPanel == null)
            {
                onComplete?.Invoke(null);
                return;
            }

            var viewType = typeof(TView);
            LogTool.Log($"[UIManager] 界面已存在，尝试置顶/重排: {viewType.Name}");
            var activeViewModel = viewModel;
            if (existingPanel is IView existingView && existingView.ViewModel is TViewModel existingViewModel)
            {
                activeViewModel = existingViewModel;
                if (!ReferenceEquals(existingViewModel, viewModel))
                {
                    viewModel?.OnDestroy();
                }
            }

            UILevel existingLevel = level;
            if (mPanelInfos.TryGetValue(viewType, out var existingInfo))
            {
                existingLevel = existingInfo.Level;
            }

            bool restored = PanelStackManager.RestoreTo(viewType, existingLevel);
            if (!restored)
            {
                existingPanel.Show();
            }

            if (existingPanel.IsManaged && !(activeViewModel is UserToolPanelViewModel))
            {
                PanelStackManager.Push<TView, TViewModel>(activeViewModel, existingLevel);
            }

            onComplete?.Invoke(existingPanel);
        }

        /// <summary>
        /// 预加载全屏界面（实例级：异步加载Prefab并以隐藏状态实例化，打开时直接复用）
        /// 仅支持 Common/Panel 层级的全屏界面，弹窗（PopUI/Guide/Tips/Loading）不做预加载
        /// </summary>
        public static void PreloadPanel<TView>(UILevel level = UILevel.Common, Action<bool> onComplete = null)
            where TView : UIPanel
        {
            PreloadPanel(typeof(TView), level, onComplete);
        }

        public static void PreloadPanel(Type viewType, UILevel level = UILevel.Common, Action<bool> onComplete = null)
        {
            if (viewType == null || !typeof(UIPanel).IsAssignableFrom(viewType))
            {
                LogTool.LogError($"[UIManager] 预加载失败: {viewType?.Name} 不是UIPanel类型");
                onComplete?.Invoke(false);
                return;
            }

            // 弹窗不做预加载
            if (level != UILevel.Common && level != UILevel.Panel)
            {
                LogTool.LogWarning($"[UIManager] 仅全屏界面（Common/Panel）支持预加载，已忽略: {viewType.Name}, Level={level}");
                onComplete?.Invoke(false);
                return;
            }

            // 已打开或已预加载
            if (mPanels.ContainsKey(viewType) || mPreloadedPanels.ContainsKey(viewType))
            {
                onComplete?.Invoke(true);
                return;
            }

            // 正在预加载中，追加完成回调
            if (mPreloadingPanels.TryGetValue(viewType, out var callbacks))
            {
                mPreloadingPanels[viewType] = callbacks + onComplete;
                return;
            }
            mPreloadingPanels[viewType] = onComplete;

            // 确保UIRoot已初始化
            if (UIRoot.Instance == null)
            {
                Initialize();
            }

            UIRoot.Instance.StartCoroutine(PreloadPanelCoroutine(viewType, level, success =>
            {
                if (mPreloadingPanels.TryGetValue(viewType, out var cb))
                {
                    mPreloadingPanels.Remove(viewType);
                    cb?.Invoke(success);
                }
            }));
        }

        /// <summary>
        /// 批量预加载全屏界面（串行加载，可通过onProgress展示进度）
        /// </summary>
        public static async UniTask PreloadPanelsAsync(IReadOnlyList<Type> panelTypes, UILevel level = UILevel.Common, Action<int, int> onProgress = null)
        {
            if (panelTypes == null)
            {
                return;
            }

            int total = panelTypes.Count;
            for (int i = 0; i < total; i++)
            {
                var tcs = new UniTaskCompletionSource<bool>();
                PreloadPanel(panelTypes[i], level, success => tcs.TrySetResult(success));
                await tcs.Task;
                onProgress?.Invoke(i + 1, total);
            }
        }

        /// <summary>
        /// 是否已完成预加载
        /// </summary>
        public static bool IsPreloaded<TView>() where TView : UIPanel
        {
            return mPreloadedPanels.ContainsKey(typeof(TView));
        }

        /// <summary>
        /// 释放指定界面的预加载实例
        /// </summary>
        public static void ReleasePreload<TView>() where TView : UIPanel
        {
            ReleasePreload(typeof(TView));
        }

        public static void ReleasePreload(Type viewType)
        {
            if (mPreloadedPanels.TryGetValue(viewType, out var preloaded))
            {
                mPreloadedPanels.Remove(viewType);
                if (preloaded.Instance != null)
                {
                    UnityEngine.Object.Destroy(preloaded.Instance);
                }
                // 预加载协程给图集 +1 过引用,释放实例时一并归还
                if (AtlasManager.EnableAutoPreload)
                {
                    AtlasManager.Instance.ReleaseForPanel(viewType);
                }
            }
        }

        /// <summary>
        /// 释放所有预加载实例;进行中的预加载完成后会被代次检查丢弃
        /// </summary>
        public static void ReleaseAllPreloads()
        {
            mPreloadGeneration++;
            foreach (var kvp in mPreloadedPanels)
            {
                if (kvp.Value.Instance != null)
                {
                    UnityEngine.Object.Destroy(kvp.Value.Instance);
                }
                if (AtlasManager.EnableAutoPreload)
                {
                    AtlasManager.Instance.ReleaseForPanel(kvp.Key);
                }
            }
            mPreloadedPanels.Clear();
        }

        #endregion
        
        #region 内部方法
        
        internal static void OpenPanelInternal<TView, TViewModel>(
            TViewModel viewModel,
            NodeBindingContext nodeContext,
            Action<TView> onComplete,
            UILevel level)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel
        {
            Type viewType = typeof(TView);
            
            // 检查是否已存在
            if (mPanels.TryGetValue(viewType, out var existingPanel))
            {
                HandleExistingPanelOpen((TView)existingPanel, viewModel, level, onComplete);
                return;
            }
            
            // 确保UIRoot已初始化
            if (UIRoot.Instance == null)
            {
                Initialize();
            }

            LogTool.Log($"{nameof(viewModel)}");
           
            // 加载Prefab
            UIRoot.Instance.StartCoroutine(LoadAndCreatePanel(viewType, viewModel, nodeContext, onComplete, level));
        }
        
        /// <summary>
        /// 处理 UserTool 逻辑
        /// </summary>
        private static void HandleUserTool<TViewModel>(TViewModel viewModel, Type viewType, UILevel level) where TViewModel : ViewModel
        {
            // UserTool 更新已收口到 UIPanel.Show() 生命周期中处理
        }
        
        internal static UniTask<TView> OpenPanelInternalAsync<TView, TViewModel>(
            TViewModel viewModel,
            NodeBindingContext nodeContext,
            UILevel level,
            Action<TView> onComplete = null)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel
        {
            var tcs = new UniTaskCompletionSource<TView>();
            
            OpenPanelInternal<TView, TViewModel>(viewModel, nodeContext, (panel) =>
            {
                onComplete?.Invoke(panel);
                tcs.TrySetResult(panel);
            }, level);
            
            nodeContext?.Clear();
            return tcs.Task;
        }
        
        private static IEnumerator LoadAndCreatePanel<TView, TViewModel>(
            Type viewType,
            TViewModel viewModel,
            NodeBindingContext nodeContext,
            Action<TView> onComplete,
            UILevel level)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel
        {
            // 内存埋点：记录打开界面前的内存快照
            long memBefore = EnableMemoryLog ? GetAllocatedMemory() : 0;
            long monoBefore = EnableMemoryLog ? GetMonoMemory() : 0;

            // 获取预制体路径
            string prefabPath = PathProvider?.GetPrefabPath(viewType) ?? $"UI/Prefabs/{viewType.Name}";

            GameObject instance = null;

            // 优先复用预加载的隐藏实例（仅全屏界面会被预加载）
            var preloaded = ConsumePreloadedPanel(viewType);
            if (preloaded != null)
            {
                instance = preloaded.Instance;

                // 打开层级与预加载层级不一致时，重新挂到目标层级
                if (preloaded.Level != level)
                {
                    var targetRoot = GetLevelRoot(level);
                    if (targetRoot != null)
                    {
                        instance.transform.SetParent(targetRoot, false);
                    }
                }

                // 预加载实例是以非激活状态创建的，Awake/OnEnable 尚未执行。
                // 在 Init 绑定 ViewModel 之前先激活，保持与正常实例化流程一致的生命周期顺序
                // （Awake/OnEnable -> OnInit/OnBindViewModel -> OnOpen），
                // 否则 Awake 中缓存的组件引用在 OnBindViewModel 时还未就绪，绑定会被跳过。
                instance.SetActive(true);
                LogTool.Log($"[UIManager] 复用预加载实例: {viewType.Name}");
            }
            else
            {
                // 统一图集预加载：打开界面前按 UIAtlasPreloadTable 异步预加载依赖图集。
                if (AtlasManager.EnableAutoPreload)
                {
                    yield return AtlasManager.Instance.PreloadForPanelAsync(viewType).ToCoroutine();
                }

                LogTool.Log($"需要加载的界面路径:{prefabPath}");
                GameObject prefab = null;
                bool loaded = false;

                // 仅允许通过 AssetLoader 加载
                if (AssetLoader == null)
                {
                    LogTool.LogError($"[UIFramework] AssetLoader 未配置，无法加载Prefab: {prefabPath}");
                    onComplete?.Invoke(null);
                    yield break;
                }

                AssetLoader.LoadAssetAsync<GameObject>(prefabPath, (asset) =>
                {
                    prefab = asset;
                    loaded = true;
                });

                // 等待加载完成
                while (!loaded)
                {
                    yield return null;
                }

                // 检查节点是否仍然有效（nodeContext可能为null，此时跳过检查）
                if (nodeContext != null && !nodeContext.AreAllNodesValid())
                {
                    LogTool.LogWarning($"[UIFramework] 加载过程中绑定的关键节点被销毁，取消打开{viewType.Name}");
                    if (prefab != null)
                    {
                        AssetLoader?.ReleaseAsset(prefabPath);
                    }
                    onComplete?.Invoke(null);
                    yield break;
                }

                if (prefab == null)
                {
                    LogTool.LogError($"[UIFramework] 找不到Prefab: {prefabPath}");
                    onComplete?.Invoke(null);
                    yield break;
                }

                // 实例化
                instance = UnityEngine.Object.Instantiate(prefab);

                // 释放资源句柄（如果使用的是YooAssets等需要手动释放的系统）
                AssetLoader?.ReleaseAsset(prefabPath);
                instance.name = prefabPath;
                // 再次检查节点
                if (!nodeContext.AreAllNodesValid())
                {
                    LogTool.LogWarning($"[UIFramework] 实例化后绑定的关键节点被销毁，销毁{viewType.Name}");
                    UnityEngine.Object.Destroy(instance);
                    onComplete?.Invoke(null);
                    yield break;
                }

                // 设置层级
                var levelRoot = GetLevelRoot(level);
                if (levelRoot != null)
                {
                    instance.transform.SetParent(levelRoot, false);
                }
            }
            
            // 获取Panel组件
            TView panel = instance.GetComponent<TView>();
            if (panel == null)
            {
                LogTool.LogError($"[UIFramework] Prefab上找不到{viewType.Name}组件");
                UnityEngine.Object.Destroy(instance);
                onComplete?.Invoke(null);
                yield break;
            }
            
            panel.Level = level;
            
            // 判断Panel是否参与堆栈管理
            bool isManaged = panel.IsManaged;
            
            if (isManaged)
            {
                // 缓存信息（只有参与堆栈管理的Panel才注册到UIManager）
                mPanels[viewType] = panel;
                
                var info = new PanelInfo
                {
                    PanelType = viewType,
                    ViewModelType = typeof(TViewModel),
                    Level = level,
                    Name = instance.name,
                    OpenTime = DateTime.Now
                };
                mPanelInfos[viewType] = info;
                
                if (!mPanelsByLevel.ContainsKey(level))
                {
                    mPanelsByLevel[level] = new List<Type>();
                }
                mPanelsByLevel[level].Add(viewType);
            }
            else
            {
                LogTool.Log($"[UIManager] {viewType.Name} 不参与堆栈管理，作为本地Panel创建");
            }
            
            // 初始化
            var data = new ViewModelData { ViewModel = viewModel };
            panel.Init(data);
            panel.Open(data);

            // 内存埋点：输出本次打开界面带来的内存增量
            if (EnableMemoryLog)
            {
                LogPanelMemory(viewType.Name, memBefore, monoBefore);
            }
            
            // 根据层级自动设置 IsFullScreen（只有参与堆栈管理的全屏界面）
            if (isManaged && (level == UILevel.Common || level == UILevel.PopUI) && !(viewModel is UserToolPanelViewModel))
            {
                viewModel.IsFullScreen = true;
            }
            
            // 压入界面栈（只有参与堆栈管理的Panel）
            // UserToolPanel 不进栈
            if (isManaged && !(viewModel is UserToolPanelViewModel))
            {
                PanelStackManager.Push<TView, TViewModel>(viewModel, level);
            }
                        
            // 注册关闭监听
            panel.OnClosed(() => OnPanelClosed(viewType, isManaged));
            
            onComplete?.Invoke(panel);
        }
        
        /// <summary>
        /// 取出预加载的隐藏实例（取出后从缓存移除；实例已被外部销毁时返回null）
        /// </summary>
        private static PreloadedPanel ConsumePreloadedPanel(Type viewType)
        {
            if (mPreloadedPanels.TryGetValue(viewType, out var preloaded))
            {
                mPreloadedPanels.Remove(viewType);
                if (preloaded.Instance != null)
                {
                    return preloaded;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 预加载协程：加载Prefab并以非激活状态实例化，挂到目标层级下隐藏等待打开
        /// </summary>
        private static IEnumerator PreloadPanelCoroutine(Type viewType, UILevel level, Action<bool> onComplete)
        {
            int generation = mPreloadGeneration;
            if (AtlasManager.EnableAutoPreload)
            {
                yield return AtlasManager.Instance.PreloadForPanelAsync(viewType).ToCoroutine();
            }

            string prefabPath = PathProvider.GetPrefabPath(viewType);
            
            if (AssetLoader == null)
            {
                LogTool.LogError($"[UIManager] AssetLoader 未配置，无法预加载Prefab: {prefabPath}");
                onComplete?.Invoke(false);
                yield break;
            }
            
            GameObject prefab = null;
            bool loaded = false;
            AssetLoader.LoadAssetAsync<GameObject>(prefabPath, asset =>
            {
                prefab = asset;
                loaded = true;
            });
            
            while (!loaded)
            {
                yield return null;
            }
            
            // 加载期间界面可能已被打开，直接丢弃本次预加载
            if (mPanels.ContainsKey(viewType))
            {
                if (prefab != null)
                {
                    AssetLoader?.ReleaseAsset(prefabPath);
                }
                // 打开路径已自行 +1 图集引用,本次预加载的引用归还
                if (AtlasManager.EnableAutoPreload)
                {
                    AtlasManager.Instance.ReleaseForPanel(viewType);
                }
                onComplete?.Invoke(true);
                yield break;
            }

            // 预加载期间被要求全部释放(如进战斗),归还图集引用并直接结束
            if (generation != mPreloadGeneration)
            {
                if (prefab != null)
                {
                    AssetLoader?.ReleaseAsset(prefabPath);
                }
                if (AtlasManager.EnableAutoPreload)
                {
                    AtlasManager.Instance.ReleaseForPanel(viewType);
                }
                onComplete?.Invoke(false);
                yield break;
            }
            
            if (prefab == null)
            {
                LogTool.LogError($"[UIManager] 预加载失败，找不到Prefab: {prefabPath}");
                onComplete?.Invoke(false);
                yield break;
            }
            
            // 以非激活状态实例化，避免 Awake/OnEnable 在预加载阶段执行业务逻辑
            bool prefabActive = prefab.activeSelf;
            prefab.SetActive(false);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            prefab.SetActive(prefabActive);
            
            // 释放资源句柄（与正常打开流程一致）
            AssetLoader?.ReleaseAsset(prefabPath);
            instance.name = prefabPath;
            
            var levelRoot = GetLevelRoot(level);
            if (levelRoot != null)
            {
                instance.transform.SetParent(levelRoot, false);
            }
            
            var panel = instance.GetComponent(viewType) as UIPanel;
            if (panel == null)
            {
                LogTool.LogError($"[UIManager] 预加载失败，Prefab上找不到{viewType.Name}组件");
                UnityEngine.Object.Destroy(instance);
                onComplete?.Invoke(false);
                yield break;
            }
            panel.Level = level;
            
            mPreloadedPanels[viewType] = new PreloadedPanel
            {
                Instance = instance,
                Level = level
            };
            LogTool.Log($"[UIManager] 预加载完成: {viewType.Name}, Level={level}");
            onComplete?.Invoke(true);
        }
        
        internal static void RemovePanel(UIPanel panel)
        {
            if (panel == null) return;
            
            Type panelType = panel.GetType();
            
            // 只有参与堆栈管理的Panel才从UIManager中移除
            if (!panel.IsManaged)
            {
                return;
            }
            
            // 获取层级信息
            UILevel level = UILevel.Common;
            if (mPanelInfos.TryGetValue(panelType, out var info))
            {
                level = info.Level;
            }
            
            // 从界面栈中同步关闭状态（仅顶层全屏界面关闭时才恢复下层）
            PanelStackManager.HandlePanelClosed(panelType, level);
            
            // PopUI 层最后一个弹窗关闭时，刷新当前全屏界面
            if (level == UILevel.PopUI && PanelStackManager.GetStackDepth(UILevel.PopUI) == 0)
            {
                RefreshCurrentFullScreenPanel();
            }
            
            if (mPanels.ContainsKey(panelType))
            {
                mPanels.Remove(panelType);
            }
            
            if (mPanelInfos.TryGetValue(panelType, out info))
            {
                if (mPanelsByLevel.TryGetValue(info.Level, out var types))
                {
                    types.Remove(panelType);
                }
                mPanelInfos.Remove(panelType);
            }
        }
        
        private static void OnPanelClosed(Type panelType, bool isManaged)
        {
            // 只有参与堆栈管理的Panel才需要处理
            if (isManaged)
            {
                // 已在RemovePanel中处理
            }
        }

        #region 内存统计

        /// <summary>
        /// Unity 当前已分配的总内存（字节，含 Native + Managed）
        /// </summary>
        private static long GetAllocatedMemory()
        {
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        }

        /// <summary>
        /// Mono/IL2CPP 托管堆已使用内存（字节）
        /// </summary>
        private static long GetMonoMemory()
        {
            return UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
        }

        /// <summary>
        /// 输出打开某个界面带来的内存增量日志
        /// 注意：增量为打开前后总内存差值，包含加载期间其他系统的分配，仅供参考
        /// </summary>
        private static void LogPanelMemory(string panelName, long memBefore, long monoBefore)
        {
            long memAfter = GetAllocatedMemory();
            long monoAfter = GetMonoMemory();
            long memDelta = memAfter - memBefore;
            long monoDelta = monoAfter - monoBefore;

            LogTool.Log($"[UIManager][Memory] 打开界面 {panelName}: " +
                        $"总内存增量={FormatBytes(memDelta)} (当前总占用={FormatBytes(memAfter)}), " +
                        $"托管堆增量={FormatBytes(monoDelta)} (当前托管堆={FormatBytes(monoAfter)})");
        }

        private static string FormatBytes(long bytes)
        {
            bool negative = bytes < 0;
            double value = Math.Abs(bytes);
            string unit = "B";
            if (value >= 1024 * 1024)
            {
                value /= 1024 * 1024;
                unit = "MB";
            }
            else if (value >= 1024)
            {
                value /= 1024;
                unit = "KB";
            }
            return $"{(negative ? "-" : "")}{value:F2}{unit}";
        }

        #endregion
        
        /// <summary>
        /// 确保 UserToolPanel 已打开（如果没有则自动打开）
        /// </summary>
        private static bool mIsOpeningUserTool = false;

        private static void BringPanelNodeToTop(UIPanel panel, string reason)
        {
            if (panel?.transform == null)
            {
                LogTool.LogWarning($"[UIManager] BringPanelNodeToTop 失败: panel为空, reason={reason}");
                return;
            }

            panel.transform.SetAsLastSibling();
            LogTool.Log($"[UIManager] 提升界面层级到最高: {panel.GetType().Name}, reason={reason}");
        }

        private static void HideUserToolPanel(string reason)
        {
            var userToolPanel = GetPanel<UserToolPanelNew>();
            if (userToolPanel != null && userToolPanel.gameObject.activeSelf)
            {
                userToolPanel.Hide();
                LogTool.Log($"[UIManager] 隐藏 UserToolPanel: reason={reason}");
            }
        }

        private static void ShowUserToolPanel(string reason)
        {
            var userToolPanel = GetPanel<UserToolPanelNew>();
            if (userToolPanel != null && !userToolPanel.gameObject.activeSelf)
            {
                userToolPanel.Show();
                LogTool.Log($"[UIManager] 显示 UserToolPanel: reason={reason}");
            }
        }

        private static void BringUserToolToTopIfNeeded(Type viewType, UILevel level)
        {
            if (viewType == typeof(UserToolPanelNew))
            {
                return;
            }

            if (level != UILevel.Common)
            {
                return;
            }

            var userToolPanel = GetPanel<UserToolPanelNew>();
            if (userToolPanel != null)
            {
                BringPanelNodeToTop(userToolPanel, $"AfterPanelLifecycle:{viewType.Name}");
            }
        }
        
        private static void EnsurePanelAtLevel(UIPanel panel, UILevel desiredLevel, string reason)
        {
            if (panel == null)
            {
                return;
            }

            if (desiredLevel != UILevel.Const && desiredLevel != UILevel.Panel )
            {
                return;
            }
            var levelRoot = GetLevelRoot(desiredLevel);
            if (levelRoot == null)
            {
                LogTool.LogWarning($"[UIManager] 无法切换层级，LevelRoot为空: panel={panel.GetType().Name}, desired={desiredLevel}, reason={reason}");
                return;
            }

            var panelType = panel.GetType();
            // 更新 mPanelsByLevel / mPanelInfos（仅对参与管理的Panel）
            if (panel.IsManaged)
            {
                if (mPanelInfos.TryGetValue(panelType, out var oldInfo))
                {
                    var oldLevel = oldInfo.Level;
                    if (mPanelsByLevel.TryGetValue(oldLevel, out var oldTypes))
                    {
                        oldTypes.Remove(panelType);
                    }

                    oldInfo.Level = desiredLevel;
                    mPanelInfos[panelType] = oldInfo;
                }

                if (!mPanelsByLevel.TryGetValue(desiredLevel, out var newTypes))
                {
                    newTypes = new List<Type>();
                    mPanelsByLevel[desiredLevel] = newTypes;
                }
                if (!newTypes.Contains(panelType))
                {
                    newTypes.Add(panelType);
                }
            }

            panel.Level = desiredLevel;
            panel.transform.SetParent(levelRoot, false);
            BringPanelNodeToTop(panel, $"EnsurePanelAtLevel:{reason}:{desiredLevel}");
        }

        private static void EnsureUserToolPanelOpen(System.Action<UserToolPanelNew> onComplete = null, UILevel desiredLevel = UILevel.Common)
        {
            // 防止递归调用（如果正在打开UserToolPanel，则直接返回）
            if (mIsOpeningUserTool) return;
            
            // 检查 UserToolPanel 是否已存在
            if (mPanels.ContainsKey(typeof(UserToolPanelNew)))
            {
                // 已经打开，直接回调
                var existingPanel = GetPanel<UserToolPanelNew>();
                if (existingPanel != null && !existingPanel.gameObject.activeSelf)
                {
                    existingPanel.Show();
                }
                EnsurePanelAtLevel(existingPanel, desiredLevel, "EnsureUserToolPanelOpen-Existing");
                BringPanelNodeToTop(existingPanel, "EnsureUserToolPanelOpen-Existing");
                onComplete?.Invoke(existingPanel);
                return;
            }
            
            mIsOpeningUserTool = true;
            
            try
            {
                // 自动打开 UserToolPanel
                var vm = new Game.UserTool.UserToolPanelViewModel();
                vm.OnCreate();
                
                // 使用OpenPanel打开，并等待完成
                OpenPanelAsync<UserToolPanelNew, Game.UserTool.UserToolPanelViewModel>(
                    vm,
                    desiredLevel,
                    onComplete: panel =>
                    {
                        mIsOpeningUserTool = false;
                        EnsurePanelAtLevel(panel, desiredLevel, "EnsureUserToolPanelOpen-New");
                        BringPanelNodeToTop(panel, "EnsureUserToolPanelOpen-New");
                        onComplete?.Invoke(panel);
                    }).Forget();
                
                LogTool.Log("[UIManager] 自动打开 UserToolPanel");
            }
            catch
            {
                mIsOpeningUserTool = false;
                throw;
            }
        }
        
        #endregion
    }
}
