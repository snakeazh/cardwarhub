using System.Collections.Generic;
using Config;

namespace FusionContinent.Model
{
    public class RewardItem
    {
        public int Id { get; set; } // 奖励 ID
        public ItemType Type { get; set; } // 奖励类型
        public long Num { get; set; } // 奖励数量

        public RewardItem(int id, long num, ItemType type = 0)
        {
            Id = id;
            Num = num;
            Type = type;
        }
    }
    
    public static class RewardModel
    {
        public static List<RewardItem> ToRewardItems(List<RewardAndNum> rewList, int multipleNum = 1)
        {
            var rewards = new List<RewardItem>();
            foreach (var rew in rewList)
            {
                rewards.Add(new RewardItem(rew.Id, rew.Num * multipleNum));
            }

            return rewards;
        }
        
    }
}