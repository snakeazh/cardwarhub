using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent;
using UnityEngine;
using YooAsset;

[Serializable]
public class PredefinedPoolConfig
{
    public string PoolName;
    public int MaxCount = 120;
    public int PrewarmCount = 0;
}

public enum PoolInitializationState
{
    None,
    Loading,
    Ready,
    Failed,
}

public abstract class BaseItemPoolManager : MonoBehaviour
{
    [SerializeField] private List<PredefinedPoolConfig> mPredefinedPoolConfigs = new();
    private readonly Dictionary<string, ItemGenericObjectPool> mPools = new();
    private readonly Dictionary<string, GameObject> mLoadedPrefabs = new();
    private readonly HashSet<string> mPredefinedPoolNameSet = new();
    private readonly Dictionary<string, PoolInitializationState> mPrefabStates = new();
    private readonly Dictionary<string, UniTask<GameObject>> mPrefabLoadingTasks = new();
    private readonly Dictionary<string, UniTask<ItemGenericObjectPool>> mPoolWarmupTasks = new();

    public IReadOnlyList<PredefinedPoolConfig> PredefinedPoolConfigs => mPredefinedPoolConfigs;

    public ItemGenericObjectPool CreateGameObjectPool(string poolKey, GameObject prefab, Transform parent = null, int maxCount = 120, bool worldPositionStays = false)
    {
        if (mPools.TryGetValue(poolKey, out var pool)) return pool;
        pool = new ItemGenericObjectPool(poolKey, prefab, parent != null ? parent : transform, maxCount, worldPositionStays);
        mPools[poolKey] = pool;
        return pool;
    }

    public void DestroyGameObjectPool(string poolKey)
    {
        if (!mPools.TryGetValue(poolKey, out var pool)) return;
        pool.DisposeAll();
        mPools.Remove(poolKey);
        mPoolWarmupTasks.Remove(poolKey);
    }

    public ItemGenericObjectPool GetGameObjectPool(string poolKey) => mPools.GetValueOrDefault(poolKey);
    public PoolInitializationState GetPrefabInitializationState(string poolKey) => mPrefabStates.GetValueOrDefault(poolKey, PoolInitializationState.None);
    public bool TryGetLoadedPrefab(string poolKey, out GameObject prefab) => mLoadedPrefabs.TryGetValue(poolKey, out prefab);

    public void AddPredefinedPool(string poolName, int maxCount = 120, int prewarmCount = 0)
    {
        if (string.IsNullOrEmpty(poolName) || ContainsPredefinedPool(poolName))
        {
            return;
        }

        mPredefinedPoolConfigs.Add(new PredefinedPoolConfig
        {
            PoolName = poolName,
            MaxCount = maxCount,
            PrewarmCount = prewarmCount,
        });
        RebuildPredefinedPoolNameSet();
    }

    public void AddPredefinedPools(IEnumerable<string> poolNames, int maxCount = 120, int prewarmCount = 0)
    {
        if (poolNames == null)
        {
            return;
        }

        bool changed = false;
        foreach (var poolName in poolNames)
        {
            if (string.IsNullOrEmpty(poolName) || ContainsPredefinedPool(poolName))
            {
                continue;
            }

            mPredefinedPoolConfigs.Add(new PredefinedPoolConfig
            {
                PoolName = poolName,
                MaxCount = maxCount,
                PrewarmCount = prewarmCount,
            });
            changed = true;
        }

        if (changed)
        {
            RebuildPredefinedPoolNameSet();
        }
    }

    public ItemGenericObjectPool GetGameObjectPoolOrCreate(string poolKey, Transform parent = null, int maxCount = 120, bool worldPositionStays = false)
    {
        if (mPools.TryGetValue(poolKey, out var pool)) return pool;
        var config = GetPredefinedPoolConfig(poolKey);
        if (config != null) maxCount = config.MaxCount;
        var prefab = GetOrLoadPrefabSync(poolKey);
        if (prefab == null)
        {
            mPrefabStates[poolKey] = PoolInitializationState.Failed;
            LogTool.LogError($"[{GetType().Name}] 资源加载失败: {poolKey}");
            return null;
        }
        return CreateGameObjectPool(poolKey, prefab, parent, maxCount, worldPositionStays);
    }

    public async UniTask<ItemGenericObjectPool> GetGameObjectPoolOrCreateAsync(string poolKey, Transform parent = null, int maxCount = 120, bool worldPositionStays = false)
    {
        if (mPools.TryGetValue(poolKey, out var pool)) return pool;
        var config = GetPredefinedPoolConfig(poolKey);
        if (config != null) maxCount = config.MaxCount;
        var prefab = await GetOrLoadPrefabAsync(poolKey);
        if (prefab == null)
        {
            mPrefabStates[poolKey] = PoolInitializationState.Failed;
            LogTool.LogError($"[{GetType().Name}] 异步资源加载失败: {poolKey}");
            return null;
        }
        return CreateGameObjectPool(poolKey, prefab, parent, maxCount, worldPositionStays);
    }

    public async UniTask PreloadPredefinedPrefabsAsync()
    {
        var tasks = new List<UniTask>();
        foreach (var config in mPredefinedPoolConfigs)
        {
            if (config == null || string.IsNullOrEmpty(config.PoolName)) continue;
            tasks.Add(GetOrLoadPrefabAsync(config.PoolName).AsUniTask());
        }
        if (tasks.Count > 0) await UniTask.WhenAll(tasks);
    }

    public async UniTask WarmupPredefinedPoolsAsync(Transform parent = null)
    {
        var tasks = new List<UniTask>();
        foreach (var config in mPredefinedPoolConfigs)
        {
            if (config == null || string.IsNullOrEmpty(config.PoolName) || config.PrewarmCount <= 0) continue;
            tasks.Add(WarmupPoolAsync(config.PoolName, config.PrewarmCount, parent, config.MaxCount).AsUniTask());
        }
        if (tasks.Count > 0) await UniTask.WhenAll(tasks);
    }

    public UniTask<ItemGenericObjectPool> WarmupPoolAsync(string poolKey, int prewarmCount, Transform parent = null, int maxCount = 120, bool worldPositionStays = false)
    {
        if (mPoolWarmupTasks.TryGetValue(poolKey, out var task)) return task;
        // Preserve：缓存中的任务会被多个调用方并发 await，原生 UniTask 不允许重复 await
        task = WarmupPoolInternalAsync(poolKey, prewarmCount, parent, maxCount, worldPositionStays).Preserve();
        mPoolWarmupTasks[poolKey] = task;
        return task;
    }

    public bool IsPredefinedPool(string poolKey) => mPredefinedPoolNameSet.Contains(poolKey);

    public PredefinedPoolConfig GetPredefinedPoolConfig(string poolKey)
    {
        for (int i = 0; i < mPredefinedPoolConfigs.Count; i++)
        {
            var config = mPredefinedPoolConfigs[i];
            if (config != null && config.PoolName == poolKey) return config;
        }
        return null;
    }

    public (ItemGenericObjectPool pool, GameObject instance) PlayGameObjectPool(GameObjectPoolData data)
    {
        var pool = GetGameObjectPoolOrCreate(data.PoolName, data.ParentTran);
        if (pool == null) return (null, null);
        var instance = pool.Get(data.ParentTran);
        if (instance == null) return (null, null);
        instance.transform.localScale = data.VectorScale ?? Vector3.one * data.LocalScale;
        instance.transform.position = data.Position;
        if (data.DelayReturn > 0) DOVirtual.DelayedCall(data.DelayReturn, () => pool.Return(instance));
        return (pool, instance);
    }

    public async UniTask<(ItemGenericObjectPool pool, GameObject instance)> PlayGameObjectPoolAsync(GameObjectPoolData data)
    {
        var pool = await GetGameObjectPoolOrCreateAsync(data.PoolName, data.ParentTran);
        if (pool == null) return (null, null);
        var instance = pool.Get(data.ParentTran);
        if (instance == null) return (null, null);
        instance.transform.localScale = data.VectorScale ?? Vector3.one * data.LocalScale;
        instance.transform.position = data.Position;
        if (data.DelayReturn > 0) DOVirtual.DelayedCall(data.DelayReturn, () => pool.Return(instance));
        return (pool, instance);
    }

    public IEnumerable<string> GetAllPoolKeys()
    {
        foreach (var key in mPools.Keys) yield return key;
    }

    public void ReturnAll()
    {
        foreach (var pool in mPools.Values) pool.ReturnAll();
    }

    public void DisposeAll()
    {
        foreach (var pool in mPools.Values) pool.DisposeAll();
        mPools.Clear();
        mLoadedPrefabs.Clear();
        mPrefabStates.Clear();
        mPrefabLoadingTasks.Clear();
        mPoolWarmupTasks.Clear();
    }

    protected virtual GameObject LoadPrefab(string poolKey) => Global.ResLoader.LoadSync<GameObject>(poolKey);

    protected virtual async UniTask<GameObject> LoadPrefabAsync(string poolKey)
    {
        var handle = YooAssets.LoadAssetAsync<GameObject>(poolKey);
        while (!handle.IsDone) await UniTask.Yield();
        return handle.Status == EOperationStatus.Succeed ? handle.GetAssetObject<GameObject>() : null;
    }

    protected virtual List<PredefinedPoolConfig> BuildDefaultPoolConfigs() => new();

    protected virtual void Awake()
    {
        MergeDefaultPoolConfigs();
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        LogTool.LogWarning($"{name} 被销毁，请使用 Release 释放对象！");
    }

    private GameObject GetOrLoadPrefabSync(string poolKey)
    {
        if (mLoadedPrefabs.TryGetValue(poolKey, out var prefab)) return prefab;
        mPrefabStates[poolKey] = PoolInitializationState.Loading;
        prefab = LoadPrefab(poolKey);
        if (prefab == null)
        {
            mPrefabStates[poolKey] = PoolInitializationState.Failed;
            return null;
        }
        mLoadedPrefabs[poolKey] = prefab;
        mPrefabStates[poolKey] = PoolInitializationState.Ready;
        return prefab;
    }

    private UniTask<GameObject> GetOrLoadPrefabAsync(string poolKey)
    {
        if (mLoadedPrefabs.TryGetValue(poolKey, out var prefab)) return UniTask.FromResult(prefab);
        if (mPrefabLoadingTasks.TryGetValue(poolKey, out var task)) return task;
        // Preserve：缓存中的任务会被多个调用方并发 await，原生 UniTask 不允许重复 await
        task = LoadPrefabInternalAsync(poolKey).Preserve();
        mPrefabLoadingTasks[poolKey] = task;
        return task;
    }

    private async UniTask<GameObject> LoadPrefabInternalAsync(string poolKey)
    {
        mPrefabStates[poolKey] = PoolInitializationState.Loading;
        try
        {
            var prefab = await LoadPrefabAsync(poolKey);
            if (prefab == null)
            {
                mPrefabStates[poolKey] = PoolInitializationState.Failed;
                LogTool.LogError($"[{GetType().Name}] 预加载预制体失败: {poolKey}");
                return null;
            }
            mLoadedPrefabs[poolKey] = prefab;
            mPrefabStates[poolKey] = PoolInitializationState.Ready;
            return prefab;
        }
        catch (Exception ex)
        {
            mPrefabStates[poolKey] = PoolInitializationState.Failed;
            LogTool.LogException(ex);
            return null;
        }
        finally
        {
            mPrefabLoadingTasks.Remove(poolKey);
        }
    }

    private async UniTask<ItemGenericObjectPool> WarmupPoolInternalAsync(string poolKey, int prewarmCount, Transform parent, int maxCount, bool worldPositionStays)
    {
        try
        {
            var config = GetPredefinedPoolConfig(poolKey);
            if (config != null)
            {
                maxCount = config.MaxCount;
                if (prewarmCount <= 0) prewarmCount = config.PrewarmCount;
            }
            var pool = await GetGameObjectPoolOrCreateAsync(poolKey, parent, maxCount, worldPositionStays);
            if (pool == null) return null;
            await PrewarmPoolAsync(pool, prewarmCount);
            return pool;
        }
        finally
        {
            mPoolWarmupTasks.Remove(poolKey);
        }
    }

    private void MergeDefaultPoolConfigs()
    {
        var defaults = BuildDefaultPoolConfigs();
        if (defaults != null)
        {
            foreach (var config in defaults)
            {
                if (config == null || string.IsNullOrEmpty(config.PoolName) || ContainsPredefinedPool(config.PoolName)) continue;
                mPredefinedPoolConfigs.Add(config);
            }
        }
        RebuildPredefinedPoolNameSet();
    }

    private bool ContainsPredefinedPool(string poolName)
    {
        for (int i = 0; i < mPredefinedPoolConfigs.Count; i++)
        {
            var config = mPredefinedPoolConfigs[i];
            if (config != null && config.PoolName == poolName) return true;
        }
        return false;
    }

    private void RebuildPredefinedPoolNameSet()
    {
        mPredefinedPoolNameSet.Clear();
        for (int i = 0; i < mPredefinedPoolConfigs.Count; i++)
        {
            var config = mPredefinedPoolConfigs[i];
            if (config == null || string.IsNullOrEmpty(config.PoolName)) continue;
            mPredefinedPoolNameSet.Add(config.PoolName);
            if (!mPrefabStates.ContainsKey(config.PoolName)) mPrefabStates.Add(config.PoolName, PoolInitializationState.None);
        }
    }

    private async UniTask PrewarmPoolAsync(ItemGenericObjectPool pool, int prewarmCount)
    {
        if (pool == null || prewarmCount <= 0) return;
        for (int i = 0; i < prewarmCount; i++)
        {
            if (!pool.PrewarmOne()) break;
            await UniTask.Yield();
        }
    }
}
