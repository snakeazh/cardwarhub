using FusionContinent;
using FusionContinent.Attribute;
using UnityEngine;

/// <summary>
/// 属性初始化模块 - 重置所有属性数据
/// </summary>
public class SceneInitAttributes : SceneMapBase
{
    public override string ModuleName => "属性初始化";

    protected override void OnStartLoad()
    {
        var gamePlayManager = GamePlayManager.Default;
        if (gamePlayManager == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        // 清空模块属性
        gamePlayManager.ModuleAttributes = new AttributeSet();
        gamePlayManager.MonsterAttributes = new AttributeSet();
        gamePlayManager.ModuleMainAttributes = new();
        gamePlayManager.ClearBattleLegacySkillState();

        // 重置怪物计数
        gamePlayManager.enemyCount = 0;
        gamePlayManager.gameTime = 0f;
        
        // 设置游戏基础参数
        Application.targetFrameRate = DataConfigManager.Tables.TbGameBattleConst.GameFps;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        progress = 1.0f;
    }
}
