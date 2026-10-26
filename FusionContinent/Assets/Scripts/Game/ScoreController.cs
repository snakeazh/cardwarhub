using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;
using QFramework;
using UnityEngine;

public class ScoreController : MonoSingleton<ScoreController>
{
    public int score = 0;
    public int scoreTime = 0;
    private int AllScore;
    private readonly Dictionary<int, int> _killMonsters = new Dictionary<int, int>();
    
    private TbMonsterGenConfig _tbMonsterGenConfig;
    private TbStageScoreConfig _tbStageScoreConfig;
    private TbStageBattleConfig _tbStageBattleConfig;

    private int gametime;
    private void Awake()
    {
        _tbMonsterGenConfig= DataConfigManager.Tables.TbMonsterGenConfig;
        _tbStageScoreConfig= DataConfigManager.Tables.TbStageScoreConfig;
        _tbStageBattleConfig= DataConfigManager.Tables.TbStageBattleConfig;
    }

    public void AddScore(float time, int stageId, int day)
    {
        score = 0;
        var stageCfg = _tbStageBattleConfig.GetOrDefault(stageId);
        switch (stageCfg.StageType)
        {
            case StageType.ChallengeBoss:
                var bossId = ChallengeBossModel.GetStageToBossId(stageCfg.Id);
                score = (int)Global.PlayerChallengeBoss[bossId].GetDamageToScore(Global.TotalDamage.Value);
                break;
            
            default:
                score = _tbMonsterGenConfig.Get(stageId).StageList.FirstOrDefault(item => item.Day == day)!.WaveList.Last().Score;
                break;
        }
        
        scoreTime = 0;
        gametime=(int)time;
        scoreTime = GetTimeRangeIndex(stageId, time);
        AllScore = Global.PlayerBattle.Score + score+scoreTime;
        
        // 请求服务器
        Global.PlayerBattle.CurrentStageId = stageId;
        Global.PlayerBattle.ClearStageTime =(int)time;
        Global.PlayerBattle.Score = AllScore;
        

    }

    public int GetScore()
    {
        return score;
    }
    public int GetScoreTime()
    {
        return scoreTime;
    }

    public int GetTime()
    {
        return gametime;
    }

    // 根据时间范围返回对应的索引
    
    public  int GetTimeRangeIndex(int uid,float time)
    {
        List<int> scoretb = _tbStageBattleConfig.Get(uid).ExtraScores;
        int i = 0;
        for (; i < scoretb.Count; i++)
        {
            if (time >= _tbStageScoreConfig.Get(scoretb[i]).TimeRange[0] &&
                time <= _tbStageScoreConfig.Get(scoretb[i]).TimeRange[1])
            {
                return _tbStageScoreConfig.Get(scoretb[i]).Score;
            }
            else
            {
                continue;
            }
        }
        return 0;
    }

    public Dictionary<int, int> GetKillMonsters()
    {
        return _killMonsters;
    }

    public bool AddKillMonster(int monsterId)
    {
        return _killMonsters.TryAdd(monsterId, (int)RewardStatus.CanReceive);
    }
}
