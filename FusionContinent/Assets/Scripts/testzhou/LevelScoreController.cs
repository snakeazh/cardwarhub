using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using UnityEngine;

public class LevelScoreController : MonoBehaviour
{
    private TbStageBattleConfig TbStageBattleConfig;
    //public LevelScoreSituationTable LevelTable;
    // Start is called before the first frame update
    void Start()
    {
       // TbStageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
       // for (int i = 0; i < TbStageBattleConfig.DataList.Count; i++)
       // {
       //     if (!Global.PlayerStages.ContainsKey(TbStageBattleConfig.DataList[i].Id))
       //     {
       //         PlayerStage levelData = new PlayerStage();
       //         levelData.Id = TbStageBattleConfig.DataList[i].Id;
       //         levelData.ClearStageTime = 0;
       //         levelData.Score = 0;
       //         Global.PlayerStages.Add(TbStageBattleConfig.DataList[i].Id, levelData);
       //     }
       // }
    }

}
