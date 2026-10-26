using FusionContinent;
using UnityEngine;

/// <summary>
/// 建筑槽位初始化模块 - 初始化建筑槽位相关数据
/// </summary>
public class SceneInitBuildingSlots : SceneMapBase
{
    public override string ModuleName => "建筑槽位初始化";

    protected override void OnStartLoad()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        gpm.buildingDic.Clear();
        gpm.buildingLock.Clear();

        // 建筑卡槽已从 GameLand 场景移除 / 未绑定时，跳过槽位扫描，避免卡死加载流程
        if (GameLand.Default == null || GameLand.Default.CardSlot == null)
        {
            LogTool.LogWarning("[建筑槽位初始化] GameLand.CardSlot 未绑定，跳过建筑槽位扫描");
            Global.BuildUnLock.Value = 2;
            progress = 1.0f;
            return;
        }

        var cardSlot = GameLand.Default.CardSlot.transform;
        for (int i = 0; i < cardSlot.childCount; i++)
        {
            gpm.buildingDic.Add(i, null);
            Transform child = cardSlot.GetChild(i);
            Transform lockingObject = child.Find("LockIng");
            gpm.buildingLock.Add(i, lockingObject);
        }

        Global.BuildUnLock.Value = 2;

        progress = 1.0f;
    }
}
