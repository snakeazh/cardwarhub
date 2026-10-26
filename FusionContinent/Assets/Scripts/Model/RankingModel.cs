using System;
using System.Collections.Generic;
using Config;
using Newtonsoft.Json;
using UnityEngine;

namespace FusionContinent.Model
{
    public class UserRankingData
    {
        public int Rank { get; set; }
        public Int64 Score { get; set; }
        public int UserId { get; set; }
        public int Level { get; set; }
        public int ClearStageId { get; set; }
        public int ClearStageTime { get; set; }
        public string UserName { get; set; }
        public string Avatar { get; set; }
        public string Figure { get; set; }
    }

    // 返回数据
    public class RankingResult
    {
        public List<UserRankingData> List { get; set; } 
        public UserRankingData MyRank { get; set; }
    }
    
}