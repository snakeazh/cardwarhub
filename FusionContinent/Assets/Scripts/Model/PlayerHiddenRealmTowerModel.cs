using System;
using System.Collections.Generic;
using Config;
using Newtonsoft.Json;

namespace FusionContinent.Model
{
    public class HiddenRealmFloorEntryData
    {
        public int EntryType { get; set; }
        public int Level { get; set; }
    }

    public class HiddenRealmFloorData
    {
        public int Floor { get; set; }
        public List<HiddenRealmFloorEntryData> Entries { get; set; } = new List<HiddenRealmFloorEntryData>();
        public bool Cleared { get; set; }
    }

    public class PlayerHiddenRealmTower
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TowerId { get; set; }
        public int Floor { get; set; }
        public string FloorRewards { get; set; }
        public string DailyRewards { get; set; }
        public int DailyChallengeNum { get; set; }
        public int TotalChallengeNum { get; set; }
        public int DailyStageSweepingNum { get; set; }
        public List<int> FloorRewardList { get; set; }
        public List<int> DailyRewardList { get; set; }
        public List<HiddenRealmFloorData> Floors { get; set; } = new List<HiddenRealmFloorData>();

        public PlayerHiddenRealmTower()
        {
        }

        public PlayerHiddenRealmTower(PlayerHiddenRealmTower v)
        {
            Id = v.Id;
            UserId = v.UserId;
            TowerId = v.TowerId;
            Floor = v.Floor;
            DailyChallengeNum = v.DailyChallengeNum;
            TotalChallengeNum = v.TotalChallengeNum;
            DailyStageSweepingNum = v.DailyStageSweepingNum;
            
            DailyRewards = v.DailyRewards ?? "[]";
            FloorRewards = v.FloorRewards ?? "[]";
            
            FloorRewardList = JsonConvert.DeserializeObject<List<int>>(FloorRewards);
            DailyRewardList = JsonConvert.DeserializeObject<List<int>>(DailyRewards);
            
            Floors = v.Floors ?? new List<HiddenRealmFloorData>();
        }
    }

    public class HiddenRealmTowerResult
    {
        public ItemGroup Items { get; set; } // 同步道具
        public Dictionary<string, int> Rewards { get; set; } // 奖励
        public PlayerHiddenRealmTower Tower { get; set; } 
    }
}