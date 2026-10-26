using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using YooAsset;

namespace UIFramework
{
    /// <summary>
    /// 负责 UI 图集预加载与引用计数管理。
    /// AtlasKey 为短名，加载地址为 SpriteAtlas_{Key}；依赖清单见 UIAtlasPreloadTable。
    /// </summary>
    public sealed class AtlasManager
    {
        private static readonly AtlasManager sInstance = new AtlasManager();
        public static AtlasManager Instance => sInstance;

        /// <summary>
        /// 全局开关：是否启用“打开界面前自动预加载图集”的功能。
        /// 关闭后不会影响 Unity 的 atlasRequested 懒加载回调，只是跳过面板打开前的预加载步骤。
        /// </summary>
        public static bool EnableAutoPreload { get; set; } = true;

        private readonly Dictionary<string, SpriteAtlas> mLoadedAtlas = new Dictionary<string, SpriteAtlas>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> mRefCount = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> mPinnedAtlas = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> mLoading = new HashSet<string>();
        private readonly Dictionary<string, AssetHandle> mLoadedAtlasHandles = new Dictionary<string, AssetHandle>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<FieldInfo>> mGlobalAtlasFieldsByTag = new Dictionary<string, List<FieldInfo>>(StringComparer.Ordinal);
        private bool mGlobalAtlasFieldMapInitialized;
        private bool mCommonAtlasesPreloaded;
        private bool mUnityAtlasListenerRegistered;

        private AtlasManager()
        {
            EnsureUnityAtlasRequestListener();
        }

        public async UniTask<bool> PreloadForPanelAsync(Type panelType)
        {
            if (!EnableAutoPreload)
            {
                return true;
            }

            if (!UIAtlasPreloadTable.TryGet(panelType, out var atlasKeys))
            {
                LogTool.LogWarning($"[AtlasManager] 未找到面板图集配置: {panelType?.FullName}");
                return true;
            }

            return await PreloadAsync(atlasKeys);
        }

        public bool PreloadForPanelSync(Type panelType)
        {
            if (!EnableAutoPreload)
            {
                return true;
            }

            if (!UIAtlasPreloadTable.TryGet(panelType, out var atlasKeys))
            {
                LogTool.LogWarning($"[AtlasManager] 未找到面板图集配置: {panelType?.FullName}");
                return true;
            }

            return PreloadSync(atlasKeys);
        }

        public async UniTask<bool> PreloadForPanelAsync(string panelKey)
        {
            if (!EnableAutoPreload)
            {
                return true;
            }

            if (!UIAtlasPreloadTable.TryGet(panelKey, out var atlasKeys))
            {
                LogTool.LogWarning($"[AtlasManager] 未找到面板图集配置: {panelKey}");
                return true;
            }

            return await PreloadAsync(atlasKeys);
        }

        public async UniTask<bool> PreloadAsync(IReadOnlyList<string> atlasKeys)
        {
            EnsureUnityAtlasRequestListener();

            if (atlasKeys == null || atlasKeys.Count == 0)
            {
                LogTool.Log("[AtlasManager] PreloadAsync atlasKeys 为空，跳过预加载");
                return true;
            }

            LogTool.Log($"[AtlasManager] 开始预加载图集，数量: {atlasKeys.Count}，keys: {string.Join(",", atlasKeys)}");

            for (int i = 0; i < atlasKeys.Count; i++)
            {
                var atlasKey = atlasKeys[i];
                if (string.IsNullOrWhiteSpace(atlasKey))
                {
                    continue;
                }

                if (mLoadedAtlas.ContainsKey(atlasKey))
                {
                    IncreaseRef(atlasKey);
                    continue;
                }

                bool loaded = await LoadAtlasAsync(atlasKey);
                if (!loaded)
                {
                    LogTool.LogError($"[AtlasManager] 图集加载失败: {atlasKey}");
                    return false;
                }
            }

            return true;
        }

        public async UniTask<bool> PreloadCommonAtlasesAsync()
        {
            if (mCommonAtlasesPreloaded)
            {
                LogTool.Log("[AtlasManager] 通用图集已预加载，跳过");
                return true;
            }

            var commonKeys = UIAtlasPreloadTable.CommonAtlasKeys;
            LogTool.Log($"[AtlasManager] 准备预加载 CommonAtlasKeys，数量: {commonKeys.Length}");
            bool result = await PreloadAsync(commonKeys);
            if (result)
            {
                for (int i = 0; i < commonKeys.Length; i++)
                {
                    var key = commonKeys[i];
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        mPinnedAtlas.Add(key);
                    }
                }

                mCommonAtlasesPreloaded = true;
                LogTool.Log("[AtlasManager] 通用图集预加载完成");
            }
            else
            {
                LogTool.LogError("[AtlasManager] 通用图集预加载失败");
            }

            return result;
        }

        public void ReleaseForPanel(Type panelType)
        {
            if (!UIAtlasPreloadTable.TryGet(panelType, out var atlasKeys))
            {
                return;
            }

            Release(atlasKeys);
        }

        public void ReleaseForPanel(string panelKey)
        {
            if (!UIAtlasPreloadTable.TryGet(panelKey, out var atlasKeys))
            {
                return;
            }

            Release(atlasKeys);
        }

        public void Release(IReadOnlyList<string> atlasKeys)
        {
            if (atlasKeys == null || atlasKeys.Count == 0)
            {
                return;
            }

            for (int i = 0; i < atlasKeys.Count; i++)
            {
                var atlasKey = atlasKeys[i];
                if (string.IsNullOrWhiteSpace(atlasKey))
                {
                    continue;
                }

                if (mPinnedAtlas.Contains(atlasKey) || UIAtlasPreloadTable.IsCommon(atlasKey))
                {
                    continue;
                }

                if (!mRefCount.TryGetValue(atlasKey, out int count))
                {
                    continue;
                }

                count--;
                if (count > 0)
                {
                    mRefCount[atlasKey] = count;
                    continue;
                }

                mRefCount.Remove(atlasKey);
                mLoadedAtlas.Remove(atlasKey);
                UpdateGlobalAtlasTargetFields(atlasKey, null);
                ReleaseAtlasHandleIfNeeded(atlasKey);
                LogTool.Log($"[AtlasManager] 释放图集: {atlasKey}");
            }
        }

        public bool PreloadSync(IReadOnlyList<string> atlasKeys)
        {
            EnsureUnityAtlasRequestListener();

            if (atlasKeys == null || atlasKeys.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < atlasKeys.Count; i++)
            {
                var atlasKey = atlasKeys[i];
                if (string.IsNullOrWhiteSpace(atlasKey))
                {
                    continue;
                }

                if (mLoadedAtlas.ContainsKey(atlasKey))
                {
                    IncreaseRef(atlasKey);
                    continue;
                }

                if (!LoadAtlasSync(atlasKey))
                {
                    LogTool.LogError($"[AtlasManager] 同步加载图集失败: {atlasKey}");
                    return false;
                }
            }

            return true;
        }

        public SpriteAtlas GetAtlas(string atlasKey)
        {
            if (string.IsNullOrWhiteSpace(atlasKey))
            {
                return null;
            }

            mLoadedAtlas.TryGetValue(atlasKey, out var atlas);
            return atlas;
        }

        public bool TryGetSprite(string atlasKey, string spriteName, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(atlasKey) || string.IsNullOrWhiteSpace(spriteName))
            {
                return false;
            }

            var atlas = GetAtlas(atlasKey);
            if (atlas == null)
            {
                return false;
            }

            sprite = atlas.GetSprite(spriteName);
            return sprite != null;
        }

        /// <summary>调试快照：当前已加载图集的（短名, 引用计数, 是否常驻pin），供编辑器工具/排查用。</summary>
        public List<(string key, int refCount, bool pinned)> GetDebugSnapshot()
        {
            var list = new List<(string, int, bool)>(mLoadedAtlas.Count);
            foreach (var kv in mLoadedAtlas)
            {
                mRefCount.TryGetValue(kv.Key, out int count);
                list.Add((kv.Key, count, mPinnedAtlas.Contains(kv.Key)));
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
            return list;
        }

        /// <summary>
        /// 释放战斗图集（退出战斗场景时由 BattleSceneLifecycle 调用），
        /// 与 ScenePreloadBattleAssets 的预载一一对应；引用计数未归零的（面板仍持有）不会卸载。
        /// </summary>
        public void ReleaseBattleAtlases()
        {
            Release(UIAtlasPreloadTable.BattleAtlasKeys);
            LogTool.Log("[AtlasManager] 战斗图集已释放");
        }

        private void EnsureUnityAtlasRequestListener()
        {
            if (mUnityAtlasListenerRegistered)
            {
                return;
            }

            SpriteAtlasManager.atlasRequested += OnAtlasRequested;
            SpriteAtlasManager.atlasRegistered += OnAtlasRegistered;
            mUnityAtlasListenerRegistered = true;
            LogTool.Log("[AtlasManager] atlasRequested/atlasRegistered 监听已注册");
        }

        private void OnAtlasRequested(string atlasTag, Action<SpriteAtlas> callback)
        {
            if (string.IsNullOrWhiteSpace(atlasTag))
            {
                if (callback != null)
                {
                    callback.Invoke(null);
                }

                LogTool.LogWarning("[AtlasManager] atlasRequested 收到空 atlasTag");
                return;
            }

            if (mLoadedAtlas.TryGetValue(atlasTag, out var cachedAtlas) && cachedAtlas != null)
            {
                if (callback != null)
                {
                    callback.Invoke(cachedAtlas);
                }

                UpdateGlobalAtlasTargetFields(atlasTag, cachedAtlas);
                return;
            }

#if UNITY_WEBGL
            // WebGL 不支持同步加载，atlasRequested 允许延迟回调：异步加载完成后再回传图集
            LoadAtlasAsync(atlasTag).ContinueWith(success =>
            {
                if (callback != null)
                {
                    callback.Invoke(success ? GetAtlas(atlasTag) : null);
                }

                if (!success)
                {
                    LogTool.LogError($"[AtlasManager] atlasRequested 加载失败: {atlasTag}");
                }
            }).Forget();
            return;
#else
            if (LoadAtlasSync(atlasTag))
            {
                var atlas = GetAtlas(atlasTag);
                if (callback != null)
                {
                    callback.Invoke(atlas);
                }

                return;
            }

            if (callback != null)
            {
                callback.Invoke(null);
            }

            LogTool.LogError($"[AtlasManager] atlasRequested 加载失败: {atlasTag}");
#endif
        }

        private void OnAtlasRegistered(SpriteAtlas atlas)
        {
            var atlasName = atlas != null ? atlas.name : "null";
            LogTool.Log($"[AtlasManager] atlasRegistered: {atlasName}");
        }

        private async UniTask<bool> LoadAtlasAsync(string atlasKey)
        {
            if (mLoadedAtlas.ContainsKey(atlasKey))
            {
                IncreaseRef(atlasKey);
                return true;
            }

            while (mLoading.Contains(atlasKey))
            {
                await UniTask.Yield();
                if (mLoadedAtlas.ContainsKey(atlasKey))
                {
                    IncreaseRef(atlasKey);
                    return true;
                }
            }

            mLoading.Add(atlasKey);
            try
            {
                var atlasAddress = "SpriteAtlas_" + atlasKey;
                var handle = YooAssets.LoadAssetAsync<SpriteAtlas>(atlasAddress);
                if (!handle.IsDone)
                {
                    var tcs = new UniTaskCompletionSource();
                    handle.Completed += _ => tcs.TrySetResult();
                    await tcs.Task;
                }

                if (handle.Status != EOperationStatus.Succeed)
                {
                    LogTool.LogError($"[AtlasManager] YooAssets 异步加载失败: {atlasAddress}, error: {handle.LastError}");
                    handle.Release();
                    return false;
                }

                var loadedAtlas = handle.AssetObject as SpriteAtlas;
                if (loadedAtlas == null)
                {
                    LogTool.LogError($"[AtlasManager] LoadAssetAsync 返回空图集: {atlasAddress}");
                    handle.Release();
                    return false;
                }

                mLoadedAtlas[atlasKey] = loadedAtlas;
                mLoadedAtlasHandles[atlasKey] = handle;
                mRefCount[atlasKey] = 1;
                UpdateGlobalAtlasTargetFields(atlasKey, loadedAtlas);
                LogTool.Log($"[AtlasManager] 加载图集成功: {atlasKey} ({atlasAddress})");
                return true;
            }
            catch (Exception ex)
            {
                LogTool.LogException(ex);
                return false;
            }
            finally
            {
                mLoading.Remove(atlasKey);
            }
        }

        private bool LoadAtlasSync(string atlasKey)
        {
            if (mLoadedAtlas.ContainsKey(atlasKey))
            {
                IncreaseRef(atlasKey);
                return true;
            }

#if UNITY_WEBGL
            // WebGL 不支持同步加载：只认已异步加载完成的缓存。
            // 严禁走 YooAssets.LoadAssetSync，它会把同一 bundle 上进行中的异步加载一并置为失败。
            LogTool.LogError($"[AtlasManager] WebGL 不支持同步加载图集: {atlasKey}，请提前用 PreloadAsync 预加载");
            return false;
#else
            var atlasAddress = "SpriteAtlas_" + atlasKey;

            try
            {
                var handle = YooAssets.LoadAssetSync<SpriteAtlas>(atlasAddress);
                if (handle.Status != EOperationStatus.Succeed)
                {
                    LogTool.LogError($"[AtlasManager] YooAssets 同步加载失败: {atlasAddress}, error: {handle.LastError}");
                    handle.Release();
                    return false;
                }

                var atlas = handle.AssetObject as SpriteAtlas;
                if (atlas == null)
                {
                    LogTool.LogError($"[AtlasManager] YooAssets 同步加载返回空 SpriteAtlas: {atlasAddress}");
                    handle.Release();
                    return false;
                }

                mLoadedAtlas[atlasKey] = atlas;
                mLoadedAtlasHandles[atlasKey] = handle;
                mRefCount[atlasKey] = 1;
                UpdateGlobalAtlasTargetFields(atlasKey, atlas);
                LogTool.Log($"[AtlasManager] 同步加载图集成功: {atlasKey} ({atlasAddress})");
                return true;
            }
            catch (Exception ex)
            {
                LogTool.LogException(ex);
                return false;
            }
#endif
        }

        private void ReleaseAtlasHandleIfNeeded(string atlasKey)
        {
            if (!mLoadedAtlasHandles.TryGetValue(atlasKey, out var handle) || handle == null)
            {
                return;
            }

            mLoadedAtlasHandles.Remove(atlasKey);
            handle.Release();

            // handle.Release 只减引用计数；尝试卸载底层 bundle，图集纹理随之真正释放。
            // 仍被引用的资源 YooAsset 不会卸载，TryUnloadUnusedAsset 是安全操作
            var package = YooAssets.GetPackage("DefaultPackage");
            if (package != null)
            {
                package.TryUnloadUnusedAsset("SpriteAtlas_" + atlasKey);
            }
        }

        private void IncreaseRef(string atlasKey)
        {
            if (mRefCount.TryGetValue(atlasKey, out int count))
            {
                mRefCount[atlasKey] = count + 1;
            }
            else
            {
                mRefCount[atlasKey] = 1;
            }
        }

        /// <summary>全局图集字段写回(Global.*SpriteAtlas)后触发,供业务重注册依赖图集实例的映射</summary>
        public static Action OnGlobalAtlasFieldsUpdated;

        private void UpdateGlobalAtlasTargetFields(string atlasTag, SpriteAtlas atlas)
        {
            if (string.IsNullOrWhiteSpace(atlasTag))
            {
                return;
            }

            EnsureGlobalAtlasFieldMap();
            if (!mGlobalAtlasFieldsByTag.TryGetValue(atlasTag, out var fields) || fields == null || fields.Count == 0)
            {
                return;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                fields[i].SetValue(null, atlas);
            }

            OnGlobalAtlasFieldsUpdated?.Invoke();
        }

        private void EnsureGlobalAtlasFieldMap()
        {
            if (mGlobalAtlasFieldMapInitialized)
            {
                return;
            }

            mGlobalAtlasFieldMapInitialized = true;
            mGlobalAtlasFieldsByTag.Clear();

            var globalType = typeof(FusionContinent.Global);
            var fields = globalType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.FieldType != typeof(SpriteAtlas))
                {
                    continue;
                }

                var attr = field.GetCustomAttribute<SpriteAtlasTargetAttribute>();
                if (attr == null || string.IsNullOrWhiteSpace(attr.Name))
                {
                    continue;
                }

                if (!mGlobalAtlasFieldsByTag.TryGetValue(attr.Name, out var mappedFields))
                {
                    mappedFields = new List<FieldInfo>();
                    mGlobalAtlasFieldsByTag[attr.Name] = mappedFields;
                }

                mappedFields.Add(field);
            }
        }
    }
}
