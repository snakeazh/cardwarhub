using UnityEngine;

public class ItemGenericObjectPool : GameObjectPool
{
    public int CachedCount => InactiveCount;

    public ItemGenericObjectPool(GameObject prefab, Transform parent, int maxCount = 120, bool worldPositionStays = false)
        : base(prefab != null ? prefab.name : "PooledGameObject", prefab, parent, maxCount, worldPositionStays)
    {
    }

    public ItemGenericObjectPool(string poolKey, GameObject prefab, Transform parent, int maxCount = 120, bool worldPositionStays = false)
        : base(poolKey, prefab, parent, maxCount, worldPositionStays)
    {
    }

    public virtual GameObject Spawn()
    {
        return Get();
    }

    public GameObject Allocate()
    {
        return Get();
    }

    public bool Recycle(GameObject obj)
    {
        return Return(obj);
    }

    public void DisposeAll()
    {
        Clear();
    }
}
