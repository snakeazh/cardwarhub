using FusionContinent;
using UnityEngine;

/// <summary>
/// 开始刷怪模块 - 在所有初始化完成后执行，开始第一天刷怪
/// </summary>
public class SceneInitStartSpawning : SceneMapBase
{
    public override string ModuleName => "开始刷怪";

    protected override void OnStartLoad()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        if (gpm._monsterGenCfg == null)
        {
            ErrorMessage = "MonsterGenCfg is null";
            return;
        }

        // 初始化战场显隐与双侧挂点（默认 PVE 只开 pve）
        if (GameLand.Default != null)
        {
            GameLand.Default.InitializeBattleArena();
        }

        // PVP：Arena 就绪后，用 OpponentDetail 在对侧刷初始英灵
        gpm.InitOpponentCardTeams();

        // 初始化第一天刷怪（这会设置 _monsterGenDayList 并递增 Global.Date）
        gpm.InitFirstDay();
        LogTool.Log("[SceneInitStartSpawning] 第一天刷怪初始化完成");

        progress = 1.0f;
    }
}
