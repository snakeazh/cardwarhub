using FusionContinent.Helper;
using FusionContinent.Model;
using FusionContinent.Sounds;
using Game.Loading;
using Manager;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIFramework;
using UIFramework.Services;

namespace FusionContinent
{
    /// <summary>
    /// 加载界面 - 新框架版本 (MVVM)
    /// 复用原 LoadingPanel 的 UI 组件
    /// </summary>
    [PrefabPath("LoadingPanel")]
    public class LoadingPanelNew : UIPanel<LoadingPanelViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private LoadingPanel mOldPanel;

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<LoadingPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[LoadingPanelNew] 找不到 LoadingPanel 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            LogTool.Log("[LoadingPanelNew] 已关联 LoadingPanel");
        }

        protected override void OnBindViewModel(LoadingPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性到 UI
            BindProperties(vm);

            // 绑定 ViewModel 事件
            vm.OnLoadCompleted += OnLoadCompleted;
            vm.OnLoadFailed += OnLoadFailed;
        }

        protected override void OnUnbindViewModel(LoadingPanelViewModel vm)
        {
            // 取消事件订阅
            vm.OnLoadCompleted -= OnLoadCompleted;
            vm.OnLoadFailed -= OnLoadFailed;
        }

        private void BindProperties(LoadingPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 进度条
            vm.CurrentProgress.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.progressBar != null)
                {
                    mOldPanel.progressBar.value = newVal;
                }
            };

            // 进度文本
            vm.ProgressText.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.ProgressText != null)
                {
                    mOldPanel.ProgressText.text = newVal;
                }
            };

            // 提示文本
            vm.TipText.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.TipText != null)
                {
                    mOldPanel.TipText.text = newVal;
                }
            };

            // 提示文本1
            vm.TipText1.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.Tips != null && mOldPanel.Tips.childCount > 0)
                {
                    var text = mOldPanel.Tips.GetChild(0).GetComponent<TMP_Text>();
                    if (text != null) text.text = newVal;
                }
            };

            // 提示文本2
            vm.TipText2.OnValueChanged += (oldVal, newVal) =>
            {
                if (mOldPanel.Tips != null && mOldPanel.Tips.childCount > 1)
                {
                    var text = mOldPanel.Tips.GetChild(1).GetComponent<TMP_Text>();
                    if (text != null) text.text = newVal;
                }
            };
        }

        /// <summary>
        /// 开始加载
        /// </summary>
        public void StartLoad(string targetScene)
        {
            if (VM == null)
            {
                LogTool.LogError("[LoadingPanelNew] ViewModel 未绑定");
                return;
            }

            // 初始化 Spine 动画
            if (mOldPanel?.Spine != null)
            {
                var spine = mOldPanel.Spine.GetComponentInChildren<SkeletonGraphic>();
                if (spine != null)
                {
                    spine.Initialize(true);
                }
            }

            // 调用 ViewModel 开始加载
            VM.OnStartLoadCommand.Execute(targetScene);
        }

        private void OnLoadCompleted(string sceneName)
        {
            LogTool.Log($"[LoadingPanelNew] 场景加载完成: {sceneName}");

            // 播放音效
            if (sceneName.Equals("GameStart"))
            {
                SoundManager.Instance?.PlayBgm(SoundType.SoundMainBGM);
            }

            // 隐藏面板
            UIFramework.UIManager.HidePanel<LoadingPanelNew>();
            Global.Publisher.Publish(MessagePublisher.SKILL_COOLING_INITIALIZED, null);
            
        }

        private void OnLoadFailed(string error)
        {
            LogTool.LogError($"[LoadingPanelNew] 场景加载失败: {error}");

            // 可以在这里显示错误提示
            // TODO: 显示错误弹窗
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // 面板打开时的逻辑
            // 如果有传入场景名，自动开始加载
            if (uiData is LoadingData data)
            {
                StartLoad(data.TargetScene);
            }
        }

        protected override void OnShow()
        {
            // 面板显示时的逻辑
        }

        protected override void OnHide()
        {
            // 面板隐藏时的逻辑
        }
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    public class LoadingData : IUIData
    {
        public string TargetScene { get; set; }
    }
}
