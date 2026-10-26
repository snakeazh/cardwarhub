namespace FusionContinent.Pool
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class UIPoolWarmUpScheduler : MonoBehaviour
    {
        private class WarmUpConfig { public int TargetCount; public int PerFrame; public int MinBuffer; public int RefillCount; }
        private readonly Dictionary<string, WarmUpConfig> warmUpConfigs = new Dictionary<string, WarmUpConfig>();
        private readonly List<string> warmUpKeys = new List<string>();
        private Coroutine globalWarmUpCoroutine;


        private void Start()
        {
            RegisterPool();
            StartGlobalWarmUp();
        }

        private void RegisterPool()
        {
#if UNITY_WEBGL
            // WebGL 启动期不做 UI 池预热：预热只是性能优化，且启动早期 Instantiate UI 槽位会触发组件 NRE；
            // 池在面板首次打开时按需创建（预制体已在启动时预载进 ResLoader 缓存）
            return;
#else
            RegisterWarmUp("CardGroupSlot",50);
            RegisterWarmUp("SoldierCardGroupSlot",10);
#endif
        }
        
        public void RegisterWarmUp(string poolKey, int targetCount = -1, int perFrame = 1)
        {
            RegisterWarmUpAsync(poolKey, targetCount, perFrame).Forget();
        }

        private async UniTaskVoid RegisterWarmUpAsync(string poolKey, int targetCount, int perFrame)
        {
            var mgr = UIPoolManager.Instance;
            if (mgr == null) return;

            // WebGL 不支持同步加载，统一走异步创建对象池（预制体通常已在启动时预载，这里多为秒回）
            var pool = mgr.GetGameObjectPool(poolKey);
            if (pool == null)
            {
                pool = await mgr.GetGameObjectPoolOrCreateAsync(poolKey);
            }
            if (pool == null) return;

            // StartCoroutine 的首次 MoveNext 同步执行，预热 Instantiate 触发的组件异常会同步传回这里，统一捕获定位
            try
            {
                if (targetCount < 0) targetCount = pool.MaxCount;
                targetCount = Math.Min(targetCount, pool.MaxCount);
                warmUpConfigs[poolKey] = new WarmUpConfig { TargetCount = targetCount, PerFrame = Math.Max(1, perFrame), MinBuffer = 0, RefillCount = 0 };
                if (!warmUpKeys.Contains(poolKey)) warmUpKeys.Add(poolKey);

                // 注册是异步完成的，这里兜底触发一次全局预热（方法内部有重入保护）
                StartGlobalWarmUp();
            }
            catch (Exception ex)
            {
                LogTool.LogError($"[UIPoolWarmUpScheduler] 注册预热失败: poolKey={poolKey}, targetCount={targetCount}, perFrame={perFrame}, poolNull={pool == null}, configsNull={warmUpConfigs == null}, keysNull={warmUpKeys == null}");
                LogTool.LogException(ex);
            }
        }

        public void UnregisterWarmUp(string poolKey)
        {
            warmUpConfigs.Remove(poolKey);
            warmUpKeys.Remove(poolKey);
        }

        public void SetMaintenance(string poolKey, int minBuffer, int refillCount, int perFrame = 1)
        {
            var pool = UIPoolManager.Instance?.GetGameObjectPool(poolKey);
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
            var mgr = UIPoolManager.Instance;
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
                var removeCurrent = false;
                try
                {
                    var pool = UIPoolManager.Instance?.GetGameObjectPool(key);
                    if (pool == null || !warmUpConfigs.TryGetValue(key, out var cfg))
                    {
                        removeCurrent = true;
                    }
                    else if (cfg.TargetCount >= 0 && pool.CachedCount < cfg.TargetCount)
                    {
                        for (int i = 0; i < cfg.PerFrame && pool.CachedCount < cfg.TargetCount; i++)
                        {
                            pool.PrewarmOne();
                        }
                        if (pool.CachedCount >= cfg.TargetCount && cfg.MinBuffer <= 0)
                        {
                            removeCurrent = true;
                        }
                        else if (pool.CachedCount >= cfg.TargetCount && cfg.MinBuffer > 0)
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
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogTool.LogError($"[UIPoolWarmUpScheduler] 预热池异常: poolKey={key}");
                    LogTool.LogException(ex);
                }

                if (removeCurrent)
                {
                    warmUpConfigs.Remove(key);
                    warmUpKeys.RemoveAt(idx);
                    if (warmUpKeys.Count > 0) idx %= warmUpKeys.Count;
                }
                else
                {
                    idx = (idx + 1) % warmUpKeys.Count;
                }

                yield return null;
            }
            globalWarmUpCoroutine = null;
        }

        
    }
}