namespace FusionContinent.Base
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using YooAsset;

    /// <summary>
    /// 纯异步资源加载器：没有同步加载入口，资源必须先 await LoadAsync/PreloadAsync 完成后才能用 Get 取用。
    /// 未就绪就 Get 会直接抛异常（fail fast），WebGL 等不支持同步加载的平台统一走这个接口。
    /// </summary>
    public class AsyncResLoader
    {
        private readonly ResourcePackage resourcePackage;
        private readonly Dictionary<string, AssetHandle> mHandles = new Dictionary<string, AssetHandle>();

        public AsyncResLoader(ResourcePackage resourcePackage)
        {
            this.resourcePackage = resourcePackage;
        }

        /// <summary>资源是否已打进包（可加载）。</summary>
        public bool CheckLocationValid(string assetName)
        {
            return resourcePackage.CheckLocationValid(assetName);
        }

        /// <summary>资源已加载完成且可取用。</summary>
        public bool IsReady(string assetName)
        {
            AssetHandle handle;
            return mHandles.TryGetValue(assetName, out handle) && handle.IsDone && handle.AssetObject != null;
        }

        /// <summary>
        /// 异步加载资源。同名资源并发调用共享同一个 handle，重复 await 安全。
        /// 定位无效或加载失败会抛异常并清掉缓存（允许重试）。
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string assetName) where T : UnityEngine.Object
        {
            AssetHandle handle;
            if (!mHandles.TryGetValue(assetName, out handle))
            {
                if (!resourcePackage.CheckLocationValid(assetName))
                {
                    throw new Exception($"[AsyncResLoader] 资源定位无效（未打进包）: {assetName}");
                }
                handle = resourcePackage.LoadAssetAsync<T>(assetName);
                mHandles.Add(assetName, handle);
            }

            var task = handle.Task;
            if (task == null)
            {
                mHandles.Remove(assetName);
                throw new Exception($"[AsyncResLoader] 资源句柄已失效: {assetName}");
            }
            await task;

            if (handle.Status != EOperationStatus.Succeed || handle.AssetObject == null)
            {
                var error = handle.LastError;
                mHandles.Remove(assetName);
                handle.Release();
                throw new Exception($"[AsyncResLoader] 加载失败: {assetName}, {error}");
            }

            return (T)handle.AssetObject;
        }

        /// <summary>
        /// 批量异步预载（并行）。单个失败不中断整批，全部结束后若有失败则抛出含名单的异常。
        /// </summary>
        public async UniTask PreloadAsync<T>(IReadOnlyList<string> assetNames, Action<int, int> onProgress = null) where T : UnityEngine.Object
        {
            int total = assetNames.Count;
            int doneCount = 0;
            var failed = new List<string>();
            var tasks = new List<UniTask>(total);
            for (int i = 0; i < total; i++)
            {
                tasks.Add(LoadOne<T>(assetNames[i], failed, () =>
                {
                    doneCount++;
                    if (onProgress != null)
                    {
                        onProgress.Invoke(doneCount, total);
                    }
                }));
            }
            await UniTask.WhenAll(tasks);

            if (failed.Count > 0)
            {
                throw new Exception($"[AsyncResLoader] 预载失败 {failed.Count}/{total}: {string.Join(",", failed)}");
            }
        }

        private async UniTask LoadOne<T>(string assetName, List<string> failed, Action onDone) where T : UnityEngine.Object
        {
            try
            {
                await LoadAsync<T>(assetName);
                onDone.Invoke();
            }
            catch (Exception ex)
            {
                LogTool.LogException(ex);
                failed.Add(assetName);
            }
        }

        /// <summary>
        /// 取用已加载的资源。未就绪（未加载/加载中/已失败）直接抛异常——这就是"必须先等加载完才能用"的约束。
        /// </summary>
        public T Get<T>(string assetName) where T : UnityEngine.Object
        {
            AssetHandle handle;
            if (mHandles.TryGetValue(assetName, out handle) && handle.IsDone && handle.AssetObject != null)
            {
                return (T)handle.AssetObject;
            }
            throw new InvalidOperationException($"[AsyncResLoader] 资源未就绪: {assetName}，必须先 await LoadAsync/PreloadAsync 完成");
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
    }
}
