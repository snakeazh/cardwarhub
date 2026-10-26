using System.Collections.Generic;
using UnityEngine;

public class BattleItemPoolManager : BaseItemPoolManager
{
    public static BattleItemPoolManager Instance { get; private set; }

    protected override List<PredefinedPoolConfig> BuildDefaultPoolConfigs()
    {
        return new List<PredefinedPoolConfig>
        {
            new PredefinedPoolConfig { PoolName = "coin", MaxCount = 60, PrewarmCount = 15 },
            new PredefinedPoolConfig { PoolName = "Damage",MaxCount =  100, PrewarmCount = 15 },
            new PredefinedPoolConfig { PoolName = "CityDamage",MaxCount =  100, PrewarmCount = 15 },
            new PredefinedPoolConfig(){PoolName = "BondEffectGet01",MaxCount = 2,PrewarmCount = 1},
        };
    }

    public void AddBattlePool(string poolName, int maxCount = 120, int prewarmCount = 0)
    {
        AddPredefinedPool(poolName, maxCount, prewarmCount);
    }

    public void AddBattlePools(IEnumerable<string> poolNames, int maxCount = 120, int prewarmCount = 0)
    {
        AddPredefinedPools(poolNames, maxCount, prewarmCount);
    }

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        base.Awake();
    }

    protected override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDestroy();
    }
}
