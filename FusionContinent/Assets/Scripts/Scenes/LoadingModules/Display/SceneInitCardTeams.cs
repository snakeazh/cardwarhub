using System.Collections.Generic;
using FusionContinent;
using FusionContinent.Attribute;
using UnityEngine;

/// <summary>
/// 出战单位初始化模块 - 初始化士兵卡牌和技能卡牌
/// </summary>
public class SceneInitCardTeams : SceneMapBase
{
    public override string ModuleName => "出战单位初始化";

    protected override void OnStartLoad()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        // 初始化出战单位
        gpm.InitCardTeams();
        LogTool.Log("[SceneInitCardTeams] 出战单位初始化完成");

        progress = 1.0f;
    }
}
