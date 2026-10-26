using System.Collections.Generic;
using Newtonsoft.Json;

namespace FusionContinent.Model
{
    public class PlayerChallenge
    {
        public int Id { get; set; } // 主键
        public int DailyGemFusionNum { get; set; } // 每日融合游戏次数
        public string GemFusionRewards { get; set; } // 融合游戏奖励
        public List<int> GemFusionRewardList { get; set; } // 融合游戏奖励

        public PlayerChallenge()
        {
        }

        public PlayerChallenge(PlayerChallenge v)
        {
            Id = v.Id;
            GemFusionRewards = v.GemFusionRewards;
            DailyGemFusionNum = v.DailyGemFusionNum;
            GemFusionRewardList = JsonConvert.DeserializeObject<List<int>>(v.GemFusionRewards);
        }
    }
    
    public class GemGameResult
    {
        public PlayerChallenge Challenge { get; set; }
        public ItemGroup Items { get; set; } // 同步道具
        public Dictionary<string, int> Rewards { get; set; } // 奖励
    }
    
}