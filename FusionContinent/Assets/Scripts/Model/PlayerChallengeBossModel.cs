using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.Model
{
    public class PlayerChallengeBoss
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BossId { get; set; }
        public Int64 TotalScore { get; set; }
        public Int64 TotalDamage { get; set; }
        public int DailyChallengeNum { get; set; }
        public int TotalChallengeNum { get; set; }
        public string ScoreRewards { get; set; }
        public string DailyRewards { get; set; }
        public List<int> ScoreRewardList { get; set; }
        public List<int> DailyRewardList { get; set; }
        public ChallengeBossConfig Config { get; set; }

        public PlayerChallengeBoss()
        {
        }

        public PlayerChallengeBoss(PlayerChallengeBoss v)
        {
            Id = v.Id;
            UserId = v.UserId;
            BossId = v.BossId;
            Config = DataConfigManager.Tables.TbChallengeBossConfig.GetOrDefault(BossId);
            if (Config == null)
            {
                throw new Exception($"不存在的挑战Boss配置[{BossId}]");
            }

            TotalScore = v.TotalScore;
            TotalDamage = v.TotalDamage;
            DailyChallengeNum = v.DailyChallengeNum;
            TotalChallengeNum = v.TotalChallengeNum;
            ScoreRewards = v.ScoreRewards ?? "[]";
            DailyRewards = v.DailyRewards ?? "[]";
            ScoreRewardList = JsonConvert.DeserializeObject<List<int>>(ScoreRewards);
            DailyRewardList = JsonConvert.DeserializeObject<List<int>>(DailyRewards);
        }
        
        
        /**
         * 获取伤害转换积分
         */
        public Int64 GetDamageToScore(Int64 damage)
        {
            Int64 score = 0;
            if (Config.DamageToScoreList.Count > 0)
            {
                foreach (var v in Config.DamageToScoreList.Where(v => damage >= v.Damage))
                {
                    score = v.Score;
                }
            }

            return score;
        }
    }

    public abstract class ChallengeBossModel
    {
        public static int GetStageToBossId(int stageId)
        {
            var bossList = DataConfigManager.Tables.TbChallengeBossConfig.DataList;
            return (from cfg in bossList 
                where cfg.Status == ActivityStatusType.Open && cfg.StageId == stageId select cfg.Id).FirstOrDefault();
        }
    }

    public class ChallengeBossResult
    {
        public PlayerChallengeBoss Boss { get; set; } 
        public ItemGroup Items { get; set; } // 同步道具
        public Dictionary<string, int> Rewards { get; set; } // 奖励
    }
}