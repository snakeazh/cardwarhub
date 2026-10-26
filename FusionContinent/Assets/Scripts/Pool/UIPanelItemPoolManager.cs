using System.Collections.Generic;
using UnityEngine;

public class UIPanelItemPoolManager : BaseItemPoolManager
{
    public static UIPanelItemPoolManager Instance { get; private set; }

    protected override List<PredefinedPoolConfig> BuildDefaultPoolConfigs()
    {
        return new List<PredefinedPoolConfig>
        {
            new PredefinedPoolConfig { PoolName = "CardGroupSlot", MaxCount = 50, PrewarmCount = 10 },
            new PredefinedPoolConfig { PoolName = "SoldierCardGroupSlot", MaxCount = 20, PrewarmCount = 5 },
            
            new PredefinedPoolConfig { PoolName = "ConfirmDialogPanel", MaxCount = 3, PrewarmCount = 1 },
            new PredefinedPoolConfig { PoolName = "ShopItemTitleObj", MaxCount = 20, PrewarmCount = 5 },
            // new PredefinedPoolConfig { PoolName = "SoldierCardGroupSlot", MaxCount = 20, PrewarmCount = 5 },
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
