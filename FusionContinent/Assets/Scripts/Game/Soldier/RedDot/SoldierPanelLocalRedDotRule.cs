using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;
using Game.RedDot.Rules;

namespace Game.Soldier.RedDot
{
    public sealed class SoldierPanelLocalRedDotRule : LocalRedDotRuleBase
    {
        public override string Scope => SoldierPanelRedDotScope.ScopeName;

        protected override void OnBind()
        {
            IReadOnlyList<ItemConfig> consumeItems = SoldierRedDotHelper.SoldierExpConsumeItems;
            for (int i = 0; i < consumeItems.Count; i++)
            {
                Global.Publisher.Subscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{consumeItems[i].Id}", OnDataChanged);
            }

            Global.Publisher.Subscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnDataChanged);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnDataChanged);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_CARD_CHANGE, OnDataChanged);
            Global.Publisher.Subscribe(MessagePublisher.REFRESH_SOLDIER_SKILL_LIST, OnDataChanged);
        }

        protected override void OnUnbind()
        {
            IReadOnlyList<ItemConfig> consumeItems = SoldierRedDotHelper.SoldierExpConsumeItems;
            for (int i = 0; i < consumeItems.Count; i++)
            {
                Global.Publisher.Unsubscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{consumeItems[i].Id}", OnDataChanged);
            }

            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnDataChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, OnDataChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_CARD_CHANGE, OnDataChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.REFRESH_SOLDIER_SKILL_LIST, OnDataChanged);
        }

        void OnDataChanged(object obj)
        {
            MarkAllDirty();
        }

        public override int Evaluate(LocalRedDotKey key)
        {
            SoldierPanelRedDotScope scope = GetScope<SoldierPanelRedDotScope>();
            if (scope == null)
            {
                return 0;
            }

            if (TryGetNodeType(key, out RedNodeType nodeType))
            {
                switch (nodeType)
                {
                    case RedNodeType.SoliderUpgradeCard:
                        return EvaluateCardUpgrade(scope.SelectedCardId);
                    case RedNodeType.SoliderDevelopCard:
                        return EvaluateCardDevelop(scope.SelectedCardId);
                    case RedNodeType.SoliderAwakeningCard:
                        return EvaluateCardAwakening(scope.SelectedCardId);
                }
            }

            if (TryParseCardPath(key.Path, "Upgrade", out int upgradeCardId))
            {
                return EvaluateCardUpgrade(upgradeCardId);
            }

            if (TryParseCardPath(key.Path, "Develop", out int developCardId))
            {
                return EvaluateCardDevelop(developCardId);
            }

            if (TryParseCardPath(key.Path, "Activate", out int activateCardId))
            {
                return EvaluateCardActivate(activateCardId);
            }

            if (TryParseCardPath(key.Path, "OwnedHint", out int ownedHintCardId))
            {
                return EvaluateCardOwnedHint(ownedHintCardId);
            }

            if (TryParseCardPath(key.Path, "Awakening", out int awakeningCardId))
            {
                return EvaluateCardAwakening(awakeningCardId);
            }

            return 0;
        }

        static int EvaluateCardUpgrade(int cardId)
        {
            if (cardId <= 0)
            {
                return 0;
            }

            return Global.PlayerCards.TryGetValue(cardId, out PlayerCard card)
                   && SoldierRedDotHelper.CheckSoldierUpgrade(card)
                ? 1
                : 0;
        }

        static int EvaluateCardDevelop(int cardId)
        {
            return cardId > 0 && SoldierRedDotHelper.CheckSoldierDevelop(cardId) ? 1 : 0;
        }

        static int EvaluateCardActivate(int cardId)
        {
            return cardId > 0 && SoldierRedDotHelper.CanActivateSoldier(cardId) ? 1 : 0;
        }

        static int EvaluateCardAwakening(int cardId)
        {
            return cardId > 0 && SoldierRedDotHelper.CheckSoldierAwakening(cardId) ? 1 : 0;
        }

        static int EvaluateCardOwnedHint(int cardId)
        {
            if (cardId <= 0)
            {
                return 0;
            }

            if (EvaluateCardUpgrade(cardId) > 0
                || EvaluateCardDevelop(cardId) > 0
                || EvaluateCardAwakening(cardId) > 0)
            {
                return 1;
            }

            return 0;
        }

        static int EvaluateArrow(SoldierPanelRedDotScope scope, ERedArrowType arrowType)
        {
            IReadOnlyList<int> soldierCardIds = scope.SoldierCardIds;
            if (soldierCardIds == null || scope.SelectedCardId <= 0)
            {
                return 0;
            }

            Dictionary<int, PlayerCard> soldiers = SoldierRedDotHelper.CollectSoldiersBeforeSelected(
                soldierCardIds,
                scope.SelectedCardId,
                arrowType);

            int count = 0;
            foreach (KeyValuePair<int, PlayerCard> cardInfo in soldiers)
            {
                if (SoldierRedDotHelper.CheckSoldierUpgrade(cardInfo.Value))
                {
                    count++;
                }

                if (SoldierRedDotHelper.CheckSoldierDevelop(cardInfo.Value.CardId))
                {
                    count++;
                }

                if (SoldierRedDotHelper.CheckSoldierAwakening(cardInfo.Value))
                {
                    count++;
                }
            }

            return count;
        }

        static bool TryParseCardPath(string path, string suffix, out int cardId)
        {
            cardId = 0;
            const string prefix = "Card/";
            string suffixToken = $"/{suffix}";
            if (!path.StartsWith(prefix) || !path.EndsWith(suffixToken))
            {
                return false;
            }

            string idPart = path.Substring(prefix.Length, path.Length - prefix.Length - suffixToken.Length);
            return int.TryParse(idPart, out cardId);
        }
    }
}
