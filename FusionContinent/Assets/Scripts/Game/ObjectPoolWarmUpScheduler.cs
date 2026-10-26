using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolWarmUpScheduler : MonoBehaviour
{
    private class WarmUpConfig { public int TargetCount; public int PerFrame; public int MinBuffer; public int RefillCount; }
    private readonly Dictionary<string, WarmUpConfig> warmUpConfigs = new Dictionary<string, WarmUpConfig>();
    private readonly List<string> warmUpKeys = new List<string>();
    private Coroutine globalWarmUpCoroutine;

    public void RegisterWarmUp(string poolKey, int targetCount = -1, int perFrame = 1)
    {
        var pool = ObjectPoolManager.Instance?.GetGameObjectPool(poolKey);
        if (pool == null) return;
        if (targetCount < 0) targetCount = pool.MaxCount;
        targetCount = Math.Min(targetCount, pool.MaxCount);
        warmUpConfigs[poolKey] = new WarmUpConfig { TargetCount = targetCount, PerFrame = Math.Max(1, perFrame), MinBuffer = 0, RefillCount = 0 };
        if (!warmUpKeys.Contains(poolKey)) warmUpKeys.Add(poolKey);
    }

    public void UnregisterWarmUp(string poolKey)
    {
        warmUpConfigs.Remove(poolKey);
        warmUpKeys.Remove(poolKey);
    }

    public void SetMaintenance(string poolKey, int minBuffer, int refillCount, int perFrame = 1)
    {
        var pool = ObjectPoolManager.Instance?.GetGameObjectPool(poolKey);
        if (pool == null) return;
        var cfg = warmUpConfigs.ContainsKey(poolKey) ? warmUpConfigs[poolKey] : new WarmUpConfig();
        cfg.MinBuffer = Math.Max(0, Math.Min(minBuffer, pool.MaxCount));
        cfg.RefillCount = Math.Max(0, refillCount);
        cfg.PerFrame = Math.Max(1, perFrame);
        if (!warmUpConfigs.ContainsKey(poolKey)) warmUpConfigs[poolKey] = cfg;
        if (!warmUpKeys.Contains(poolKey)) warmUpKeys.Add(poolKey);
    }

    public void RegisterAllPoolsForWarmUp(int perFrame = 1)
    {
        var mgr = ObjectPoolManager.Instance;
        if (mgr == null) return;
        foreach (var key in mgr.GetAllPoolKeys())
        {
            RegisterWarmUp(key, -1, perFrame);
        }
    }

    public void StartGlobalWarmUp()
    {
        if (globalWarmUpCoroutine != null) return;
        if (warmUpKeys.Count == 0) return;
        globalWarmUpCoroutine = StartCoroutine(GlobalWarmUpCoroutine());
    }

    public void StopGlobalWarmUp()
    {
        if (globalWarmUpCoroutine != null)
        {
            StopCoroutine(globalWarmUpCoroutine);
            globalWarmUpCoroutine = null;
        }
    }

    private IEnumerator GlobalWarmUpCoroutine()
    {
        int idx = 0;
        while (warmUpKeys.Count > 0)
        {
            var key = warmUpKeys[idx];
            var pool = ObjectPoolManager.Instance?.GetGameObjectPool(key);
            if (pool == null || !warmUpConfigs.TryGetValue(key, out var cfg))
            {
                warmUpConfigs.Remove(key);
                warmUpKeys.RemoveAt(idx);
                if (warmUpKeys.Count == 0) break;
                idx %= warmUpKeys.Count;
                yield return null;
                continue;
            }

            bool worked = false;
            if (cfg.TargetCount >= 0 && pool.CachedCount < cfg.TargetCount)
            {
                for (int i = 0; i < cfg.PerFrame && pool.CachedCount < cfg.TargetCount; i++)
                {
                    pool.PrewarmOne();
                    worked = true;
                }
                if (pool.CachedCount >= cfg.TargetCount && cfg.MinBuffer <= 0)
                {
                    warmUpConfigs.Remove(key);
                    warmUpKeys.RemoveAt(idx);
                    if (warmUpKeys.Count == 0) break;
                    idx %= warmUpKeys.Count;
                    yield return null;
                    continue;
                }
                if (pool.CachedCount >= cfg.TargetCount && cfg.MinBuffer > 0)
                {
                    cfg.TargetCount = -1;
                    warmUpConfigs[key] = cfg;
                }
            }
            else if (cfg.MinBuffer > 0 && pool.CachedCount < cfg.MinBuffer)
            {
                int toMake = Math.Min(cfg.RefillCount, cfg.MinBuffer - pool.CachedCount);
                for (int i = 0; i < cfg.PerFrame && i < toMake; i++)
                {
                    pool.PrewarmOne();
                    worked = true;
                }
            }

            idx = warmUpKeys.Count == 0 ? 0 : (idx + 1) % warmUpKeys.Count;

            yield return null;
        }
        globalWarmUpCoroutine = null;
    }
    static List<string> doDestroy = new List<string>()
    {
        "ShopItemTitleObj",
        "RewardEntity",
    };
    private void OnDestroy()
    {
        foreach (var key in ObjectPoolManager.Instance.GetAllPoolKeys())
        {
            if (doDestroy.Contains(key))
            {
                continue;
            }
            ObjectPoolManager.Instance.GetGameObjectPool(key).DisposeAll();
        }
        

    }
}