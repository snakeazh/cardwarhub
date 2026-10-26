using UIFramework;
using UnityEngine;
using UIFramework.Services;
using Cysharp.Threading.Tasks;

namespace Game.Loading
{
    /// <summary>
    /// 加载界面 ViewModel
    /// </summary>
    public class LoadingPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<float> CurrentProgress { get; } = new BindableProperty<float>(0f);
        public BindableProperty<string> ProgressText { get; } = new BindableProperty<string>("0%");
        public BindableProperty<string> TipText { get; } = new BindableProperty<string>("");
        public BindableProperty<string> TipText1 { get; } = new BindableProperty<string>("");
        public BindableProperty<string> TipText2 { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> IsLoading { get; } = new BindableProperty<bool>(false);
        #endregion

        #region 命令
        public Command<string> OnStartLoadCommand { get; private set; }
        public Command OnCancelLoadCommand { get; private set; }
        #endregion

        #region 依赖
        private LoadingService mLoadingService;
        #endregion

        #region 配置
        private readonly string[] _defaultTipList =
        {
            "提示：合理搭配英雄阵容才能取胜！",
            "提示：敌人越强，掉落也越丰厚！",
            "提示：升级技能能显著提升战力！",
            "提示：使用闪避可以避免致命伤害！"
        };
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 获取Service
            mLoadingService = GameServiceManager.Get<LoadingService>();

            // 初始化命令
            OnStartLoadCommand = new Command<string>(OnStartLoad);
            OnCancelLoadCommand = new Command(OnCancelLoad);

            // 订阅Service事件
            mLoadingService.OnProgressChanged += OnProgressChanged;
            mLoadingService.OnLoadComplete += OnLoadComplete;
            mLoadingService.OnLoadError += OnLoadError;

            LogTool.Log("[LoadingPanelViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            // 取消订阅Service事件
            if (mLoadingService != null)
            {
                mLoadingService.OnProgressChanged -= OnProgressChanged;
                mLoadingService.OnLoadComplete -= OnLoadComplete;
                mLoadingService.OnLoadError -= OnLoadError;
            }

            LogTool.Log("[LoadingPanelViewModel] 销毁");
        }

        #region 命令实现

        private async void OnStartLoad(string targetScene)
        {
            if (IsLoading.Value)
            {
                LogTool.LogWarning("[LoadingPanelViewModel] 正在加载中，请勿重复调用");
                return;
            }

            IsLoading.Value = true;
            CurrentProgress.Value = 0f;
            ProgressText.Value = "0%";

            // 随机选择提示文本
            TipText.Value = _defaultTipList[Random.Range(0, _defaultTipList.Length)];

            // 从配置表获取加载提示
            var loadingConfig = mLoadingService?.GetLoadingConfig();
            if (loadingConfig != null)
            {
                TipText1.Value = loadingConfig.TipText1;
                TipText2.Value = loadingConfig.TipText2;
            }

            LogTool.Log($"[LoadingPanelViewModel] 开始加载场景: {targetScene}");

            // 调用Service开始加载
            var result = await mLoadingService.LoadSceneAsync(targetScene);

            if (result)
            {
                LogTool.Log($"[LoadingPanelViewModel] 场景加载成功: {targetScene}");
            }
            else
            {
                LogTool.LogError($"[LoadingPanelViewModel] 场景加载失败: {targetScene}");
                IsLoading.Value = false;
            }
        }

        private void OnCancelLoad()
        {
            mLoadingService?.CancelLoad();
            IsLoading.Value = false;
            LogTool.Log("[LoadingPanelViewModel] 取消加载");
        }

        #endregion

        #region Service事件处理

        private void OnProgressChanged(float progress)
        {
            CurrentProgress.Value = progress;
            ProgressText.Value = (progress * 100f).ToString("F1") + "%";
        }

        private void OnLoadComplete(string sceneName)
        {
            IsLoading.Value = false;
            OnLoadCompleted?.Invoke(sceneName);
        }

        private void OnLoadError(string error)
        {
            IsLoading.Value = false;
            OnLoadFailed?.Invoke(error);
        }

        #endregion

        #region 公共方法

        public void ResetProgress()
        {
            CurrentProgress.Value = 0f;
            ProgressText.Value = "0%";
            IsLoading.Value = false;
        }

        #endregion

        #region 事件

        public System.Action<string> OnLoadCompleted;
        public System.Action<string> OnLoadFailed;

        #endregion
    }
}
