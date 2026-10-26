using System;
using System.Collections.Generic;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.Model
{
    public class PlayerEndlessTower
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TowerId { get; set; }
        public int Floor { get; set; }
        public int MapId { get; set; }
        public string ClearNodes { get; set; }
        public string FloorRewards { get; set; }
        public string DailyRewards { get; set; }
        public int DailyChallengeNum { get; set; }
        public int TotalChallengeNum { get; set; }
        public Dictionary<int, int> ClearNodeList { get; set; }
        public List<int> FloorRewardList { get; set; }
        public List<int> DailyRewardList { get; set; }

        public PlayerEndlessTower()
        {
        }

        public PlayerEndlessTower(PlayerEndlessTower v)
        {
            Id = v.Id;
            UserId = v.UserId;
            TowerId = v.TowerId;
            Floor = v.Floor;
            MapId = v.MapId;
            DailyChallengeNum = v.DailyChallengeNum;
            TotalChallengeNum = v.TotalChallengeNum;
            
            ClearNodes = v.ClearNodes ?? "{}";
            DailyRewards = v.DailyRewards ?? "[]";
            FloorRewards = v.FloorRewards ?? "[]";
            
            ClearNodeList = JsonConvert.DeserializeObject<Dictionary<int, int>>(ClearNodes);
            FloorRewardList = JsonConvert.DeserializeObject<List<int>>(FloorRewards);
            DailyRewardList = JsonConvert.DeserializeObject<List<int>>(DailyRewards);
        }
    }

    public class EndlessTowerResult
    {
        public ItemGroup Items { get; set; } // 同步道具
        public Dictionary<string, int> Rewards { get; set; } // 奖励
        public PlayerEndlessTower Tower { get; set; } 
    }
}