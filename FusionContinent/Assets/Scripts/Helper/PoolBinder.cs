using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PoolBinder : MonoBehaviour
{
    private UIPoolScope mPoolScope;
    private string selfPoolName;

    private void Init()
    {
        mPoolScope ??= UIPoolScope.Get(gameObject);
    }

    public void Add(GameObject obj)
    {
        Init();
        var pool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate(selfPoolName);
        mPoolScope.Track(pool, obj, selfPoolName);
    }

    public void RemoveAll()
    {
        Init();
        mPoolScope.ReleaseGroup(selfPoolName);
    }

    public static PoolBinder Get(GameObject target, string poolName)
    {
        var binder = target.GetOrAddComponent<PoolBinder>();
        binder.selfPoolName = poolName;
        binder.Init();
        return binder;
    }
}
