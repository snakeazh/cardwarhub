using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FusionContinent;

#if HYBRIDCLR && !UNITY_WEBGL
using HybridCLR;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

namespace QFramework
{
    using System.Collections;

    public class ResourceInitContext
    {
        public HotUpdateUI UpdateUI;
        public AppConfigData cfgData;
    }

    public class ResourceInitStep : IPipelineStep
    {
        private const string DefaultPackageName = "DefaultPackage";

        private readonly IAssetProvider mProvider;
        private readonly string mName;
        private EPlayMode mPlayMode;
        private ResourceInitContext ctx;

        private ResourcePackage CurrentResource;
        private string packageVersion;
        private bool updateError;
        private bool mStepSucceeded;
        private bool mPackagePrepared;
        private bool mHostPhaseSucceeded;
        private bool mDownloadConfirmed;
        private string mLastErrorMessage;

        private enum DialogResult
        {
            Pending,
            Confirmed,
            Cancelled
        }

        private DialogResult mDialogResult;

        public ResourceInitStep(EPlayMode playMode, ResourceInitContext context, IAssetProvider provider,
            string name = "ResourceInit")
        {
            mProvider = provider;
            mName = name;
            mPlayMode = playMode;
            ctx = context;
        }

        public string Name => mName;

        public IEnumerator Execute()
        {
            YooAssets.Initialize();
            CurrentResource = YooAssets.CreatePackage(DefaultPackageName);

            while (true)
            {
                ctx.UpdateUI.SetMessage("正在初始化包管理器");
                ctx.UpdateUI.SetProgress(0);
                updateError = false;

                yield return EnsureChannelConfig();
                if (mDialogResult == DialogResult.Cancelled)
                    yield break;

                ApplyChannelNotice();

                yield return PrepareResourcePackage();
                if (!mPackagePrepared)
                {
                    yield return AskRetryAndResetPackage();
                    if (mDialogResult != DialogResult.Confirmed)
                        yield break;
                    continue;
                }

                if (mPlayMode == EPlayMode.HostPlayMode)
                {
                    yield return RunHostDownloadPhase();
                    if (mDialogResult == DialogResult.Cancelled)
                        yield break;
                    if (!mHostPhaseSucceeded)
                    {
                        yield return AskRetryAndResetPackage();
                        if (mDialogResult != DialogResult.Confirmed)
                            yield break;
                        continue;
                    }
                }

                break;
            }

            yield return FinalizeResourcesAndStartGame();
        }

        private IEnumerator FinalizeResourcesAndStartGame()
        {
            ctx.UpdateUI.SetMessage("正在校验");
            ctx.UpdateUI.SetProgress(0.5f);
            YooAssets.SetDefaultPackage(YooAssets.GetPackage(DefaultPackageName));
            var handle = YooAssets.LoadAssetAsync<ShaderVariantCollection>("FontShaderVariants");
            yield return handle;
            var svc = handle.AssetObject as ShaderVariantCollection;
            if (svc != null)
                svc.WarmUp();
            handle.Release();
            // yield return ResKit.InitAsync();
            ctx.UpdateUI.SetProgress(1f);

#if HYBRIDCLR && !UNITY_WEBGL
            yield return LoadDllsAndStartGame();
#else
            // 纯 AOT：HotFix 程序集已编译进主包，直接反射调用入口
            var hotFixAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "HotFix");
            if (hotFixAssembly == null)
            {
                Debug.LogError("[ResourceInitStep] 未找到 HotFix 程序集");
                yield break;
            }

            InvokeGameEntry(hotFixAssembly);
            Debug.Log("纯 AOT 启动完成！");
#endif
        }

        private void ApplyChannelNotice()
        {
            ctx.UpdateUI.ShowNotice();
            ctx.UpdateUI.SetAnnouncement(AotManager.HotFixData.UpdateNotice ?? string.Empty);
        }

        private IEnumerator PrepareResourcePackage()
        {
            mPackagePrepared = false;

            yield return InitPackageByPlayMode();
            if (!mStepSucceeded)
            {
                mLastErrorMessage = "资源包初始化失败，是否重试";
                yield break;
            }

            ctx.UpdateUI.SetMessage("正在请求资源版本号");
            yield return RequestPackageVersion();
            if (!mStepSucceeded || string.IsNullOrEmpty(packageVersion))
            {
                mLastErrorMessage = "获取资源版本失败，是否重试";
                yield break;
            }

            ctx.UpdateUI.SetMessage("正在请求资源清单");
            yield return UpdatePackageManifest();
            if (!mStepSucceeded)
            {
                mLastErrorMessage = "更新资源清单失败，是否重试";
                yield break;
            }

            mPackagePrepared = true;
        }

        private IEnumerator RunHostDownloadPhase()
        {
            mHostPhaseSucceeded = false;

            while (true)
            {
                updateError = false;
                ctx.UpdateUI.SetMessage("正在下载资源");
                yield return Download();
                if (mDialogResult == DialogResult.Cancelled)
                    yield break;

                if (!updateError)
                    break;

                yield return ShowDialogAndWait("下载失败，是否重试");
                if (mDialogResult != DialogResult.Confirmed)
                    yield break;

                yield return ResetAndRefreshHostPackage();
                if (!mPackagePrepared)
                    yield break;
            }

            if (mDialogResult == DialogResult.Cancelled || updateError)
                yield break;

            ctx.UpdateUI.SetMessage("正在请求资源清单");
            yield return UpdatePackageManifest();
            mHostPhaseSucceeded = mStepSucceeded;
            if (!mHostPhaseSucceeded)
                mLastErrorMessage = "更新资源清单失败，是否重试";
        }

        private IEnumerator ResetAndRefreshHostPackage()
        {
            mPackagePrepared = false;
            yield return ResetCurrentPackage();
            yield return HostInitPackage();
            if (!mStepSucceeded)
            {
                mLastErrorMessage = "资源包初始化失败，是否重试";
                yield break;
            }

            ctx.UpdateUI.SetMessage("正在请求资源版本号");
            yield return RequestPackageVersion();
            if (!mStepSucceeded || string.IsNullOrEmpty(packageVersion))
            {
                mLastErrorMessage = "获取资源版本失败，是否重试";
                yield break;
            }

            ctx.UpdateUI.SetMessage("正在请求资源清单");
            yield return UpdatePackageManifest();
            mPackagePrepared = mStepSucceeded;
            if (!mPackagePrepared)
                mLastErrorMessage = "更新资源清单失败，是否重试";
        }

        private IEnumerator AskRetryAndResetPackage()
        {
            yield return ShowDialogAndWait(mLastErrorMessage);
            if (mDialogResult != DialogResult.Confirmed)
                yield break;

            yield return ResetCurrentPackage();
        }

        /// <summary>
        /// 单机化：不再请求渠道接口，渠道信息改读本地配置（Resources/AotConfig 的 CdnUrl 作为热更地址）。
        /// 服务器列表已随登录页选服移除，恒为空。
        /// </summary>
        private IEnumerator EnsureChannelConfig()
        {
            AotManager.HotFixData = new HotFixData
            {
                ChannelKey = ctx.cfgData.Channel,
                HotfixCdn = NormalizeCdnUrl(ctx.cfgData.CdnUrl),
                HotfixFallbackCdn = NormalizeCdnUrl(ctx.cfgData.FallbackCdnUrl),
                UpdateNotice = string.Empty,
                Servers = new List<global::ServerInfo>(),
            };
            if (NeedsRemoteHotfixCdn() && string.IsNullOrEmpty(ctx.cfgData.CdnUrl))
            {
                Debug.LogError("[ResourceInitStep] Host/Web 模式需要在 AotConfig 配置 CdnUrl 作为热更地址");
            }
            yield break;
        }

        /// <summary>
        /// 规范化 CDN 地址：缺协议头补 http://，统一以 / 结尾；空串原样返回。
        /// </summary>
        private static string NormalizeCdnUrl(string cdnUrl)
        {
            if (string.IsNullOrEmpty(cdnUrl))
                return string.Empty;
            string url = cdnUrl.Contains("://") ? cdnUrl : $"http://{cdnUrl}";
            return url.TrimEnd('/') + "/";
        }

        private bool NeedsRemoteHotfixCdn()
        {
            return mPlayMode == EPlayMode.HostPlayMode || mPlayMode == EPlayMode.WebPlayMode;
        }

        private IEnumerator ShowDialogAndWait(string message)
        {
            mDialogResult = DialogResult.Pending;
            ctx.UpdateUI.ShowDialog(message,
                () => mDialogResult = DialogResult.Confirmed,
                () => mDialogResult = DialogResult.Cancelled);
            yield return new WaitUntil(() => mDialogResult != DialogResult.Pending);
        }

        private IEnumerator ResetCurrentPackage()
        {
            var packageName = CurrentResource.PackageName;
            yield return CurrentResource.DestroyAsync();
            YooAssets.RemovePackage(packageName);
            CurrentResource = YooAssets.CreatePackage(packageName);
        }

        private IEnumerator InitPackageByPlayMode()
        {
            if (mPlayMode == EPlayMode.EditorSimulateMode)
                yield return EditorInitPackage();
            else if (mPlayMode == EPlayMode.OfflinePlayMode)
                yield return InitPackage();
            else if (mPlayMode == EPlayMode.WebPlayMode)
                yield return InitWebPackage();
            else
                yield return HostInitPackage();
        }

        private static string GetPlatformFolder()
        {
#if UNITY_WEBGL
            return "WebGL";
#else
            if (Application.platform == RuntimePlatform.IPhonePlayer)
                return "iOS";
            return "Android";
#endif
        }

        private static string BuildHotfixCdnUrl()
        {
            return BuildHotfixCdnUrl(AotManager.HotFixData.HotfixCdn);
        }

        private static string BuildFallBackCdnUrl()
        {
            string fallbackCdn = AotManager.HotFixData.HotfixFallbackCdn;
            if (string.IsNullOrEmpty(fallbackCdn))
                fallbackCdn = AotManager.HotFixData.HotfixCdn; // 未配置备用 CDN 时与主地址一致
            return BuildHotfixCdnUrl(fallbackCdn);
        }

        private static string BuildHotfixCdnUrl(string cdnBase)
        {
            string url = $"{cdnBase}{GetPlatformFolder()}/v{Application.version}";

            return url;
        }
#if HYBRIDCLR && !UNITY_WEBGL
        private IEnumerator LoadDllsAndStartGame()
        {
            var package = CurrentResource;
            string[] aotMetaDlls = AOTGenericReferences.PatchedAOTAssemblyList.ToArray();

            foreach (var aotDll in aotMetaDlls)
            {
                var handle = package.LoadAssetAsync<TextAsset>($"{aotDll}");
                yield return handle;

                byte[] dllBytes = handle.GetAssetObject<TextAsset>().bytes;
                if (dllBytes != null)
                {
                    var err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
                    Debug.Log($"Load Metadata {aotDll}: {err}");
                    if (err != 0)
                        Debug.LogError($"加载 AOT 元数据失败，错误码: {err}");
                }

                handle.Release();
            }

            var hotfixHandle = package.LoadAssetAsync<TextAsset>("HotFix.dll");
            yield return hotfixHandle;

            byte[] hotfixBytes = hotfixHandle.GetAssetObject<TextAsset>().bytes;
            hotfixHandle.Release();

            Assembly hotFixAssembly = Assembly.Load(hotfixBytes);
            Debug.Log($"当前程序集名称: {hotFixAssembly.FullName}");
            InvokeGameEntry(hotFixAssembly);
            Debug.Log("HybridCLR + YooAsset 启动完成！");
        }
#endif

        private static void InvokeGameEntry(Assembly assembly)
        {
            Type entryType = assembly.GetType("HotFix.GameEntry");
            if (entryType == null)
            {
                Debug.LogError("[ResourceInitStep] 未找到 HotFix.GameEntry");
                return;
            }

            MethodInfo startGame = entryType.GetMethod("StartGame");
            if (startGame == null)
            {
                Debug.LogError("[ResourceInitStep] 未找到 HotFix.GameEntry.StartGame");
                return;
            }

            startGame.Invoke(null, null);
        }

        private IEnumerator InitWebPackage()
        {
            string cdnUrl = BuildHotfixCdnUrl();
            string fallbackUrl = BuildFallBackCdnUrl();
            IRemoteServices remoteServices = new RemoteServices(cdnUrl, fallbackUrl);
            var createParameters = new WebPlayModeParameters
            {
                WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(),
                WebRemoteFileSystemParameters =
                    FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices)
            };

            yield return RunPackageInitialization(CurrentResource.InitializeAsync(createParameters));
        }

        private IEnumerator InitPackage()
        {
            var createParameters = new OfflinePlayModeParameters
            {
                BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
            };

            yield return RunPackageInitialization(CurrentResource.InitializeAsync(createParameters));
        }

        private IEnumerator EditorInitPackage()
        {
#if UNITY_EDITOR
            var buildResult = EditorSimulateModeHelper.SimulateBuild(DefaultPackageName);
            var createParameters = new EditorSimulateModeParameters
            {
                EditorFileSystemParameters =
                    FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory)
            };

            yield return RunPackageInitialization(CurrentResource.InitializeAsync(createParameters));
#else
            mStepSucceeded = true;
            yield return null;
#endif
        }

        private IEnumerator HostInitPackage()
        {
            string cdnUrl = BuildHotfixCdnUrl();
            IRemoteServices remoteServices = new RemoteServices(cdnUrl, BuildFallBackCdnUrl(), "cardswar.game.com");
            var createParameters = new HostPlayModeParameters
            {
                BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices)
            };

            yield return RunPackageInitialization(CurrentResource.InitializeAsync(createParameters));
        }

        private IEnumerator RunPackageInitialization(InitializationOperation initOperation)
        {
            yield return initOperation;
            ApplyInitOperationResult(initOperation.Status, initOperation.Error);
        }

        private void ApplyInitOperationResult(EOperationStatus status, string error)
        {
            mStepSucceeded = status == EOperationStatus.Succeed;
            if (mStepSucceeded)
                Debug.Log("资源包初始化成功！");
            else
                Debug.LogError($"资源包初始化失败：{error}");
        }

        private IEnumerator Download()
        {
            const int downloadingMaxNum = 10;
            const int failedTryAgain = 3;
            var downloader = CurrentResource.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
            if (downloader.TotalDownloadCount == 0)
            {
                Debug.Log("没有要下载的资源");
                yield break;
            }

            long totalDownloadBytes = downloader.TotalDownloadBytes;
            yield return ConfirmDownloadEnvironment(totalDownloadBytes);
            if (!mDownloadConfirmed)
                yield break;

            ctx.UpdateUI.ResetUI();
            ctx.UpdateUI.SetMessage("正在下载资源");
            downloader.DownloadUpdateCallback = OnDownloadUpdateFunction;
            downloader.BeginDownload();
            yield return downloader;

            updateError = downloader.Status != EOperationStatus.Succeed;
            if (updateError)
                Debug.LogError("资源下载失败");
            else
                Debug.Log("资源下载完成");
        }

        private IEnumerator ConfirmDownloadEnvironment(long totalDownloadBytes)
        {
            mDownloadConfirmed = false;
            NetworkReachability reachability = Application.internetReachability;
            if (reachability == NetworkReachability.NotReachable)
            {
                yield return ShowDialogAndWait("当前无网络链接");
                if (mDialogResult != DialogResult.Confirmed)
                    yield break;
                reachability = Application.internetReachability;
            }

            bool isWiFi = reachability == NetworkReachability.ReachableViaLocalAreaNetwork;
#if UNITY_ANDROID && !UNITY_EDITOR
            isWiFi = CheckAndroidWiFiState();
#endif

            Debug.Log(isWiFi ? "当前网络：WiFi" : "当前网络：移动数据/有线网络");
            if (!isWiFi)
            {
                yield return ShowDialogAndWait("当前没有连接wifi,是否继续下载？");
                if (mDialogResult != DialogResult.Confirmed)
                    yield break;
            }

            yield return ShowDialogAndWait($"需要下载资源{FormatFileSize(totalDownloadBytes)}，是否继续？");
            if (mDialogResult != DialogResult.Confirmed)
                yield break;

            mDownloadConfirmed = true;
        }

        private static bool CheckAndroidWiFiState()
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject connectivityManager =
                       activity.Call<AndroidJavaObject>("getSystemService", "connectivity"))
                {
                    using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        int sdkInt = buildVersion.GetStatic<int>("SDK_INT");

                        if (sdkInt >= 23)
                        {
                            AndroidJavaObject network = connectivityManager.Call<AndroidJavaObject>("getActiveNetwork");
                            if (network == null) return false;

                            AndroidJavaObject capabilities =
                                connectivityManager.Call<AndroidJavaObject>("getNetworkCapabilities", network);
                            if (capabilities == null) return false;

                            using (AndroidJavaClass nc = new AndroidJavaClass("android.net.NetworkCapabilities"))
                            {
                                int transportWifi = nc.GetStatic<int>("TRANSPORT_WIFI");
                                return capabilities.Call<bool>("hasTransport", transportWifi);
                            }
                        }

                        AndroidJavaObject networkInfo =
                            connectivityManager.Call<AndroidJavaObject>("getActiveNetworkInfo");
                        if (networkInfo == null) return false;

                        return networkInfo.Call<int>("getType") == 1;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Android WiFi检测异常：{e.Message}");
                return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
            }
        }

        private void OnDownloadUpdateFunction(DownloadUpdateData data)
        {
            ctx.UpdateUI.SetProgress(data.Progress);
            ctx.UpdateUI.SetDownloadProgress(data.CurrentDownloadBytes, data.TotalDownloadBytes);
        }

        private IEnumerator RequestPackageVersion()
        {
            mStepSucceeded = false;
            var operation = CurrentResource.RequestPackageVersionAsync();
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                packageVersion = operation.PackageVersion;
                PlayerPrefs.SetString("ResPackageVersion", packageVersion);
                mStepSucceeded = true;
                Debug.Log($"Request {CurrentResource.PackageName} package Version :  {packageVersion}");
            }
            else
            {
                Debug.LogError(operation.Error);
            }
        }

        private IEnumerator UpdatePackageManifest()
        {
            mStepSucceeded = false;
            if (string.IsNullOrEmpty(packageVersion))
            {
                Debug.LogError("[ResourceInitStep] packageVersion 为空，无法更新资源清单");
                yield break;
            }

            var operation = CurrentResource.UpdatePackageManifestAsync(packageVersion);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                mStepSucceeded = true;
                Debug.Log("[YooAsset] 更新资源清单成功");
            }
            else
            {
                Debug.LogError(operation.Error);
            }
        }

        public static string FormatFileSize(long bytes, int decimalPlaces = 2)
        {
            if (bytes < 0)
            {
                Debug.LogWarning("文件大小不能为负数，已自动转为0");
                bytes = 0;
            }

            double size = bytes;
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size.ToString($"F{decimalPlaces}")} {units[unitIndex]}";
        }
    }
}
