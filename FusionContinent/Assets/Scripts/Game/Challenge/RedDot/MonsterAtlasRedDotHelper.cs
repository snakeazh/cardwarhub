using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;

namespace Game.Challenge.RedDot
{
    /// <summary>
    /// 怪物图鉴红点：以 RewardStatus.CanReceive（解锁后可领奖励）为真值源。
    /// </summary>
    public static class MonsterAtlasRedDotHelper
    {
        public static bool CanReceive(int monsterId)
        {
            return Global.PlayerAtlas.GetMonsterAtlasStatus(monsterId) == RewardStatus.CanReceive;
        }

        public static int CountCanReceive()
        {
            int count = 0;
            foreach (System.Collections.Generic.KeyValuePair<int, int> entry in Global.PlayerAtlas.MonsterAtlasMap)
            {
                if (entry.Value == (int)RewardStatus.CanReceive)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountCanReceiveByQuality(MonsterQuality quality)
        {
            int count = 0;
            foreach (System.Collections.Generic.KeyValuePair<int, int> entry in Global.PlayerAtlas.MonsterAtlasMap)
            {
                if (entry.Value != (int)RewardStatus.CanReceive)
                {
                    continue;
                }

                MonsterBattleConfig cfg = DataConfigManager.Tables.TbMonsterBattleConfig.GetOrDefault(entry.Key);
                if (cfg != null && cfg.MonsterQuality == quality)
                {
                    count++;
                }
            }

            return count;
        }

        public static void MarkDirty()
        {
            RedDotExtensions.MarkRedDotDirty(RedNodeType.MonsterAtlasBtn);
        }
    }
}
