using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;

public class UIPoolScope : MonoBehaviour
{
    private const string DefaultGroup = "__default__";

    [SerializeField] private bool mReleaseOnDisable = false;
    [SerializeField] private bool mReleaseOnDestroy = true;

    private class ScopeItem
    {
        public string Group;
        public ItemGenericObjectPool Pool;
        public GameObject Instance;
    }

    private readonly List<ScopeItem> mItems = new List<ScopeItem>();

    public bool ReleaseOnDisable
    {
        get => mReleaseOnDisable;
        set => mReleaseOnDisable = value;
    }

    public bool ReleaseOnDestroy
    {
        get => mReleaseOnDestroy;
        set => mReleaseOnDestroy = value;
    }

    public GameObject Spawn(string poolKey, Transform parent = null, string group = null)
    {
        var pool = UIPanelItemPoolManager.Instance?.GetGameObjectPoolOrCreate(poolKey, parent);
        if (pool == null)
        {
            LogTool.LogError($"[UIPoolScope] 找不到对象池: {poolKey}");
            return null;
        }

        var instance = pool.Get(parent != null ? parent : transform);
        if (instance == null)
        {
            return null;
        }

        Track(pool, instance, group);
        return instance;
    }

    public async UniTask<GameObject> SpawnAsync(string poolKey, Transform parent = null, string group = null)
    {
        var pool = await UIPanelItemPoolManager.Instance.GetGameObjectPoolOrCreateAsync(poolKey, parent);
        if (pool == null)
        {
            LogTool.LogError($"[UIPoolScope] 异步创建对象池失败: {poolKey}");
            return null;
        }

        var instance = pool.Get(parent != null ? parent : transform);
        if (instance == null)
        {
            return null;
        }

        Track(pool, instance, group);
        return instance;
    }

    public T Spawn<T>(string poolKey, Transform parent = null, string group = null) where T : Component
    {
        var instance = Spawn(poolKey, parent, group);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    public async UniTask<T> SpawnAsync<T>(string poolKey, Transform parent = null, string group = null) where T : Component
    {
        var instance = await SpawnAsync(poolKey, parent, group);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    public void Track(ItemGenericObjectPool pool, GameObject instance, string group = null)
    {
        if (pool == null || instance == null)
        {
            return;
        }

        mItems.Add(new ScopeItem
        {
            Group = string.IsNullOrEmpty(group) ? DefaultGroup : group,
            Pool = pool,
            Instance = instance,
        });
    }

    public void ReleaseGroup(string group)
    {
        string targetGroup = string.IsNullOrEmpty(group) ? DefaultGroup : group;
        for (int i = mItems.Count - 1; i >= 0; i--)
        {
            var item = mItems[i];
            if (item.Group != targetGroup)
            {
                continue;
            }

            ReleaseItem(item);
            mItems.RemoveAt(i);
        }
    }

    public void ReleaseAll()
    {
        for (int i = mItems.Count - 1; i >= 0; i--)
        {
            ReleaseItem(mItems[i]);
        }

        mItems.Clear();
    }

    private void OnDisable()
    {
        if (mReleaseOnDisable)
        {
            ReleaseAll();
        }
    }

    private void OnDestroy()
    {
        if (mReleaseOnDestroy)
        {
            ReleaseAll();
        }
    }

    private void ReleaseItem(ScopeItem item)
    {
        if (item?.Pool == null || item.Instance == null)
        {
            return;
        }

        item.Pool.Return(item.Instance);
    }

    public static UIPoolScope Get(GameObject target)
    {
        return target.GetOrAddComponent<UIPoolScope>();
    }
}
