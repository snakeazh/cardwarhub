using Config;
using FusionContinent;
using UnityEngine;

/// <summary>
/// 贪婪预支卡初始化模块 - 在奥数卡加载后设置开局金币
/// </summary>
public class SceneInitGreedyAdvance : SceneMapBase
{
    public override string ModuleName => "贪婪预支卡初始化";

    protected override void OnStartLoad()
    {
        // 贪婪预支卡检测：此时奥数卡已通过 AddGeneralSkill 加载到 GeneralSkillManager
        bool hasGreedyAdvance = GeneralSkillManager.Instance.HasGreedyAdvanceCard;
        
        // 开局金币 = 初始值 + 额外获得的金币
        Global.GameGold.Value = DataConfigManager.Tables.TbGameBattleConst.InitialCapitalReserves;
        if (hasGreedyAdvance)
        {
            Global.GameGold.Value += DataConfigManager.Tables.TbGameBattleConst.SpecialArcCardGoldNumOne;
            // 初始化透支系统（初始债务为 SpecialArcCardGoldNumOne）
            GeneralSkillManager.Instance.InitOverdraftSystem();
            LogTool.Log($"[SceneInitGreedyAdvance] 检测到贪婪预支卡，开局金币={Global.GameGold.Value}（初始={DataConfigManager.Tables.TbGameBattleConst.InitialCapitalReserves} + 额外={DataConfigManager.Tables.TbGameBattleConst.SpecialArcCardGoldNumOne}），需偿还债务={DataConfigManager.Tables.TbGameBattleConst.SpecialArcCardGoldNumOne}");
        }
        
        progress = 1.0f;
    }
}
