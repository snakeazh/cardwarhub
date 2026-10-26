using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Soldier.RedDot
{
    /// <summary>
    /// 奥术卡合成可执行判定：对应品质 ComposeRewards 材料足够。
    /// </summary>
    public static class ArcCardCombineRedDotHelper
    {
        public static readonly QualityType[] ComposeQualities =
        {
            QualityType.Ultimate,
            QualityType.Legend,
            QualityType.Epic,
            QualityType.Rarity,
            QualityType.Ordinary,
        };

        public static bool CanCombine(QualityType quality)
        {
            ArcCardProConfig cfg = DataConfigManager.Tables.TbArcCardProConfig.GetOrDefault(quality);
            if (cfg == null || cfg.ComposeRewards == null || cfg.ComposeRewards.Count == 0)
            {
                return false;
            }

            return Items.CheckEnough(cfg.ComposeRewards);
        }

        public static int CountCombinable()
        {
            int count = 0;
            for (int i = 0; i < ComposeQualities.Length; i++)
            {
                if (CanCombine(ComposeQualities[i]))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
