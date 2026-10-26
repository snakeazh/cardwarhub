using UIFramework.Services;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using FusionContinent;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Toast提示服务 - 全局管理Toast提示
    /// </summary>
    public class ToastService : GameServiceBase
    {
        #region 属性
        private Queue<ToastData> mToastQueue = new Queue<ToastData>();
        private bool mIsShowing = false;
        private ToastData mCurrentToast = null;
        
        // 默认显示时长
        private const float DEFAULT_DURATION = 2f;
        #endregion

        #region 事件
        /// <summary>
        /// 显示Toast事件
        /// </summary>
        public event Action<string, float, ToastType> OnShowToast;

        /// <summary>
        /// Toast关闭事件
        /// </summary>
        public event Action OnToastClosed;
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            LogTool.Log("[ToastService] 初始化完成");
        }

        #region 公共方法 - 便捷接口

        /// <summary>
        /// 显示普通Toast
        /// </summary>
        public void Show(string message, float duration = DEFAULT_DURATION)
        {
            ShowToast(message, duration, ToastType.Normal);
        }

        /// <summary>
        /// 显示成功Toast
        /// </summary>
        public void ShowSuccess(string message, float duration = DEFAULT_DURATION)
        {
            ShowToast(message, duration, ToastType.Success);
        }

        /// <summary>
        /// 显示警告Toast
        /// </summary>
        public void ShowWarning(string message, float duration = DEFAULT_DURATION)
        {
            ShowToast(message, duration, ToastType.Warning);
        }

        /// <summary>
        /// 显示错误Toast
        /// </summary>
        public void ShowError(string message, float duration = DEFAULT_DURATION)
        {
            ShowToast(message, duration, ToastType.Error);
        }

        /// <summary>
        /// 显示带类型的Toast
        /// </summary>
        public void Show(string message, float duration, ToastType type)
        {
            ShowToast(message, duration, type);
        }

        #endregion

        /// <summary>
        /// 显示Toast提示（基础方法）- 如果有上个tips，删除旧的显示新的
        /// </summary>
        public void ShowToast(string message, float duration = 2f, ToastType type = ToastType.Normal)
        {
            // 清空队列和当前显示，立即显示新的（参考ShowTipManager的逻辑）
            ClearAllToasts();
            duration = 1f;
            var toastData = new ToastData
            {
                Message = message,
                Duration = duration,
                Type = type
            };

            mToastQueue.Enqueue(toastData);
            LogTool.Log($"[ToastService] 添加消息到队列: {message}");

            // 立即显示
            if (!mIsShowing)
            {
                ShowNextToast();
            }
        }



        /// <summary>
        /// 关闭当前Toast
        /// </summary>
        public void CloseCurrentToast()
        {
            if (mIsShowing)
            {
                mIsShowing = false;
                OnToastClosed?.Invoke();
            }
        }

        /// <summary>
        /// 清空所有待显示的Toast
        /// </summary>
        public void ClearAllToasts()
        {
            mToastQueue.Clear();
            mIsShowing = false;
            mCurrentToast = null;
            OnToastClosed?.Invoke();
            LogTool.Log("[ToastService] 清空所有Toast");
        }

        /// <summary>
        /// 获取队列中的消息数量
        /// </summary>
        public int GetQueueCount()
        {
            return mToastQueue.Count;
        }

        /// <summary>
        /// 是否正在显示Toast
        /// </summary>
        public bool IsShowing()
        {
            return mIsShowing;
        }

        #region 私有方法

        private async void ShowNextToast()
        {
            if (mToastQueue.Count == 0)
            {
                mIsShowing = false;
                mCurrentToast = null;
                return;
            }

            mIsShowing = true;
            mCurrentToast = mToastQueue.Dequeue();

            LogTool.Log($"[ToastService] 显示Toast: {mCurrentToast.Message}, 类型: {mCurrentToast.Type}");

            // 确保 ToastPanel 是打开的
            await EnsureToastPanelOpen();

            // 触发显示事件（带类型）- 这会替换旧的Toast
            OnShowToast?.Invoke(mCurrentToast.Message, mCurrentToast.Duration, mCurrentToast.Type);

            // 等待显示时间（动画时间0.5s + 持续时间 + 消失时间0.5s）
            float totalTime = mCurrentToast.Duration + 1.0f;
            await UniTask.Delay(
                TimeSpan.FromSeconds(totalTime),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update);

            // 显示完成
            mIsShowing = false;
            mCurrentToast = null;

            // 显示下一个（如果有的话）
            ShowNextToast();
        }

        /// <summary>
        /// 确保 ToastPanel 是打开的
        /// </summary>
        private async UniTask EnsureToastPanelOpen()
        {
            // 检查 ToastPanel 是否已打开
            var toastPanel = UIFramework.UIManager.GetPanel<ToastPanel>();
            if (toastPanel == null)
            {
                // 打开 ToastPanel
                var vm = new ToastViewModel();
                vm.OnCreate();
                await UIFramework.UIManager.OpenPanelAsync<ToastPanel, ToastViewModel>(
                    vm,
                    UIFramework.UILevel.Tips);
                
                LogTool.Log("[ToastService] ToastPanel 已打开");
            }
        }

        #endregion
    }

    /// <summary>
    /// Toast类型
    /// </summary>
    public enum ToastType
    {
        Normal,     // 普通提示
        Success,    // 成功提示
        Warning,    // 警告提示
        Error       // 错误提示
    }
}
