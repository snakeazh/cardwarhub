using FusionContinent;
using FusionContinent.Model;

namespace Game.Equip.RedDot
{
    /// <summary>
    /// 融合石可突破（升阶/compound）判定：主石未满阶，且存在可吞噬的未装备次石。
    /// 吞噬规则与 FusionGemCompoundPanel 一致：同 GemId、阶数 ∈ [主阶-3, 主阶]、阶差 ≤ 4。
    /// </summary>
    public static class FusionGemBreakthroughRedDotHelper
    {
        const int FusionConsumeTierDownLimit = 3;
        const int FusionConsumeMaxTierGap = 4;

        public static bool CanBeConsumedAsSecondary(PlayerFusionGem mainGem, PlayerFusionGem candidateGem)
        {
            if (mainGem == null || candidateGem == null)
            {
                return false;
            }

            if (candidateGem.Id == mainGem.Id)
            {
                return false;
            }

            if (candidateGem.GemId != mainGem.GemId)
            {
                return false;
            }

            int mainTier = mainGem.Level;
            int candidateTier = candidateGem.Level;
            int minTier = mainTier - FusionConsumeTierDownLimit;
            if (minTier < 1)
            {
                minTier = 1;
            }

            if (candidateTier < minTier || candidateTier > mainTier)
            {
                return false;
            }

            if (mainTier - candidateTier > FusionConsumeMaxTierGap)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 合成面板列表红点：未装备且可被当前主石吞噬。
        /// </summary>
        public static bool IsUnequippedConsumableSecondary(PlayerFusionGem mainGem, PlayerFusionGem candidateGem)
        {
            if (candidateGem == null || GlobalFunc.IsFusionGemEquipped(candidateGem))
            {
                return false;
            }

            return CanBeConsumedAsSecondary(mainGem, candidateGem);
        }

        public static bool CanBreakthrough(PlayerFusionGem mainGem)
        {
            if (mainGem == null || mainGem.IsMaxLevel())
            {
                return false;
            }

            if (Global.PlayerFusionGems == null)
            {
                return false;
            }

            // 必须有未装备的可吞噬次石，才提示可突破（已装备石互吞不提示）
            foreach (PlayerFusionGem gem in Global.PlayerFusionGems.Values)
            {
                if (IsUnequippedConsumableSecondary(mainGem, gem))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanBreakthrough(int gemOnlyId)
        {
            if (gemOnlyId <= 0 || Global.PlayerFusionGems == null)
            {
                return false;
            }

            if (!Global.PlayerFusionGems.TryGetValue(gemOnlyId, out PlayerFusionGem gem))
            {
                return false;
            }

            return CanBreakthrough(gem);
        }

        public static int CountBreakthroughGems()
        {
            if (Global.PlayerFusionGems == null)
            {
                return 0;
            }

            int count = 0;
            foreach (PlayerFusionGem gem in Global.PlayerFusionGems.Values)
            {
                if (CanBreakthrough(gem))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
