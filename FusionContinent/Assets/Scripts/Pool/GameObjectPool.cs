using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class GameObjectPool
{
    private readonly GameObject mPrefab;
    private readonly Transform mRoot;
    private readonly bool mWorldPositionStays;
    private readonly Queue<GameObject> mInactiveObjects = new Queue<GameObject>();
    private readonly HashSet<GameObject> mActiveObjects = new HashSet<GameObject>();
    private readonly HashSet<GameObject> mAllObjects = new HashSet<GameObject>();

    public string PoolKey { get; }
    public int MaxCount { get; set; }
    public int ActiveCount => mActiveObjects.Count;
    public int InactiveCount => mInactiveObjects.Count;
    public int TotalCount => mAllObjects.Count;

    public GameObjectPool(string poolKey, GameObject prefab, Transform root, int maxCount = 120, bool worldPositionStays = false)
    {
        PoolKey = poolKey;
        mPrefab = prefab;
        mRoot = root;
        mWorldPositionStays = worldPositionStays;
        MaxCount = Math.Max(0, maxCount);
    }

    public bool PrewarmOne()
    {
        if (InactiveCount >= MaxCount)
        {
            return false;
        }

        var instance = CreateInstance(false);
        return Return(instance);
    }

    public GameObject Get(Transform parent = null, bool markActive = true)
    {
        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.ObjectPoolGet))
        {
            var instance = DequeueInactive();
            if (instance == null)
            {
                instance = CreateInstance(TotalCount >= MaxCount);
            }

            var identity = EnsureIdentity(instance);
            identity.IsTemporaryOverflow = identity.IsTemporaryOverflow && TotalCount > MaxCount;

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }
            else if (mRoot != null && instance.transform.parent != mRoot)
            {
                instance.transform.SetParent(mRoot, mWorldPositionStays);
            }

            if (markActive)
            {
                mActiveObjects.Add(instance);
            }

            instance.SetActive(true);
            NotifySpawn(instance);
            return instance;
        }
    }

    public bool Return(GameObject instance)
    {
        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.ObjectPoolRelease))
        {
            if (instance == null)
            {
                return false;
            }

            if (!mAllObjects.Contains(instance))
            {
                return false;
            }

            var identity = instance.GetComponent<PoolIdentity>();
            if (identity == null || identity.PoolKey != PoolKey)
            {
                return false;
            }

            mActiveObjects.Remove(instance);
            NotifyRecycle(instance);
            instance.SetActive(false);

            if (identity.IsTemporaryOverflow)
            {
                mAllObjects.Remove(instance);
                UnityEngine.Object.Destroy(instance);
                return true;
            }

            if (mRoot != null)
            {
                instance.transform.SetParent(mRoot, mWorldPositionStays);
            }

            if (mInactiveObjects.Contains(instance))
            {
                return false;
            }

            mInactiveObjects.Enqueue(instance);
            return true;
        }
    }

    public void ReturnAll()
    {
        var activeSnapshot = new List<GameObject>(mActiveObjects);
        foreach (var instance in activeSnapshot)
        {
            if (instance == null)
            {
                continue;
            }

            Return(instance);
        }
    }

    public void Clear()
    {
        foreach (var instance in mAllObjects)
        {
            if (instance != null)
            {
                UnityEngine.Object.Destroy(instance);
            }
        }

        mInactiveObjects.Clear();
        mActiveObjects.Clear();
        mAllObjects.Clear();
    }

    private GameObject DequeueInactive()
    {
        while (mInactiveObjects.Count > 0)
        {
            var instance = mInactiveObjects.Dequeue();
            if (instance != null)
            {
                return instance;
            }
        }

        return null;
    }

    private GameObject CreateInstance(bool isTemporaryOverflow)
    {
        var instance = mPrefab != null
            ? UnityEngine.Object.Instantiate(mPrefab, mRoot, mWorldPositionStays)
            : new GameObject($"Pooled_{PoolKey}");
        if (instance == null)
        {
            LogTool.E($"找不到对应的池子 {PoolKey}");
            return null;
        }
        var identity = EnsureIdentity(instance);
        identity.PoolKey = PoolKey;
        identity.IsTemporaryOverflow = isTemporaryOverflow;
        mAllObjects.Add(instance);
        instance.SetActive(false);
        return instance;
    }

    private PoolIdentity EnsureIdentity(GameObject instance)
    {
        var identity = instance.GetComponent<PoolIdentity>();
        if (identity == null)
        {
            identity = instance.AddComponent<PoolIdentity>();
        }

        if (string.IsNullOrEmpty(identity.PoolKey))
        {
            identity.PoolKey = PoolKey;
        }

        return identity;
    }

    private void NotifySpawn(GameObject instance)
    {
        var poolables = instance.GetComponents<IPoolable>();
        foreach (var poolable in poolables)
        {
            poolable.OnSpawn();
        }
    }

    private void NotifyRecycle(GameObject instance)
    {
        var poolables = instance.GetComponents<IPoolable>();
        foreach (var poolable in poolables)
        {
            poolable.OnRecycle();
        }
    }
}
