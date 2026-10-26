using System.Collections.Generic;

using Config;
using Game.RedDot;

using FusionContinent;

using FusionContinent.Model;

using Game.RedDot.Rules;



namespace Game.RedDot.Business.Rules

{

    public sealed class SoliderUpgradeRedDotRule : RedDotRuleBase

    {

        public override RedNodeType NodeType => RedNodeType.SoliderCanUpgrade;



        protected override void OnBind()

        {

            IReadOnlyList<ItemConfig> consumeItems = SoldierRedDotHelper.SoldierExpConsumeItems;

            for (int i = 0; i < consumeItems.Count; i++)

            {

                Global.Publisher.Subscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{consumeItems[i].Id}", OnItemChange);

            }

        }



        protected override void OnUnbind()

        {

            IReadOnlyList<ItemConfig> consumeItems = SoldierRedDotHelper.SoldierExpConsumeItems;

            for (int i = 0; i < consumeItems.Count; i++)

            {

                Global.Publisher.Unsubscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{consumeItems[i].Id}", OnItemChange);

            }

        }



        void OnItemChange(object obj)

        {

            Context.MarkDirty();

        }



        public override int Evaluate()

        {

            Dictionary<int, PlayerCard> soldiers = SoldierRedDotHelper.CollectOwnedSoldiers(SoldierRedDotHelper.GetSoldierCardIds());

            int count = 0;

            foreach (KeyValuePair<int, PlayerCard> cardInfo in soldiers)

            {

                if (SoldierRedDotHelper.CheckSoldierUpgrade(cardInfo.Value))

                {

                    count++;

                }

            }



            return count;

        }

    }

}

