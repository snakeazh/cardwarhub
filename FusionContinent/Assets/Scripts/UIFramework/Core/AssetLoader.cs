using System;
using System.Collections;
using UnityEngine;
using YooAsset;

namespace UIFramework
{
    /// <summary>
    /// 资源加载接口
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// 初始化加载器
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 加载资源（异步）
        /// </summary>
        void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : UnityEngine.Object;
        
        /// <summary>
        /// 加载资源（协程方式）
        /// </summary>
        IEnumerator LoadAssetCoroutine<T>(string path, Action<T> onComplete) where T : UnityEngine.Object;
        
        /// <summary>
        /// 释放资源
        /// </summary>
        void ReleaseAsset(string path);
        
        /// <summary>
        /// 加载场景（异步）
        /// </summary>
        void LoadSceneAsync(string sceneName, Action onComplete = null);
    }
    
    /// <summary>
    /// Resources 加载器（默认）
    /// </summary>
    public class ResourcesAssetLoader : IAssetLoader
    {
        public void Initialize()
        {
            // Resources 不需要初始化
        }
        
        public void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : UnityEngine.Object
        {
            UIRoot.Instance.StartCoroutine(LoadAssetCoroutine(path, onComplete));
        }
        
        public IEnumerator LoadAssetCoroutine<T>(string path, Action<T> onComplete) where T : UnityEngine.Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            yield return request;
            
            T asset = request.asset as T;
            onComplete?.Invoke(asset);
        }
        
        public void ReleaseAsset(string path)
        {
            // Resources 不需要手动释放
        }
        
        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            UIRoot.Instance.StartCoroutine(LoadSceneCoroutine(sceneName, onComplete));
        }
        
        private IEnumerator LoadSceneCoroutine(string sceneName, Action onComplete)
        {
            var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            yield return operation;
            onComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// YooAssets 加载器
    /// 使用方式：
    /// UIManager.AssetLoader = new YooAssetsLoader("DefaultPackage");
    /// </summary>
    public class YooAssetsLoader : IAssetLoader
    {
        private readonly string mPackageName;
        
        public YooAssetsLoader(string packageName = "DefaultPackage")
        {
            mPackageName = packageName;
        }
        
        public void Initialize()
        {
            // YooAssets 初始化应在游戏启动时完成
            // 这里可以检查是否已经初始化
        }
        
        public void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : UnityEngine.Object
        {
            var package = YooAssets.GetPackage(mPackageName);
            if (package == null)
            {
                LogTool.LogError($"[YooAssetsLoader] 找不到Package: {mPackageName}");
                onComplete?.Invoke(null);
                return;
            }
            
            var handle = package.LoadAssetAsync<T>(path);
            handle.Completed += (assetHandle) =>
            {
                if (assetHandle.Status == EOperationStatus.Succeed)
                {
                    onComplete?.Invoke(assetHandle.AssetObject as T);
                }
                else
                {
                    LogTool.LogError($"[YooAssetsLoader] 加载失败: {path}, 错误: {assetHandle.LastError}");
                    onComplete?.Invoke(null);
                }
            };
        }
        
        public IEnumerator LoadAssetCoroutine<T>(string path, Action<T> onComplete) where T : UnityEngine.Object
        {
            var package = YooAssets.GetPackage(mPackageName);
            if (package == null)
            {
                LogTool.LogError($"[YooAssetsLoader] 找不到Package: {mPackageName}");
                onComplete?.Invoke(null);
                yield break;
            }
            
            var handle = package.LoadAssetAsync<T>(path);
            yield return handle;
            
            if (handle.Status == EOperationStatus.Succeed)
            {
                onComplete?.Invoke(handle.AssetObject as T);
            }
            else
            {
                LogTool.LogError($"[YooAssetsLoader] 加载失败: {path}");
                onComplete?.Invoke(null);
            }
        }
        
        public void ReleaseAsset(string path)
        {
            var package = YooAssets.GetPackage(mPackageName);
            package?.TryUnloadUnusedAsset(path);
        }
        
        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            var package = YooAssets.GetPackage(mPackageName);
            var handle = package.LoadSceneAsync(sceneName);
            handle.Completed += (sceneHandle) =>
            {
                onComplete?.Invoke();
            };
        }
    }
    
    /// <summary>
    /// 预制体路径提供器
    /// </summary>
    public interface IPrefabPathProvider
    {
        /// <summary>
        /// 获取预制体路径
        /// </summary>
        string GetPrefabPath(Type panelType);
    }
    
    /// <summary>
    /// 默认预制体路径提供器
    /// </summary>
    public class DefaultPrefabPathProvider : IPrefabPathProvider
    {
        private readonly string mBasePath;
        
        public DefaultPrefabPathProvider(string basePath = "UI/Prefabs")
        {
            mBasePath = basePath;
        }
        
        public string GetPrefabPath(Type panelType)
        {
            return $"{panelType.Name}";
        }
    }
}
