using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Rules
{
    public enum ERedArrowType
    {
        Left,
        Right,
    }

    public static class SoldierRedDotHelper
    {
        static List<ItemConfig> _soldierExpConsumeItems;
        static List<int> _soldierCardIds;

        public static IReadOnlyList<int> GetSoldierCardIds()
        {
            if (_soldierCardIds == null)
            {
                _soldierCardIds = new List<int>();
                foreach (CardBaseConfig cfg in DataConfigManager.Tables.TbCardBaseConfig.DataList)
                {
                    if (cfg.Type == CardType.Soldier)
                    {
                        _soldierCardIds.Add(cfg.Id);
                    }
                }
            }

            return _soldierCardIds;
        }

        public static IReadOnlyList<ItemConfig> SoldierExpConsumeItems
        {
            get
            {
                if (_soldierExpConsumeItems == null)
                {
                    _soldierExpConsumeItems = DataConfigManager.Tables.TbItemConfig.DataList
                        .Where(t => t.Type == ItemType.SoldierExp)
                        .ToList();
                }

                return _soldierExpConsumeItems;
            }
        }

        public static bool CheckSoldierUpgrade(PlayerCard cardInfo)
        {
            CardDevelopConfig developCfg = DataConfigManager.Tables.TbCardDevelopConfig.GetOrDefault(cardInfo.CardId);
            if (cardInfo.Lv >= developCfg.LevelPropertyList.Count || !cardInfo.IsHave)
            {
                return false;
            }

            bool hasAwakening = developCfg.LevelPropertyList
                .Where(x => x.Level >= cardInfo.Lv)
                .OrderBy(x => x.Level)
                .FirstOrDefault(x => x.AwakeningConsume.Any()) != null;
            int awakingLevel = 0;
            if (hasAwakening)
            {
                DevelopLevelPropertyList awakeningLevel = developCfg.LevelPropertyList
                    .Where(x => x.Level >= cardInfo.Lv)
                    .OrderBy(x => x.Level)
                    .First(x => x.AwakeningConsume.Any());
                awakingLevel = awakeningLevel.Level;
            }

            bool canAwakening = cardInfo.Lv >= awakingLevel;
            if (canAwakening)
            {
                return false;
            }

            DevelopLevelPropertyList lvCfg = developCfg.LevelPropertyList[cardInfo.Lv - 1];
            // 消耗判断与 SoldierPanelViewModel.CheckCanUpgrade / UpGradeBtn.interactable 对齐：
            // 逐项检查当前等级升级所需消耗道具是否足够，而非统计经验药水总经验值。
            // 否则会出现「经验池够但具体道具不足」时红点亮、按钮却点不动的不一致。
            return Items.CheckEnough(lvCfg.ConsumeItems);
        }

        public static bool CheckSoldierDevelop(int cardId)
        {
            PlayerCard cardInfo = Global.GetCardById(cardId);
            if (cardInfo == null)
            {
                return false;
            }

            CardDevelopConfig developCfg = DataConfigManager.Tables.TbCardDevelopConfig.GetOrDefault(cardInfo.CardId);
            List<DevelopLevelPropertyList> developRewCfg = developCfg.LevelPropertyList
                .Where(t => cardInfo.Lv >= t.Level && t.TakeRewards.Count > 0)
                .ToList();

            for (int i = 0; i < developRewCfg.Count; i++)
            {
                DevelopLevelPropertyList cfg = developRewCfg[i];
                if (cardInfo.ObjData.ReceiveDevelops == null || !cardInfo.ObjData.ReceiveDevelops.Contains(cfg.Level))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 未拥有英灵是否已满足 ActivationCost（可解锁）。
        /// </summary>
        public static bool CanActivateSoldier(int cardId)
        {
            if (cardId <= 0)
            {
                return false;
            }

            PlayerCard card = Global.GetCardById(cardId);
            if (card != null && card.IsHave)
            {
                return false;
            }

            SoldierBattleConfig soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(cardId);
            if (soldierCfg == null || soldierCfg.ActivationCost == null || soldierCfg.ActivationCost.Count == 0)
            {
                return false;
            }

            return Items.CheckEnough(soldierCfg.ActivationCost);
        }

        public static int CountActivatableSoldiers()
        {
            IReadOnlyList<int> soldierCardIds = GetSoldierCardIds();
            int count = 0;
            for (int i = 0; i < soldierCardIds.Count; i++)
            {
                if (CanActivateSoldier(soldierCardIds[i]))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 英灵是否可觉醒：已拥有、未觉醒、等级正好在觉醒档，且 AwakeningConsume 材料足够。
        /// 与 AwakeningPanelViewModel.CanAwaken + CanExecuteAwakening 对齐。
        /// </summary>
        public static bool CheckSoldierAwakening(PlayerCard card)
        {
            if (card == null || !card.IsHave || card.ObjData == null || card.ObjData.IsAwaked)
            {
                return false;
            }

            CardDevelopConfig developCfg = DataConfigManager.Tables.TbCardDevelopConfig.GetOrDefault(card.CardId);
            if (developCfg == null || developCfg.LevelPropertyList == null)
            {
                return false;
            }

            DevelopLevelPropertyList awakeningLevel = developCfg.LevelPropertyList
                .Where(x => x.Level >= card.Lv && x.AwakeningConsume != null && x.AwakeningConsume.Count > 0)
                .OrderBy(x => x.Level)
                .FirstOrDefault();

            if (awakeningLevel == null || card.Lv != awakeningLevel.Level)
            {
                return false;
            }

            return Items.CheckEnough(awakeningLevel.AwakeningConsume);
        }

        public static bool CheckSoldierAwakening(int cardId)
        {
            if (cardId <= 0 || !Global.PlayerCards.TryGetValue(cardId, out PlayerCard card))
            {
                return false;
            }

            return CheckSoldierAwakening(card);
        }

        public static int CountAwakenableSoldiers()
        {
            IReadOnlyList<int> soldierCardIds = GetSoldierCardIds();
            int count = 0;
            for (int i = 0; i < soldierCardIds.Count; i++)
            {
                if (!Global.PlayerCards.TryGetValue(soldierCardIds[i], out PlayerCard card))
                {
                    continue;
                }

                if (CheckSoldierAwakening(card))
                {
                    count++;
                }
            }

            return count;
        }

        public static Dictionary<int, PlayerCard> CollectOwnedSoldiers(IReadOnlyList<int> soldierCardIds)
        {
            Dictionary<int, PlayerCard> soldiers = new Dictionary<int, PlayerCard>();
            for (int i = 0; i < soldierCardIds.Count; i++)
            {
                int cfg = soldierCardIds[i];
                if (Global.PlayerCards.TryGetValue(cfg, out PlayerCard card))
                {
                    soldiers.TryAdd(cfg, card);
                }
            }

            return soldiers;
        }

        public static Dictionary<int, PlayerCard> CollectSoldiersBeforeSelected(
            IReadOnlyList<int> soldierCardIds,
            int selectedSoldierId,
            ERedArrowType arrowType)
        {
            Dictionary<int, PlayerCard> soldiers = new Dictionary<int, PlayerCard>();
            if (arrowType == ERedArrowType.Right)
            {
                for (int i = soldierCardIds.Count - 1; i >= 0; i--)
                {
                    int cfg = soldierCardIds[i];
                    if (cfg == selectedSoldierId)
                    {
                        break;
                    }

                    if (Global.PlayerCards.TryGetValue(cfg, out PlayerCard card))
                    {
                        soldiers.TryAdd(cfg, card);
                    }
                }
            }
            else
            {
                for (int i = 0; i < soldierCardIds.Count; i++)
                {
                    int cfg = soldierCardIds[i];
                    if (cfg == selectedSoldierId)
                    {
                        break;
                    }

                    if (Global.PlayerCards.TryGetValue(cfg, out PlayerCard card))
                    {
                        soldiers.TryAdd(cfg, card);
                    }
                }
            }

            return soldiers;
        }
    }
}
