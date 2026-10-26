using System.Collections.Generic;

using Config;
using Game.RedDot;

using FusionContinent.Model;

using Game.RedDot.Rules;



namespace Game.RedDot.Business.Rules

{

    public sealed class SoliderDevelopRedDotRule : RedDotRuleBase

    {

        public override RedNodeType NodeType => RedNodeType.SoliderCanUpgrade;



        public override int Evaluate()

        {

            Dictionary<int, PlayerCard> soldiers = SoldierRedDotHelper.CollectOwnedSoldiers(SoldierRedDotHelper.GetSoldierCardIds());

            int count = 0;

            foreach (KeyValuePair<int, PlayerCard> cardInfo in soldiers)

            {

                if (SoldierRedDotHelper.CheckSoldierDevelop(cardInfo.Value.CardId))

                {

                    count++;

                }

            }



            return count;

        }

    }

}

