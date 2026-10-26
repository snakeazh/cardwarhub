namespace QFramework
{
using System;
using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;
using Unity.Profiling;
using Object = UnityEngine.Object;

public class GenericObjectPool : Pool<GameObject>
{
    private readonly GameObject mPrefab;
    private readonly Transform mParent;
    private readonly bool mWorldPositionStays;
    private List<GameObject> allObjects = new List<GameObject>(10);
    
    Stack<GameObject> cacheAllObjects = new Stack<GameObject>(10);
    public int MaxCount
    {
        get => mMaxCount;
        set => mMaxCount = Math.Max(0, value);
    }

    public GenericObjectPool(GameObject prefab, Transform parent, int maxCount = 120, bool worldPositionStays = false)
    {
        mPrefab = prefab;
        mParent = parent;
        mWorldPositionStays = worldPositionStays;
        mMaxCount = Math.Max(0, maxCount);

        SetFactoryMethod(() =>
        {
            var go = mPrefab ? UnityEngine.Object.Instantiate(mPrefab, mParent, mWorldPositionStays) : new GameObject("PooledGameObject");
            return go;
        });
    }

    public int CachedCount => mCacheStack.Count;

    public bool PrewarmOne()
    {
        if (mCacheStack.Count >= mMaxCount) return false;
        var go = mFactory.Create();
        cacheAllObjects.Push(go);
        return Recycle(go);
    }

    public virtual GameObject Spawn()
    {
        return mFactory.Create();
    }
    
    
    public override GameObject Allocate()
    {
        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.ObjectPoolGet))
        {
            return AllocateInternal();
        }
    }

    private GameObject AllocateInternal()
    {
        GameObject go = null;

        // 跳过所有已 Destroy 的对象
        while (mCacheStack.Count > 0)
        {
            go = mCacheStack.Pop();
            if (go != null)
                break;
        }

        // 池里无可用对象 → 新建
        if (go == null)
        {
            go = mFactory.Create();
            cacheAllObjects.Push(go);
        }

        // 设置父节点
        if (mParent && go.transform.parent != mParent)
        {
            go.transform.SetParent(mParent, mWorldPositionStays);
        }

        // 激活
        go.SetActive(true);

        allObjects.Add(go);
        return go;
    }

    public override bool Recycle(GameObject obj)
    {
        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.ObjectPoolRelease))
        {
            return RecycleInternal(obj);
        }
    }

    private bool RecycleInternal(GameObject obj)
    {
        // Unity MissingReference 安全判空
        if (obj == null)
            return false;

        // 防止重复回收（引用层面）
        if (mCacheStack.Contains(obj))
            return false;

        // 池满 → 直接销毁（不再访问）
        if (mCacheStack.Count >= mMaxCount)
        {
            UnityEngine.Object.Destroy(obj);
            return false;
        }

        // 正常回收
        obj.SetActive(false);

        if (mParent)
        {
            obj.transform.SetParent(mParent, mWorldPositionStays);
        }

        mCacheStack.Push(obj);
        return true;
    }

    // 清空并销毁池内对象
    public void DisposeAll()
    {
        while (cacheAllObjects.Count > 0)
        {
            var obj = cacheAllObjects.Pop();
            if (obj) UnityEngine.Object.Destroy(obj);
        }
        allObjects.Clear();
        mCacheStack.Clear();
    }
    
    public GameObject Get()
    {
        GameObject obj = Allocate();
        obj.SetActive(true);
        return obj;
    }

    public bool Return(GameObject obj)
    {
        return Recycle(obj);
    }
    private bool IsObjectValid(GameObject obj)
    {
        // Unity 重载的 == 运算符能正确检测销毁对象
        if (obj == null) return false;
    
        // 额外检查场景有效性（防止跨场景对象）
        return obj.scene.IsValid(); 
    }
    public void ReturnAll()
    {
        for (int i = allObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = allObjects[i];
        
            // 严格对象有效性检查
            if (!IsObjectValid(obj))
            {
                allObjects.RemoveAt(i);
                continue;
            }
            
            // 标准回收流程
            obj.SetActive(false);
            Return(obj); // 会进入队列二次检查
        }
    }
    
    public void ReturnAllEffect()
    {
        // for (int i = allObjects.Count - 1; i >= 0; i--)
        // {
        //     GameObject obj = allObjects[i];
        //
        //     // 严格对象有效性检查
        //     if (!IsObjectValid(obj))
        //     {
        //         allObjects.RemoveAt(i);
        //         continue;
        //     }
        //
        //     // 安全设置父节点
        //     if (ObjectPoolManager.Instance != null && 
        //         ObjectPoolManager.Instance.transform != null)
        //     {
        //         obj.transform.SetParent(ObjectPoolManager.Instance.transform);
        //     }
        //
        //     // 标准回收流程
        //     obj.SetActive(false);
        //     Return(obj); // 会进入队列二次检查
        // }
    }
    
    
}
}