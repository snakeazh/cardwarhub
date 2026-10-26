using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UnityEngine;
using Unity.Profiling;
using YooAsset;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    private Dictionary<string, GameObject> _effectPrefabs = new();

    private List<string>  _poolKeys = new List<string>();  
    
    [SerializeField] private string _effectFolder = "VFX";
    [SerializeField] private float _defaultEffectLifetime = 5f;
    [SerializeField] private int _defaultPoolMaxCount = 20;
    [SerializeField] private int _defaultPoolPrewarmCount = 8;

    // ⭐ 分层
    private Transform _worldRoot;
    private Transform _characterRoot;
    private Transform _uiRoot;

    // 挂在怪物/士兵等战斗实体下时，不能在对方 OnDisable 期间改 parent，延迟一帧回池
    private readonly List<GameObject> _deferredRecycleQueue = new List<GameObject>(16);
    private bool _deferredRecycleScheduled;

    public bool IsReady => _effectPrefabs.Count > 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);

            InitLayers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitLayers()
    {
        _worldRoot = new GameObject("Effect_World").transform;
        _characterRoot = new GameObject("Effect_Character").transform;
        _uiRoot = new GameObject("Effect_UI").transform;

        _worldRoot.SetParent(transform);
        _characterRoot.SetParent(transform);
        _uiRoot.SetParent(transform);
    }

    private Transform GetLayerRoot(EffectLayer layer)
    {
        return layer switch
        {
            EffectLayer.World => _worldRoot,
            EffectLayer.Character => _characterRoot,
            EffectLayer.UI => _uiRoot,
            _ => _worldRoot
        };
    }

    // ================= 预加载 =================

    public void PreloadAllEffects(string folder, Action onComplete = null)
    {
        StartCoroutine(PreloadCoroutine(folder, onComplete));
    }

    /// <summary>
    /// 按名单异步预载特效（WebGL 不支持同步加载，战斗入场时必须先把特效灌进 _effectPrefabs）。
    /// 走 Global.ResLoader.LoadAsync，同时暖 ResLoader 缓存，CreatePool 建池时的同步读才能命中。
    /// </summary>
    public IEnumerator PreloadEffectsAsync(IReadOnlyList<string> effectNames)
    {
        if (effectNames == null)
        {
            yield break;
        }

        for (int i = 0; i < effectNames.Count; i++)
        {
            var effectName = effectNames[i];
            if (string.IsNullOrEmpty(effectName) || _effectPrefabs.ContainsKey(effectName))
            {
                continue;
            }

            GameObject prefab = null;
            yield return Global.ResLoader.LoadAsync<GameObject>(effectName, p => prefab = p);
            if (prefab == null)
            {
                LogTool.LogError($"[EffectManager] 预载特效失败: {effectName}");
                continue;
            }

            _effectPrefabs[effectName] = prefab;
            CreatePool(effectName, prefab);
        }
    }

    /// <summary>PreloadEffectsAsync 的 UniTask 版本，供无协程上下文的调用方（如技能图谱插入）await。</summary>
    public async UniTask PreloadEffectsUniTask(IReadOnlyList<string> effectNames)
    {
        if (effectNames == null)
        {
            return;
        }

        for (int i = 0; i < effectNames.Count; i++)
        {
            var effectName = effectNames[i];
            if (string.IsNullOrEmpty(effectName) || _effectPrefabs.ContainsKey(effectName))
            {
                continue;
            }

            GameObject prefab = await Global.ResLoader.LoadAssetUniTask<GameObject>(effectName);
            if (prefab == null)
            {
                LogTool.LogError($"[EffectManager] 预载特效失败: {effectName}");
                continue;
            }

            _effectPrefabs[effectName] = prefab;
            CreatePool(effectName, prefab);
        }
    }

    public void PreloadEffectPool(string effectName, int prewarmCount = -1)
    {
        if (string.IsNullOrEmpty(effectName))
        {
            return;
        }

        GameObject prefab;
        if (!TryGetPrefab(effectName, out prefab))
        {
            return;
        }

        var pool = BattleItemPoolManager.Instance.GetGameObjectPoolOrCreate(effectName);
        if (pool == null)
        {
            return;
        }

        if (!_poolKeys.Contains(effectName))
        {
            _poolKeys.Add(effectName);
        }

        int target = prewarmCount > 0 ? prewarmCount : _defaultPoolPrewarmCount;
        PrewarmPool(pool, target);
    }

    private static void PrewarmPool(ItemGenericObjectPool pool, int count)
    {
        if (pool == null || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (!pool.PrewarmOne())
            {
                break;
            }
        }
    }

    private IEnumerator PreloadCoroutine(string folder, Action onComplete)
    {
        var handle = YooAssets.LoadAllAssetsAsync(folder);
        yield return handle;

        if (handle.Status != EOperationStatus.Succeed)
        {
            LogTool.LogError($"特效加载失败: {handle.LastError}");
            handle.Release();
            onComplete?.Invoke();
            yield break;
        }

        foreach (var obj in handle.AllAssetObjects)
        {
            if (obj is not GameObject prefab) continue;

            string name = prefab.name;

            if (_effectPrefabs.ContainsKey(name)) continue;

            _effectPrefabs[name] = prefab;
            CreatePool(name, prefab);
        }

        handle.Release();
        onComplete?.Invoke();
    }

    private void CreatePool(string name, GameObject prefab)
    {
        Transform parent = new GameObject($"Pool_{name}").transform;
        parent.SetParent(transform);
        var pool = BattleItemPoolManager.Instance.GetGameObjectPoolOrCreate(name,parent,_defaultPoolMaxCount,false);
        if(!_poolKeys.Contains(name))
            _poolKeys.Add(name);
        PrewarmPool(pool, _defaultPoolPrewarmCount);
    }

    // ================= 播放 =================

    public GameObject PlayEffect(
        string effectName,
        Vector3 position,
        float lifetime = -1f,
        Vector3? scale = null,
        Quaternion? rotation = null,
        Transform parent = null,
        EffectLayer layer = EffectLayer.World)
    {
        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.EffectSpawn))
        {
            // 先隐藏取出并定位，再统一 Reset+激活，避免 Spawn/Lifecycle 多次 Clear+Play
            var effect = SpawnPooledEffect(effectName, position, scale, rotation, parent, layer, activate: false);
            if (effect == null) return null;

            var pool = BattleItemPoolManager.Instance.GetGameObjectPoolOrCreate(effectName);
            float finalLifetime = lifetime;
            if (lifetime == 0) finalLifetime = _defaultEffectLifetime;

            EnsureLifecycle(effect, pool, finalLifetime, resetVisualState: false);
            ResetEffect(effect);
            effect.SetActive(true);
            ApplySpawnTransform(effect.transform, position, rotation, scale);

            return effect;
        }
    }

    /// <summary>
    /// 播放投射物特效：按 max_time 定时回收，不监听粒子结束。
    /// </summary>
    public GameObject PlayProjectileEffect(
        string effectName,
        Vector3 position,
        float maxTime,
        Vector3? scale = null,
        Quaternion? rotation = null,
        Transform parent = null,
        EffectLayer layer = EffectLayer.World)
    {
        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.EffectSpawn))
        {
            if (!TryGetPrefab(effectName, out _)) return null;

            var pool = BattleItemPoolManager.Instance.GetGameObjectPoolOrCreate(effectName);
            if (!_poolKeys.Contains(effectName))
                _poolKeys.Add(effectName);

            var effect = SpawnPooledEffect(effectName, position, scale, rotation, parent, layer, activate: false);
            if (effect == null) return null;

            EnsureProjectileLifecycle(effect, pool, maxTime);
            FinalizePooledEffectTransform(effect, position, rotation, scale);
            return effect;
        }
    }

    private void FinalizePooledEffectTransform(
        GameObject effect,
        Vector3 position,
        Quaternion? rotation,
        Vector3? scale)
    {
        if (effect == null) return;

        ApplySpawnTransform(effect.transform, position, rotation, scale);
        ResetEffect(effect);
        effect.SetActive(true);
        ApplySpawnTransform(effect.transform, position, rotation, scale);
    }

    private static void ApplySpawnTransform(
        Transform tf,
        Vector3 position,
        Quaternion? rotation,
        Vector3? scale)
    {
        tf.position = position;
        tf.rotation = rotation ?? Quaternion.identity;
        tf.localScale = scale ?? Vector3.one;
    }

    private GameObject SpawnPooledEffect(
        string effectName,
        Vector3 position,
        Vector3? scale,
        Quaternion? rotation,
        Transform parent,
        EffectLayer layer,
        bool activate = true)
    {
        if (!TryGetPrefab(effectName, out _)) return null;

        var pool = BattleItemPoolManager.Instance.GetGameObjectPoolOrCreate(effectName);
        if (!_poolKeys.Contains(effectName))
            _poolKeys.Add(effectName);

        var effect = pool.Get();
        if (effect == null) return null;

        // 先隐藏，避免从池取出后在旧位置与目标位置之间拉出拖尾
        effect.SetActive(false);

        Transform tf = effect.transform;
        Transform root = parent != null ? parent : GetLayerRoot(layer);

        ApplySpawnTransform(tf, position, rotation, scale);
        if (tf.parent != root)
        {
            tf.SetParent(root, true);
        }

        // 仅清理拖尾/粒子残留，不在此处 Play；由调用方在最终激活时 ResetEffect 一次
        ClearEffectResiduals(effect);

        if (activate)
        {
            ResetEffect(effect);
            effect.SetActive(true);
            ApplySpawnTransform(tf, position, rotation, scale);
        }

        return effect;
    }

    private void EnsureProjectileLifecycle(GameObject effect, ItemGenericObjectPool pool, float maxTime)
    {
        var lifecycle = effect.GetComponent<EffectLifecycle>();
        if (lifecycle == null)
            lifecycle = effect.AddComponent<EffectLifecycle>();

        lifecycle.InitProjectileTimer(pool, maxTime);
    }

    public void ResetEffect(GameObject effect)
    {
        ClearEffectResiduals(effect);

        var particles = effect.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < particles.Length; i++)
        {
            var p = particles[i];
            if (p != null)
            {
                p.Play();
            }
        }
    }

    private static void ClearEffectResiduals(GameObject effect)
    {
        var trails = effect.GetComponentsInChildren<TrailRenderer>();
        for (int i = 0; i < trails.Length; i++)
        {
            var t = trails[i];
            if (t != null)
            {
                t.Clear();
            }
        }

        var particles = effect.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < particles.Length; i++)
        {
            var p = particles[i];
            if (p != null)
            {
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
    

    // ================= 工具 =================

    private bool TryGetPrefab(string name, out GameObject prefab)
    {
        if (_effectPrefabs.TryGetValue(name, out prefab))
            return true;

        prefab = TryLoadSync(name);
        return prefab != null;
    }

    private GameObject TryLoadSync(string name)
    {
#if !UNITY_WEBGL
        try
        {
            var handle = YooAssets.LoadAssetSync<GameObject>(name);
            if (handle.Status == EOperationStatus.Succeed)
            {
                var asset = handle.GetAssetObject<GameObject>();
                _effectPrefabs[name] = asset;
                CreatePool(name, asset);
                return asset;
            }
        }
        catch { }
#endif

        var res = Resources.Load<GameObject>(name);
        if (res != null)
        {
            _effectPrefabs[name] = res;
            CreatePool(name, res);
            return res;
        }

#if UNITY_WEBGL
        // WebGL 不支持同步加载，特效必须在战斗加载阶段用 PreloadEffectsAsync 预载
        LogTool.LogError($"[EffectManager] WebGL 同步加载特效失败（未预载）: {name}");
#else
        LogTool.LogWarning($"特效不存在: {name}");
#endif
        return null;
    }

    private void EnsureLifecycle(GameObject effect, ItemGenericObjectPool pool, float lifetime, bool resetVisualState = true)
    {
        var lifecycle = effect.GetComponent<EffectLifecycle>();
        if (lifecycle == null)
            lifecycle = effect.AddComponent<EffectLifecycle>();

        lifecycle.Init(pool, lifetime, null, resetVisualState);
    }

    // ================= 回收 =================

    public void RecycleEffect(GameObject effect)
    {
        if (effect == null) return;

        if (ShouldDeferRecycle(effect))
        {
            EnqueueDeferredRecycle(effect);
            return;
        }

        RecycleEffectImmediate(effect);
    }

    private bool ShouldDeferRecycle(GameObject effect)
    {
        var parent = effect.transform.parent;
        if (parent == null)
            return false;

        // 已在 EffectManager 子树（分层根 / Pool_xxx）下，可立即回池
        return !parent.IsChildOf(transform);
    }

    private void EnqueueDeferredRecycle(GameObject effect)
    {
        for (int i = 0; i < _deferredRecycleQueue.Count; i++)
        {
            if (_deferredRecycleQueue[i] == effect)
                return;
        }

        _deferredRecycleQueue.Add(effect);

        if (!_deferredRecycleScheduled)
        {
            _deferredRecycleScheduled = true;
            StartCoroutine(DeferredRecycleRoutine());
        }
    }

    private IEnumerator DeferredRecycleRoutine()
    {
        yield return null;
        _deferredRecycleScheduled = false;

        if (_deferredRecycleQueue.Count == 0)
            yield break;

        var snapshot = new List<GameObject>(_deferredRecycleQueue);
        _deferredRecycleQueue.Clear();

        for (int i = 0; i < snapshot.Count; i++)
        {
            var effect = snapshot[i];
            if (effect != null)
                RecycleEffectImmediate(effect);
        }
    }

    private void RecycleEffectImmediate(GameObject effect)
    {
        if (effect == null) return;

        var lifecycle = effect.GetComponent<EffectLifecycle>();
        if (lifecycle != null && lifecycle.enabled)
        {
            lifecycle.RecycleNow();
            return;
        }

        ReturnToPoolOrDestroy(effect);
    }

    private void ReturnToPoolOrDestroy(GameObject effect)
    {
        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.ObjectPoolRelease))
        {
            var identity = effect.GetComponent<PoolIdentity>();
            if (identity == null || string.IsNullOrEmpty(identity.PoolKey))
            {
                Destroy(effect);
                return;
            }

            var pool = BattleItemPoolManager.Instance.GetGameObjectPoolOrCreate(identity.PoolKey);
            if (pool == null)
            {
                Destroy(effect);
                return;
            }

            pool.Return(effect);
        }
    }

    // ================= 清理 =================

    public void ClearAllPools()
    {
        if (_poolKeys.Count == 0)
        {
            return;
        }

        var poolManager = BattleItemPoolManager.Instance;
        if (poolManager != null)
        {
            for (int i = _poolKeys.Count - 1; i >= 0; i--)
            {
                poolManager.DestroyGameObjectPool(_poolKeys[i]);
            }
        }

        _poolKeys.Clear();
    }

    private void OnDestroy()
    {
        ClearAllPools();
        if (Instance == this) Instance = null;
    }
}

// ================= 枚举 =================

public enum EffectLayer
{
    World,
    Character,
    UI
}