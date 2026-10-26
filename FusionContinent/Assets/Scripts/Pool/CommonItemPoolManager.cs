using System.Collections.Generic;
using UnityEngine;

public class CommonItemPoolManager : BaseItemPoolManager
{
    public static CommonItemPoolManager Instance { get; private set; }

    protected override List<PredefinedPoolConfig> BuildDefaultPoolConfigs()
    {
        return new List<PredefinedPoolConfig>
        {
            new PredefinedPoolConfig { PoolName = "RewardEntity", MaxCount = 40, PrewarmCount = 10 },
            new PredefinedPoolConfig { PoolName = "ShopItemTitleObj", MaxCount = 20, PrewarmCount = 5 },
            new PredefinedPoolConfig { PoolName = "BackPackItem", MaxCount = 50, PrewarmCount = 20 },
            new PredefinedPoolConfig { PoolName = "CombatTip", MaxCount = 2, PrewarmCount = 2 },
            new PredefinedPoolConfig { PoolName = "ShopItem", MaxCount = 20, PrewarmCount = 10 },
            new PredefinedPoolConfig { PoolName = "ShopItem2", MaxCount = 20, PrewarmCount = 10 },
        };
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
