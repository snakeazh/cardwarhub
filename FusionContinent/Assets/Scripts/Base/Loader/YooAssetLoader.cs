using UnityEngine;
using YooAsset;

namespace FusionContinent.Base
{
    using System.Collections.Generic;
    using System.Collections;

    public class YooAssetLoader
    {
        private readonly ResourcePackage resourcePackage;
        private readonly Dictionary<string, AssetHandle> mHandles = new Dictionary<string, AssetHandle>();

        public YooAssetLoader(ResourcePackage resourcePackage)
        {
            this.resourcePackage = resourcePackage;
        }

        public T LoadSync<T>(string assetName) where T : Object
        {
#if UNITY_WEBGL
            // WebGL 不支持同步加载：只能读已异步完成的缓存 handle，否则 fail fast 并给出资源名
            AssetHandle cached;
            if (mHandles.TryGetValue(assetName, out cached) && cached.IsDone)
            {
                return cached.AssetObject as T;
            }
            LogTool.LogError($"[YooAssetLoader] WebGL 不支持同步加载: {assetName}，请提前用 LoadAsync 预载");
            return null;
#else
            AssetHandle handle;
            if (!mHandles.TryGetValue(assetName, out handle))
            {
                handle = this.resourcePackage.LoadAssetSync<T>(assetName);
                mHandles[assetName] = handle;
            }
            return handle.AssetObject as T;
#endif
        }

        /// <summary>仅读取已加载完成的缓存，不触发任何加载。</summary>
        public bool TryGetLoaded<T>(string assetName, out T asset) where T : Object
        {
            asset = null;
            AssetHandle handle;
            if (mHandles.TryGetValue(assetName, out handle) && handle.IsDone && handle.AssetObject != null)
            {
                asset = handle.AssetObject as T;
                return asset != null;
            }
            return false;
        }

        public bool CheckLocationValid(string assetName)
        {
            return resourcePackage.CheckLocationValid(assetName);
        }

        public T TryLoadSync<T>(string assetName) where T : Object
        {
            if (!CheckLocationValid(assetName))
            {
                return null;
            }

            return LoadSync<T>(assetName);
        }

        public IEnumerator LoadAsync<T>(string assetName, System.Action<T> onLoaded) where T : Object
        {
            var handle = this.resourcePackage.LoadAssetAsync<T>(assetName);
            yield return handle;
            mHandles[assetName] = handle;
            onLoaded?.Invoke(handle.AssetObject as T);
        }

        /// <summary>LoadAsync 的 UniTask 版本：加载完成写入同一缓存，供 WebGL 同步读命中。</summary>
        public async Cysharp.Threading.Tasks.UniTask<T> LoadAssetUniTask<T>(string assetName) where T : Object
        {
            var handle = this.resourcePackage.LoadAssetAsync<T>(assetName);
            await handle.Task;
            mHandles[assetName] = handle;
            return handle.AssetObject as T;
        }

        public bool Release(string assetName)
        {
            AssetHandle handle;
            if (mHandles.TryGetValue(assetName, out handle))
            {
                handle.Release();
                mHandles.Remove(assetName);
                return true;
            }
            return false;
        }

        public void ClearAll()
        {
            foreach (var kv in mHandles)
            {
                kv.Value.Release();
            }
            mHandles.Clear();
        }

        public IEnumerator UnloadUnusedAssetsAsync()
        {
            yield return Resources.UnloadUnusedAssets();
        }
    }
}
