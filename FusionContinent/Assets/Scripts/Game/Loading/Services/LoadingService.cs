using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Config;
using System.Threading;
using FusionContinent;

namespace Game.Loading
{
    /// <summary>
    /// 加载服务 - 处理场景加载
    /// </summary>
    public class LoadingService : GameServiceBase
    {
        #region 属性
        public float CurrentProgress { get; private set; }
        public bool IsLoading { get; private set; }
        public string TargetScene { get; private set; }
        
        private float _smoothSpeed = 3f;
        private float _minLoadingTime = 2.0f;
        private CancellationTokenSource _cancellationTokenSource;
        #endregion

        #region 事件
        public event System.Action<float> OnProgressChanged;
        public event System.Action<string> OnLoadComplete;
        public event System.Action<string> OnLoadError;
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            LogTool.Log("[LoadingService] 初始化完成");
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        public async UniTask<bool> LoadSceneAsync(string targetScene)
        {
            if (IsLoading)
            {
                LogTool.LogWarning("[LoadingService] 正在加载中，请勿重复调用");
                return false;
            }

            IsLoading = true;
            TargetScene = targetScene;
            CurrentProgress = 0f;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // 1. 加载 LoadingScene
                var loadingOp = YooAssets.LoadSceneAsync("LoadingScene", LoadSceneMode.Single);
                await loadingOp.ToUniTask();

                // 2. 开始加载目标场景（Additive）
                float startTime = Time.time;
                var targetOp = YooAssets.LoadSceneAsync(targetScene, LoadSceneMode.Additive);

                // 等待加载完成
                bool timeEnough = false;
                while (!targetOp.IsDone || !timeEnough)
                {
                    // 检查是否取消
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        LogTool.Log("[LoadingService] 加载已取消");
                        return false;
                    }

                    // 检查是否加载失败
                    if (targetOp.Status == EOperationStatus.Failed)
                    {
                        LogTool.LogError($"[LoadingService] 场景加载失败: {targetOp.LastError}");
                        OnLoadError?.Invoke(targetOp.LastError);
                        return false;
                    }

                    // 进度计算：Unity 加载到 0.9 视为 100%
                    float targetProgress = Mathf.Clamp01(targetOp.Progress / 0.9f);
                    CurrentProgress = Mathf.Lerp(CurrentProgress, targetProgress, Time.deltaTime * _smoothSpeed);

                    OnProgressChanged?.Invoke(CurrentProgress);

                    timeEnough = Time.time - startTime >= _minLoadingTime;
                    await UniTask.Yield();
                }

                // 再次确认状态
                if (targetOp.Status != EOperationStatus.Succeed)
                {
                    LogTool.LogError("[LoadingService] 场景未成功加载");
                    OnLoadError?.Invoke("场景未成功加载");
                    return false;
                }

                // 3. 平滑过渡到 100%
                while (CurrentProgress < 1f)
                {
                    CurrentProgress = Mathf.Lerp(CurrentProgress, 1f, Time.deltaTime * 100f * _smoothSpeed);
                    OnProgressChanged?.Invoke(CurrentProgress);
                    await UniTask.Yield();
                }

                // 4. 激活场景
                targetOp.ActivateScene();
                await UniTask.WaitForEndOfFrame();

                // 设置活跃场景
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetScene));

                // 5. 卸载 LoadingScene
                await loadingOp.UnloadAsync().ToUniTask();
                loadingOp.Release();

                // 加载完成
                OnLoadComplete?.Invoke(targetScene);
                LogTool.Log($"[LoadingService] 场景加载完成: {targetScene}");
                return true;
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[LoadingService] 加载异常: {ex.Message}");
                OnLoadError?.Invoke(ex.Message);
                return false;
            }
            finally
            {
                IsLoading = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 取消加载
        /// </summary>
        public void CancelLoad()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                LogTool.Log("[LoadingService] 取消加载请求已发送");
            }
        }

        /// <summary>
        /// 获取加载配置
        /// </summary>
        public LoadingConfig GetLoadingConfig()
        {
            var configs = DataConfigManager.Tables?.TbLoadingConfig?.DataList;
            if (configs != null && configs.Count > 0)
            {
                return configs[Random.Range(0, configs.Count)];
            }
            return null;
        }

        /// <summary>
        /// 设置加载参数
        /// </summary>
        public void SetLoadingParams(float smoothSpeed, float minLoadingTime)
        {
            _smoothSpeed = smoothSpeed;
            _minLoadingTime = minLoadingTime;
        }
    }
}
