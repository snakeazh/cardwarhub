using System.Collections.Generic;
using Config;

namespace FusionContinent.Model
{
    public class PlayerAtlas
    {
        public int Id { get; set; } // 用户ID
        public Dictionary<int, int> MonsterAtlasMap { get; set; }

        public PlayerAtlas()
        {
            MonsterAtlasMap = new Dictionary<int, int>();
        }
        
        public RewardStatus GetMonsterAtlasStatus(int monsterId)
        {
            if (MonsterAtlasMap != null && MonsterAtlasMap.TryGetValue(monsterId, out var status))
            {
                return (RewardStatus)status;
            }

            return RewardStatus.Default;
        }

        public bool CheckMonsterCanReceived(int monsterId)
        {
            return GetMonsterAtlasStatus(monsterId) == RewardStatus.CanReceive;
        }
    }

    
    public class PlayerAtlasResult
    {
        public PlayerAtlas PlayerAtlas { get; set; }
        public Dictionary<string, int> Rewards { get; set; } // 奖励内容
        public ItemGroup Items { get; set; } // 同步道具
    }
}