using Config;
using FusionContinent;
using QFramework;
using UnityEngine;

/// <summary>
/// 关卡设置初始化模块 - 初始化关卡相关设置
/// </summary>
public class SceneInitStageSettings : SceneMapBase
{
    public override string ModuleName => "关卡设置初始化";

    protected override void OnStartLoad()
    {
        var gpm = GamePlayManager.Default;
        if (gpm == null)
        {
            ErrorMessage = "GamePlayManager.Default is null";
            return;
        }

        // 游戏难度
        Global.GameDifficultyLevel.Value = gpm.StageCfg.Difficulty;

        // 初始流派
        SchoolManager.Instance.Init();
        gpm._skillConfigManager.ClearLegacyCastOnlySkillGraphs();

        // 初始通用技能（奥数卡）
        if (GeneralSkillManager.Instance != null)
            GeneralSkillManager.Instance.Init();

        // 重置三选一缓存数据
        // UIKit.OpenPanel<SurpriseChooseCardPanel>().ResetData();

        progress = 1.0f;
    }
}
