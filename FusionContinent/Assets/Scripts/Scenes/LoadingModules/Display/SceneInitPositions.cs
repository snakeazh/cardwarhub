using FusionContinent;
using UnityEngine;

/// <summary>
/// 位置列表初始化模块 - 初始化怪物和友方单位位置
/// </summary>
public class SceneInitPositions : SceneMapBase
{
    public override string ModuleName => "位置列表初始化";

    protected override void OnStartLoad()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        if (GameLand.Default == null)
        {
            ErrorMessage = "GameLand.Default is null";
            return;
        }

        // 清空位置列表
        gpm.ordinaryPositions.Clear();
        gpm.elitePositions.Clear();
        gpm.bossPositions.Clear();

        // 初始化怪物位置
        foreach (Transform child in GameLand.Default.MonsterPos.transform)
        {
            if (child.name.Contains("OrdinaryPos"))
            {
                gpm.ordinaryPositions.Add(child);
            }
            else if (child.name.Contains("ElitePos"))
            {
                gpm.elitePositions.Add(child);
            }
            else if (child.name.Contains("BossPos"))
            {
                gpm.bossPositions.Add(child);
            }
        }

        // 获取士兵坐标
        var friendlySlot = GameLand.Default.FriendlySlot.transform;
        int posNum = friendlySlot.childCount;
        gpm.friendlyPositions = new Transform[posNum];
        gpm.friendlyPositionsLocked = new bool[posNum];

        for (int i = 0; i < posNum; i++)
        {
            gpm.friendlyPositions[i] = friendlySlot.GetChild(i);
            gpm.friendlyPositionsLocked[i] = false;
        }

        // 初始化城墙数据
        GameLand.Default.wall.InitData();

        progress = 1.0f;
    }
}
