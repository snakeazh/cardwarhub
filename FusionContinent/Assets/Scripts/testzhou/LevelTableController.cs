using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using UnityEngine;

public class LevelTableController : MonoBehaviour
{
    private TbStageBattleConfig TbStageBattleConfig;

    // public LevelTable LevelTable;
    // Start is called before the first frame update
    void Awake()
    {
        Global.LevelStage.Clear();
        TbStageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
        for (int i = 0; i < TbStageBattleConfig.DataList.Count; i++)
        {
            Global.LevelStage.Add(TbStageBattleConfig.DataList[i].Id, false);
        }

        for (int i = 0; i < TbStageBattleConfig.DataList.Count; i++)
        {
            if (TbStageBattleConfig.DataList[i].Id == 3001001)
            {
                Global.LevelStage[TbStageBattleConfig.DataList[i].Id] = true;
            }
            if (!Global.PlayerStages.ContainsKey(TbStageBattleConfig.DataList[i].Id))
            {
            }
            else
            {
                if (Global.PlayerStages[TbStageBattleConfig.DataList[i].Id].Status == 1)
                {
                    // 判断下一关
                    var stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(TbStageBattleConfig.DataList[i].Id + 1);
                    if (stageCfg != null)
                    {
                        // 关卡id自增
                        Global.LevelStage[TbStageBattleConfig.DataList[i].Id+1] = true;
                    }
                    else
                    {
                        var chapter = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(DataConfigManager
                            .Tables
                            .TbStageBattleConfig.GetOrDefault(TbStageBattleConfig.DataList[i].Id).ChapterId + 1);
                        if (chapter != null)
                        {
                            Global.LevelStage[chapter.InitUnlockStageId] = true;
                        }
                    }

                    if (stageCfg != null && (TbStageBattleConfig.DataList[i].Id == DataConfigManager.Tables.TbChapterStageConfig
                            .GetOrDefault(DataConfigManager.Tables
                                .TbStageBattleConfig.GetOrDefault(TbStageBattleConfig.DataList[i].Id).ChapterId).InitUnlockStageId))
                    {
                        var nextChapterStageId = DataConfigManager.Tables.TbChapterStageConfig.GetOrDefault(
                            DataConfigManager
                                .Tables
                                .TbStageBattleConfig.GetOrDefault(TbStageBattleConfig.DataList[i].Id).ChapterId +
                            1).InitUnlockStageId;
                        if (Global.LevelStage.ContainsKey(nextChapterStageId))
                        {
                            Global.LevelStage[nextChapterStageId] = true;
                        }
                        else
                        {
                        }
                    }
                }
            }
        }
    }
}