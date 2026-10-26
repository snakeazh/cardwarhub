using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UIFramework
{
    /// <summary>
    /// 界面栈节点数据
    /// </summary>
    public class PanelStackNode
    {
        /// <summary>
        /// 界面类型
        /// </summary>
        public Type PanelType { get; set; }
        
        /// <summary>
        /// ViewModel类型
        /// </summary>
        public Type ViewModelType { get; set; }
        
        /// <summary>
        /// 打开时间
        /// </summary>
        public DateTime OpenTime { get; set; }
        
        /// <summary>
        /// 界面层级
        /// </summary>
        public UILevel Level { get; set; }
        
        /// <summary>
        /// 是否是全屏界面
        /// </summary>
        public bool IsFullScreen { get; set; }
        
        /// <summary>
        /// 自定义数据（可用于恢复状态）
        /// </summary>
        public object CustomData { get; set; }
        
        /// <summary>
        /// 是否允许返回（某些界面如Loading不允许返回）
        /// </summary>
        public bool CanBack { get; set; } = true;
        
        /// <summary>
        /// ViewModel实例引用
        /// </summary>
        public ViewModel ViewModel { get; set; }
    }
    
    /// <summary>
    /// 界面栈管理器 - 统一管理所有界面栈，包括全屏界面的入栈出栈
    /// </summary>
    public static class PanelStackManager
    {
        #region 字段
        
        /// <summary>
        /// 弹窗栈（PopUI层级）
        /// </summary>
        private static readonly Stack<PanelStackNode> mPopupStack = new Stack<PanelStackNode>();
        
        /// <summary>
        /// 引导栈（Guide层级）
        /// </summary>
        private static readonly Stack<PanelStackNode> mGuideStack = new Stack<PanelStackNode>();
        
        /// <summary>
        /// 全屏界面栈（Common层级的全屏界面）
        /// </summary>
        private static readonly Stack<PanelStackNode> mFullScreenStack = new Stack<PanelStackNode>();
        
        /// <summary>
        /// 当前显示的全屏界面类型
        /// </summary>
        private static Type mCurrentFullScreenPanelType = null;
        
        /// <summary>
        /// 栈变化事件
        /// </summary>
        public static event Action<PanelStackNode, StackAction> OnStackChanged;
        
        /// <summary>
        /// 返回事件（在Back操作前触发，可取消）
        /// </summary>
        public static event Func<bool> OnBeforeBack;
        
        /// <summary>
        /// 是否正在处理Back操作（用于避免重复Pop）
        /// </summary>
        private static bool mIsProcessingBack = false;
        
        #endregion
        
        #region 公共API - 打开界面
        
        /// <summary>
        /// 将界面压入栈
        /// </summary>
        public static void Push<TView, TViewModel>(TViewModel viewModel, UILevel level = UILevel.Common, object customData = null, bool canBack = true)
            where TView : UIPanel<TViewModel>
            where TViewModel : ViewModel
        {
            var node = new PanelStackNode
            {
                PanelType = typeof(TView),
                ViewModelType = typeof(TViewModel),
                ViewModel = viewModel,
                OpenTime = DateTime.Now,
                Level = level,
                IsFullScreen = viewModel.IsFullScreen,
                CustomData = customData,
                CanBack = canBack
            };
            
            // 根据层级和是否全屏界面，决定入哪个栈
            if (level == UILevel.Common || level == UILevel.Panel)
            {
                viewModel.IsFullScreen = true;
                // 全屏界面：隐藏当前全屏界面，入全屏栈
                PushFullScreenPanel(node);
            }
            else if (level == UILevel.PopUI)
            {
                // 弹窗：入弹窗栈
                mPopupStack.Push(node);
                LogTool.Log($"[PanelStackManager] Push Popup: {typeof(TView).Name}, StackDepth: {mPopupStack.Count}");
            }
            else if (level == UILevel.Guide)
            {
                // 引导：入引导栈
                mGuideStack.Push(node);
                LogTool.Log($"[PanelStackManager] Push Guide: {typeof(TView).Name}, StackDepth: {mGuideStack.Count}");
            }
            else
            {
                // 其他Common层级非全屏界面：不入栈，只记录当前
                LogTool.Log($"[PanelStackManager] Push Normal: {typeof(TView).Name}, Level: {level}");
            }
            
            OnStackChanged?.Invoke(node, StackAction.Push);
        }
        
        /// <summary>
        /// 压入全屏界面栈
        /// </summary>
        private static bool PushFullScreenPanel(PanelStackNode node)
        {
            // 如果当前有全屏界面显示，将其压入栈并隐藏
            if (mCurrentFullScreenPanelType != null)
            {
                var currentNode = CreateNodeFromCurrentPanel();
                if (currentNode != null)
                {
                    var currentPanel = GetPanelInstance(mCurrentFullScreenPanelType);
                    mFullScreenStack.Push(currentNode);

                    if (mCurrentFullScreenPanelType != node.PanelType)
                    {
                        LogPanelHierarchy(currentPanel, $"PushFullScreenPanel - Before Hide Current={mCurrentFullScreenPanelType.Name}");
                        HidePanel(mCurrentFullScreenPanelType);
                        LogTool.Log($"[PanelStackManager] 隐藏并压入栈: {mCurrentFullScreenPanelType.Name}");
                    }
                    else
                    {
                        LogTool.Log($"[PanelStackManager] 重复打开全屏界面，记录新栈层: {mCurrentFullScreenPanelType.Name}");
                    }
                }
            }
            
            mCurrentFullScreenPanelType = node.PanelType;
            var nextPanel = GetPanelInstance(node.PanelType);
            LogPanelHierarchy(nextPanel, $"PushFullScreenPanel - New Current={node.PanelType.Name}");
            LogTool.Log($"[PanelStackManager] Push FullScreen: {node.PanelType.Name}, StackDepth: {mFullScreenStack.Count}");
            return true;
        }
        
        #endregion
        
        #region 公共API - 关闭/返回
        
        /// <summary>
        /// 面板关闭时同步栈状态（由 UIManager.RemovePanel 调用）。
        /// 仅当关闭的是当前顶层全屏界面时才恢复下层；关闭栈内隐藏界面时只移除栈节点，避免对已释放 VM 的面板误调 Show。
        /// </summary>
        public static void HandlePanelClosed(Type closedPanelType, UILevel level)
        {
            if (closedPanelType == null)
            {
                return;
            }

            var stack = GetStackByLevel(level);
            if (stack != null)
            {
                Pop(level, isAutoPop: true);
                return;
            }

            if (closedPanelType == mCurrentFullScreenPanelType)
            {
                PopFullScreenPanel(isAutoPop: true);
                return;
            }

            if (RemoveFromFullScreenStack(closedPanelType))
            {
                LogTool.Log($"[PanelStackManager] 从全屏栈移除已关闭的隐藏界面: {closedPanelType.Name}");
            }
        }

        /// <summary>
        /// 从栈中弹出当前界面（由UIManager自动调用）
        /// </summary>
        public static bool Pop(UILevel? level = null, bool isAutoPop = true)
        {
            var stack = level.HasValue ? GetStackByLevel(level.Value) : GetTopStack();
            
            if (stack == null)
            {
                // 可能是全屏界面
                if (mCurrentFullScreenPanelType != null)
                {
                    return PopFullScreenPanel(isAutoPop);
                }
                return false;
            }
            
            if (stack.Count == 0)
            {
                LogTool.Log("[PanelStackManager] Pop: 栈为空");
                return false;
            }
            
            // 检查栈顶是否匹配（避免重复Pop）
            var topNode = stack.Peek();
            if (isAutoPop && !mIsProcessingBack)
            {
                if (!IsPanelActive(topNode.PanelType))
                {
                    LogTool.Log($"[PanelStackManager] Pop: 界面 {topNode.PanelType.Name} 已被关闭，跳过");
                    return false;
                }
            }
            
            var node = stack.Pop();
            LogTool.Log($"[PanelStackManager] Pop: {node.PanelType.Name}, Level: {node.Level}, StackDepth: {stack.Count}, Auto={isAutoPop}");
            
            OnStackChanged?.Invoke(node, StackAction.Pop);
            return true;
        }
        
        /// <summary>
        /// 弹出全屏界面
        /// </summary>
        private static bool PopFullScreenPanel(bool isAutoPop)
        {
            if (mCurrentFullScreenPanelType == null)
            {
                return false;
            }
            
            if (isAutoPop && !mIsProcessingBack)
            {
                if (!IsPanelActive(mCurrentFullScreenPanelType))
                {
                    LogTool.Log($"[PanelStackManager] Pop FullScreen: 界面已被关闭，跳过");
                    return false;
                }
            }
            
            // 关闭当前全屏界面
            var closedType = mCurrentFullScreenPanelType;
            
            // 从栈中恢复上一个全屏界面
            if (mFullScreenStack.Count > 0)
            {
                var node = mFullScreenStack.Pop();
                RestoreFullScreenPanel(node);
                mCurrentFullScreenPanelType = node.PanelType;
                
                LogTool.Log($"[PanelStackManager] Pop FullScreen: 关闭 {closedType.Name}, 恢复 {node.PanelType.Name}, StackDepth: {mFullScreenStack.Count}");
                OnStackChanged?.Invoke(node, StackAction.Restore);
            }
            else
            {
                mCurrentFullScreenPanelType = null;
                LogTool.Log($"[PanelStackManager] Pop FullScreen: 关闭 {closedType.Name}, 栈为空");
            }
            
            return true;
        }
        
        /// <summary>
        /// 返回上一级（自动关闭当前界面）
        /// 优先级：弹窗栈 > 引导栈 > 全屏界面栈
        /// </summary>
        public static bool Back()
        {
            // 触发返回前事件
            if (OnBeforeBack != null)
            {
                foreach (Func<bool> handler in OnBeforeBack.GetInvocationList())
                {
                    if (!handler())
                    {
                        LogTool.Log("[PanelStackManager] Back: 被事件取消");
                        return false;
                    }
                }
            }
            
            // 优先关闭弹窗栈
            if (mPopupStack.Count > 0)
            {
                return BackFromStack(mPopupStack);
            }
            
            // 再关闭引导栈
            if (mGuideStack.Count > 0)
            {
                return BackFromStack(mGuideStack);
            }
            
            // 最后关闭全屏界面栈
            if (mCurrentFullScreenPanelType != null)
            {
                return BackFromFullScreenStack();
            }
            
            LogTool.Log("[PanelStackManager] Back: 所有栈为空");
            return false;
        }
        
        /// <summary>
        /// 返回到指定界面
        /// </summary>
        public static bool BackTo<TView>() where TView : UIPanel
        {
            var targetType = typeof(TView);
            
            // 查找界面在哪个栈
            var popupResult = FindInStack(mPopupStack, targetType);
            var guideResult = FindInStack(mGuideStack, targetType);
            var fullScreenResult = FindInFullScreenStack(targetType);
            
            // 优先处理弹窗栈
            if (popupResult.found)
            {
                return PopToIndex(mPopupStack, popupResult.index);
            }
            
            // 再处理引导栈
            if (guideResult.found)
            {
                return PopToIndex(mGuideStack, guideResult.index);
            }
            
            // 最后处理全屏界面栈
            if (fullScreenResult.found)
            {
                return PopFullScreenToIndex(fullScreenResult.index);
            }
            
            LogTool.LogWarning($"[PanelStackManager] BackTo: 找不到界面 {targetType.Name}");
            return false;
        }

        /// <summary>
        /// 按界面类型恢复到顶层（若已存在于栈中）
        /// 不关闭上层界面，只调整栈顺序与显示顺序。
        /// </summary>
        public static bool RestoreTo(Type targetType, UILevel level)
        {
            if (targetType == null)
            {
                return false;
            }

            LogStackSnapshot($"Before RestoreTo: target={targetType.Name}, level={level}");

            if (level == UILevel.PopUI)
            {
                var reordered = MoveToTop(mPopupStack, targetType);
                if (!reordered)
                {
                    return false;
                }

                ShowPanel(targetType);
                BringPanelToFront(targetType);
                OnStackChanged?.Invoke(new PanelStackNode { PanelType = targetType, Level = level }, StackAction.Restore);
                LogTool.Log($"[PanelStackManager] RestoreTo Popup (Reorder): {targetType.Name}");
                LogStackSnapshot($"After RestoreTo Popup: target={targetType.Name}");
                return true;
            }

            if (level == UILevel.Guide)
            {
                var reordered = MoveToTop(mGuideStack, targetType);
                if (!reordered)
                {
                    return false;
                }

                ShowPanel(targetType);
                BringPanelToFront(targetType);
                OnStackChanged?.Invoke(new PanelStackNode { PanelType = targetType, Level = level }, StackAction.Restore);
                LogTool.Log($"[PanelStackManager] RestoreTo Guide (Reorder): {targetType.Name}");
                LogStackSnapshot($"After RestoreTo Guide: target={targetType.Name}");
                return true;
            }

            if (level == UILevel.Common || level == UILevel.Panel)
            {
                var reordered = MoveFullScreenToCurrent(targetType);
                if (!reordered)
                {
                    return false;
                }

                ShowPanel(targetType);
                BringPanelToFront(targetType);
                OnStackChanged?.Invoke(new PanelStackNode { PanelType = targetType, Level = level }, StackAction.Restore);
                LogTool.Log($"[PanelStackManager] RestoreTo FullScreen (Reorder): {targetType.Name}");
                LogStackSnapshot($"After RestoreTo FullScreen: target={targetType.Name}");
                return true;
            }

            return false;
        }
        
        #endregion
        
        #region 公共API - 查询状态
        
        /// <summary>
        /// 获取当前显示的全屏界面类型
        /// </summary>
        public static Type GetCurrentFullScreenPanelType()
        {
            return mCurrentFullScreenPanelType;
        }
        
        /// <summary>
        /// 获取栈深度
        /// </summary>
        public static int GetStackDepth(UILevel level)
        {
            switch (level)
            {
                case UILevel.PopUI:
                    return mPopupStack.Count;
                case UILevel.Guide:
                    return mGuideStack.Count;
                case UILevel.Common:
                    return mFullScreenStack.Count + (mCurrentFullScreenPanelType != null ? 1 : 0);
                default:
                    return 0;
            }
        }
        
        /// <summary>
        /// 判断是否允许返回
        /// </summary>
        public static bool CanBack()
        {
            if (mPopupStack.Count > 0) return true;
            if (mGuideStack.Count > 0) return true;
            if (mCurrentFullScreenPanelType != null) return true;
            return false;
        }
        
        /// <summary>
        /// 判断指定界面是否在栈中
        /// </summary>
        public static bool Contains<TView>() where TView : UIPanel
        {
            var targetType = typeof(TView);
            return mPopupStack.Any(n => n.PanelType == targetType) ||
                   mGuideStack.Any(n => n.PanelType == targetType) ||
                   mFullScreenStack.Any(n => n.PanelType == targetType) ||
                   mCurrentFullScreenPanelType == targetType;
        }
        
        /// <summary>
        /// 获取栈信息（用于调试）
        /// </summary>
        public static string GetStackInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Panel Stack Info ===");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"CanBack: {CanBack()}");
            sb.AppendLine();

            sb.AppendLine("[FullScreen]");
            sb.AppendLine($"  Current: {FormatCurrentFullScreenInfo()}");
            sb.AppendLine($"  Hidden Stack Count: {mFullScreenStack.Count}");
            AppendStackDetails(sb, mFullScreenStack, "  Hidden", isHiddenStack: true);
            sb.AppendLine();

            sb.AppendLine("[Popup]");
            sb.AppendLine($"  Count: {mPopupStack.Count}");
            AppendStackDetails(sb, mPopupStack, "  Popup", isHiddenStack: false);
            sb.AppendLine();

            sb.AppendLine("[Guide]");
            sb.AppendLine($"  Count: {mGuideStack.Count}");
            AppendStackDetails(sb, mGuideStack, "  Guide", isHiddenStack: false);
            sb.AppendLine();

            return sb.ToString();
        }

        private static void LogStackSnapshot(string context)
        {
            LogTool.Log($"[PanelStackManager] {context}\n{GetStackInfo()}");
        }

        private static void LogPanelHierarchy(UIPanel panel, string context)
        {
            if (panel == null || panel.transform == null)
            {
                LogTool.LogWarning($"[PanelStackManager] {context} - panel is null");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[PanelStackManager] {context}");
            foreach (var child in mFullScreenStack)
            {
                sb.Append(child.PanelType.ToString());
                sb.Append("\t");

            }
            LogTool.Log(sb.ToString());
        }

        private static void AppendTransformHierarchy(System.Text.StringBuilder sb, Transform current, int depth)
        {
            if (current == null)
            {
                return;
            }

            var indent = new string(' ', depth * 2);
            var active = current.gameObject.activeSelf;
            sb.AppendLine($"{indent}- {current.name} (active={active}, childCount={current.childCount})");

            for (int i = 0; i < current.childCount; i++)
            {
                AppendTransformHierarchy(sb, current.GetChild(i), depth + 1);
            }
        }

        private static void AppendStackDetails(System.Text.StringBuilder sb, Stack<PanelStackNode> stack, string title, bool isHiddenStack)
        {
            var list = stack.Reverse().ToList();
            if (list.Count == 0)
            {
                sb.AppendLine($"{title}: <empty>");
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var node = list[i];
                var panel = GetPanelInstance(node.PanelType);
                var isActive = panel != null && panel.gameObject != null && panel.gameObject.activeSelf;
                var instanceName = panel != null ? panel.name : "<not-loaded>";
                var vmName = node.ViewModelType != null ? node.ViewModelType.Name : node.ViewModel?.GetType().Name ?? "<null>";
                var marker = i == list.Count - 1 ? "[Top]" : $"[#{i}]";
                var hiddenTag = isHiddenStack ? "Hidden" : (isActive ? "Active" : "Inactive");

                sb.AppendLine($"{title} {marker} Panel={node.PanelType?.Name ?? "<null>"}, VM={vmName}, Level={node.Level}, State={hiddenTag}, CanBack={node.CanBack}, OpenTime={node.OpenTime:HH:mm:ss.fff}, Instance={instanceName}");
            }
        }

        private static string FormatCurrentFullScreenInfo()
        {
            if (mCurrentFullScreenPanelType == null)
            {
                return "None";
            }

            var panel = GetPanelInstance(mCurrentFullScreenPanelType);
            var isActive = panel != null && panel.gameObject != null && panel.gameObject.activeSelf;
            var instanceName = panel != null ? panel.name : "<not-loaded>";
            var level = panel != null ? panel.Level.ToString() : UILevel.Common.ToString();
            return $"Panel={mCurrentFullScreenPanelType.Name}, Level={level}, Active={isActive}, Instance={instanceName}";
        }
        
        #endregion
        
        #region 公共API - 清空栈
        
        /// <summary>
        /// 清空指定层级的栈
        /// </summary>
        public static void ClearStack(UILevel level)
        {
            if (level == UILevel.Common)
            {
                // 清空全屏界面栈
                while (mFullScreenStack.Count > 0)
                {
                    var node = mFullScreenStack.Pop();
                    OnStackChanged?.Invoke(node, StackAction.Clear);
                }
                if (mCurrentFullScreenPanelType != null)
                {
                    ClosePanel(mCurrentFullScreenPanelType);
                    mCurrentFullScreenPanelType = null;
                }
            }
            else if (level == UILevel.PopUI)
            {
                while (mPopupStack.Count > 0)
                {
                    var node = mPopupStack.Pop();
                    ClosePanel(node.PanelType);
                    OnStackChanged?.Invoke(node, StackAction.Clear);
                }
            }
            else if (level == UILevel.Guide)
            {
                while (mGuideStack.Count > 0)
                {
                    var node = mGuideStack.Pop();
                    ClosePanel(node.PanelType);
                    OnStackChanged?.Invoke(node, StackAction.Clear);
                }
            }
            
            LogTool.Log($"[PanelStackManager] ClearStack: Level={level}");
        }
        
        /// <summary>
        /// 清空所有栈
        /// </summary>
        public static void ClearAllStacks()
        {
            ClearStack(UILevel.Common);
            ClearStack(UILevel.PopUI);
            ClearStack(UILevel.Guide);
            LogTool.Log("[PanelStackManager] ClearAllStacks");
        }
        
        #endregion
        
        #region 私有方法
        
        private static bool RemoveFromFullScreenStack(Type panelType)
        {
            if (mFullScreenStack.Count == 0)
            {
                return false;
            }

            bool removed = false;
            var remaining = new Stack<PanelStackNode>();
            while (mFullScreenStack.Count > 0)
            {
                var node = mFullScreenStack.Pop();
                if (node.PanelType == panelType)
                {
                    removed = true;
                    OnStackChanged?.Invoke(node, StackAction.Pop);
                }
                else
                {
                    remaining.Push(node);
                }
            }

            while (remaining.Count > 0)
            {
                mFullScreenStack.Push(remaining.Pop());
            }

            return removed;
        }

        private static Stack<PanelStackNode> GetStackByLevel(UILevel level)
        {
            switch (level)
            {
                case UILevel.PopUI:
                    return mPopupStack;
                case UILevel.Tips:
                    return mPopupStack;
                case UILevel.Guide:
                    return mGuideStack;
                case UILevel.Common:
                    
                default:
                    return null; // Common层级的全屏界面不走这个栈
            }
        }
        
        private static Stack<PanelStackNode> GetTopStack()
        {
            if (mPopupStack.Count > 0) return mPopupStack;
            if (mGuideStack.Count > 0) return mGuideStack;
            return null;
        }
        
        private static bool BackFromStack(Stack<PanelStackNode> stack)
        {
            if (stack.Count == 0) return false;
            
            var node = stack.Peek();
            if (!node.CanBack)
            {
                LogTool.Log($"[PanelStackManager] Back: {node.PanelType.Name} 不允许返回");
                return false;
            }
            
            mIsProcessingBack = true;
            stack.Pop();
            OnStackChanged?.Invoke(node, StackAction.Back);

            if (stack.Count > 0 && stack.Peek().PanelType == node.PanelType)
            {
                ShowPanel(node.PanelType);
                BringPanelToFront(node.PanelType);
                LogTool.Log($"[PanelStackManager] Back: 消费重复栈记录 {node.PanelType.Name}");
            }
            else
            {
                ClosePanel(node.PanelType);
                LogTool.Log($"[PanelStackManager] Back: 关闭 {node.PanelType.Name}");
            }

            mIsProcessingBack = false;
            return true;
        }
        
        private static bool BackFromFullScreenStack()
        {
            if (mCurrentFullScreenPanelType == null) return false;
            
            mIsProcessingBack = true;

            if (mFullScreenStack.Count > 0 && mFullScreenStack.Peek().PanelType == mCurrentFullScreenPanelType)
            {
                var duplicateNode = mFullScreenStack.Pop();
                OnStackChanged?.Invoke(duplicateNode, StackAction.Back);
                ShowPanel(mCurrentFullScreenPanelType);
                BringPanelToFront(mCurrentFullScreenPanelType);
                LogTool.Log($"[PanelStackManager] Back: 消费重复全屏栈记录 {mCurrentFullScreenPanelType.Name}");
                mIsProcessingBack = false;
                return true;
            }
            
            // 关闭当前界面
            var closedType = mCurrentFullScreenPanelType;
            ClosePanel(mCurrentFullScreenPanelType);
            
            // 恢复上一个界面
            if (mFullScreenStack.Count > 0)
            {
                var node = mFullScreenStack.Pop();
                RestoreFullScreenPanel(node);
                mCurrentFullScreenPanelType = node.PanelType;
                OnStackChanged?.Invoke(node, StackAction.Back);
                LogTool.Log($"[PanelStackManager] Back: 关闭 {closedType.Name}, 恢复 {node.PanelType.Name}");
            }
            else
            {
                mCurrentFullScreenPanelType = null;
                LogTool.Log($"[PanelStackManager] Back: 关闭 {closedType.Name}, 栈为空");
            }
            
            mIsProcessingBack = false;
            return true;
        }
        
        private static (bool found, int index) FindInStack(Stack<PanelStackNode> stack, Type targetType)
        {
            var list = stack.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].PanelType == targetType)
                {
                    return (true, i);
                }
            }
            return (false, -1);
        }
        
        private static (bool found, int index) FindInFullScreenStack(Type targetType)
        {
            // 检查当前显示的界面
            if (mCurrentFullScreenPanelType == targetType)
            {
                return (true, -1); // -1 表示当前显示
            }
            
            // 在栈中查找
            var list = mFullScreenStack.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].PanelType == targetType)
                {
                    return (true, i);
                }
            }
            return (false, -1);
        }
        
        private static bool PopToIndex(Stack<PanelStackNode> stack, int targetIndex)
        {
            int popCount = targetIndex;
            
            for (int i = 0; i < popCount; i++)
            {
                var node = stack.Pop();
                ClosePanel(node.PanelType);
                OnStackChanged?.Invoke(node, StackAction.Back);
            }
            
            LogTool.Log($"[PanelStackManager] PopToIndex: 关闭 {popCount} 个界面");
            return true;
        }

        private static bool MoveToTop(Stack<PanelStackNode> stack, Type targetType)
        {
            var list = stack.Reverse().ToList();
            var index = list.FindIndex(n => n.PanelType == targetType);
            if (index < 0)
            {
                return false;
            }

            var targetNode = list[index];
            list.RemoveAt(index);
            list.Add(targetNode);

            stack.Clear();
            foreach (var node in list)
            {
                stack.Push(node);
            }

            return true;
        }

        private static bool MoveFullScreenToCurrent(Type targetType)
        {
            if (mCurrentFullScreenPanelType == targetType)
            {
                return true;
            }

            var list = mFullScreenStack.Reverse().ToList();
            var index = list.FindIndex(n => n.PanelType == targetType);
            if (index < 0)
            {
                return false;
            }

            if (mCurrentFullScreenPanelType != null)
            {
                var currentNode = CreateNodeFromCurrentPanel();
                if (currentNode != null)
                {
                    list.Add(currentNode);
                }
                HidePanel(mCurrentFullScreenPanelType);
            }

            var targetNode = list[index];
            list.RemoveAt(index);
            mCurrentFullScreenPanelType = targetNode.PanelType;

            mFullScreenStack.Clear();
            foreach (var node in list)
            {
                if (node.PanelType != mCurrentFullScreenPanelType)
                {
                    mFullScreenStack.Push(node);
                }
            }

            return true;
        }

        private static bool PopFullScreenToIndex(int targetIndex)
        {
            if (targetIndex == -1)
            {
                // 目标就是当前显示的界面，无需操作
                return true;
            }
            
            // 关闭当前界面
            if (mCurrentFullScreenPanelType != null)
            {
                ClosePanel(mCurrentFullScreenPanelType);
            }
            
            // 弹出不需要的界面
            int popCount = targetIndex;
            for (int i = 0; i < popCount; i++)
            {
                var node = mFullScreenStack.Pop();
                LogTool.Log($"[PanelStackManager] PopFullScreenToIndex: 弹出 {node.PanelType.Name}");
            }
            
            // 恢复目标界面
            var targetNode = mFullScreenStack.Pop();
            RestoreFullScreenPanel(targetNode);
            mCurrentFullScreenPanelType = targetNode.PanelType;
            
            LogTool.Log($"[PanelStackManager] PopFullScreenToIndex: 恢复到 {targetNode.PanelType.Name}");
            OnStackChanged?.Invoke(targetNode, StackAction.BackTo);
            return true;
        }
        
        private static PanelStackNode CreateNodeFromCurrentPanel()
        {
            if (mCurrentFullScreenPanelType == null) return null;
            
            var panel = GetPanelInstance(mCurrentFullScreenPanelType);
            if (panel is IView view)
            {
                return new PanelStackNode
                {
                    PanelType = mCurrentFullScreenPanelType,
                    ViewModel = view.ViewModel,
                    OpenTime = DateTime.Now,
                    Level = UILevel.Common,
                    IsFullScreen = true
                };
            }
            return null;
        }
        
        private static void RestoreFullScreenPanel(PanelStackNode node)
        {
            if (node?.PanelType == null) return;
            
            ShowPanel(node.PanelType);
            LogTool.Log($"[PanelStackManager] 恢复显示: {node.PanelType.Name}");
        }
        
        private static bool IsPanelActive(Type panelType)
        {
            return UIManager.ContainsPanel(panelType);
        }
        
        private static void HidePanel(Type panelType)
        {
            UIManager.HidePanel(panelType);
        }
        
        private static void ShowPanel(Type panelType)
        {
            UIManager.ShowPanel(panelType);
        }

        private static void BringPanelToFront(Type panelType)
        {
            try
            {
                var panel = GetPanelInstance(panelType);
                // if (panel?.transform != null)
                {
                    // panel.transform.SetAsLastSibling();
                }
            }
            catch (Exception ex)
            {
                LogTool.LogError($"[PanelStackManager] BringPanelToFront 失败: {ex.Message}");
            }
        }
        
        private static void ClosePanel(Type panelType)
        {
            UIManager.ClosePanel(panelType);
        }
        
        private static UIPanel GetPanelInstance(Type panelType)
        {
            return UIManager.GetPanel(panelType);
        }
        
        #endregion
    }
    
    /// <summary>
    /// 栈操作类型
    /// </summary>
    public enum StackAction
    {
        Push,       // 压栈
        Pop,        // 出栈
        Back,       // 返回
        Restore,    // 恢复
        BackTo,     // 返回到指定界面
        Clear       // 清空
    }
}