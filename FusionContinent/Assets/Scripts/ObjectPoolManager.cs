using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;
    private Dictionary<string, QFramework.GenericObjectPool> gameObjectPools = new Dictionary<string, QFramework.GenericObjectPool>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);// 可选，视是否要跨场景保留
        }
        else
        {
            Destroy(gameObject);
        }

        // Instance = this;
        
    }

    public QFramework.GenericObjectPool CreateGameObjectPool(string poolKey, GameObject prefab, Transform parent = null)
    {
        if (gameObjectPools.TryGetValue(poolKey, out var objectPool))
        {
            return objectPool;
        }

        var pool = new QFramework.GenericObjectPool(prefab, parent);
        gameObjectPools[poolKey] = pool;
        return pool;
    }

    
    public void DestroyGameObjectPool(string poolKey)
    {
        gameObjectPools.Remove(poolKey);
    }
    
    public QFramework.GenericObjectPool GetGameObjectPool(string poolKey)
    {
        return gameObjectPools.GetValueOrDefault(poolKey);
    }
    
    public QFramework.GenericObjectPool GetGameObjectPoolOrCreate(string poolKey)
    {
        if (gameObjectPools.TryGetValue(poolKey, out var create))
        {
            return create;
        }

        var obj = Global.ResLoader.LoadSync<GameObject>(poolKey);
        CreateGameObjectPool(poolKey, obj);
        return GetGameObjectPool(poolKey);
    }
    
    /**
     * 播放通用游戏对象池
     **/
    public (QFramework.GenericObjectPool, GameObject) PlayGameObjectPool(GameObjectPoolData data)
    {
        var pool = GetGameObjectPoolOrCreate(data.PoolName);
        if (pool == null) return (null, null);

        GameObject obj = pool.Get();
        if (obj == null) return (null, null);
            
        // 设置预制体
        obj.transform.SetParent(data.ParentTran);
        obj.transform.localScale = data.VectorScale ?? Vector3.one * data.LocalScale;
        obj.transform.position = data.Position;
        if (data.DelayReturn > 0) DOVirtual.DelayedCall(data.DelayReturn, () => pool.Return(obj));
        return (pool, obj);
    } 

    public IEnumerable<string> GetAllPoolKeys()
    {
        foreach (var key in gameObjectPools.Keys)
        {
            yield return key;
        }
    }

    public void AddObjectPoolList(List<string> assetNameList, Transform parent)
    {
        if (assetNameList.Count == 0) return;
        
        foreach (var assetName in assetNameList)
        {
            GameObject obj = Global.ResLoader.LoadSync<GameObject>(assetName);
            CreateGameObjectPool(assetName, obj, parent);
            GetGameObjectPool(assetName).ReturnAll();
        }
    }

    public void ReturnAll()
    {
        foreach (var obj in gameObjectPools.Values)
        {
            obj.ReturnAll();
        }
    }
    
    void OnDestroy()
    {
        LogTool.LogWarning($"{name} 被销毁，请使用 Release 释放对象！");
    }
}

public class GameObjectPoolData
{
    public string PoolName;
    public Transform ParentTran;
    public Vector3 Position;
    public Vector3? VectorScale = null;
    public float LocalScale = 100f;
    public float DelayReturn = 0.5f;

}