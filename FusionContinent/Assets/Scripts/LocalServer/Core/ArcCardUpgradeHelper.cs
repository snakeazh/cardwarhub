using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 奥术卡（CardType.GeneralSkill）获得的统一处理：Num 恒 1，重复获得 Lv+1，
    /// 满级（TbSpecialEffectsConfig.LevelPropertyList 档数）不再升级，转 TbArcCardProConfig.DecomposeRewards 分解产物。
    /// 由统一道具入口（ItemEntrance）调用（抽奖模块已移除，原 ArcCardDrawHandler 已删）。
    /// 说明：Go 端（PlayerCardService.go:109-136）重复获得叠加 Num；去佩戴改造后改为升级，属有意偏差。
    /// </summary>
    public class ArcCardUpgradeHelper
    {
        public enum ObtainKind
        {
            NewCard,            // 未拥有，建档 Lv=1/Num=1
            Upgraded,           // 已拥有未满级，Lv+1
            MaxLevelDecomposed, // 已满级，不升级，转分解产物
        }

        public class ObtainOutcome
        {
            public ObtainKind Kind;
            public PlayerCard Card;                 // 变动卡（满级分解时为未变动的满级卡）
            public List<Config.RewardAndNum> DecomposeRewards; // 满级分解产物（由调用方发放，可能为 null）
        }

        /// <summary>对存档应用一次「获得奥术卡」。</summary>
        public ObtainOutcome ApplyObtained(LocalServerContext ctx, int cardId)
        {
            PlayerCard card = null;
            foreach (PlayerCard c in ctx.Store.Archive.Cards)
            {
                if (c.CardId == cardId)
                {
                    card = c;
                    break;
                }
            }

            var outcome = new ObtainOutcome();
            if (card == null)
            {
                card = new PlayerCard
                {
                    Id = NextCardId(ctx),
                    UserId = ctx.UserId,
                    CardId = cardId,
                    Lv = 1, // 对照 PlayerCardService.go:144 初始 Lv 恒为 1
                    Num = 1, // 去佩戴改造：奥术卡 Num 恒 1
                    Data = new PlayerCard.Types.CardData(),
                };
                ctx.Store.Archive.Cards.Add(card);
                outcome.Kind = ObtainKind.NewCard;
                outcome.Card = card;
                return outcome;
            }

            if (card.Lv < GetMaxLevel(cardId))
            {
                card.Lv += 1;
                outcome.Kind = ObtainKind.Upgraded;
                outcome.Card = card;
                return outcome;
            }

            // 满级：不升级，转分解产物；品质取 LevelPropertyList[0].Quality（对齐 GeneralCardResolvePanelViewModel:156-161）
            outcome.Kind = ObtainKind.MaxLevelDecomposed;
            outcome.Card = card;
            SpecialEffectsConfig skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cardId);
            ArcCardProConfig arcCfg = DataConfigManager.Tables.TbArcCardProConfig.GetOrDefault(skillCfg.LevelPropertyList[0].Quality);
            if (arcCfg != null)
            {
                outcome.DecomposeRewards = arcCfg.DecomposeRewards;
            }
            return outcome;
        }

        /// <summary>满级档数 = TbSpecialEffectsConfig[cardId].LevelPropertyList 档数。</summary>
        public int GetMaxLevel(int cardId)
        {
            SpecialEffectsConfig skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cardId);
            if (skillCfg == null || skillCfg.LevelPropertyList.Count == 0)
            {
                LogTool.LogError($"[ArcCardUpgradeHelper] 奥术卡[{cardId}]缺少SpecialEffects配置，按无满级处理");
                return int.MaxValue;
            }
            return skillCfg.LevelPropertyList.Count;
        }

        // 本地替代 Go DB 自增主键：取现有最大 Id + 1（同 ItemEntrance.NextCardId）
        private int NextCardId(LocalServerContext ctx)
        {
            int max = 0;
            foreach (PlayerCard card in ctx.Store.Archive.Cards)
            {
                if (card.Id > max)
                {
                    max = card.Id;
                }
            }
            return max + 1;
        }
    }
}
