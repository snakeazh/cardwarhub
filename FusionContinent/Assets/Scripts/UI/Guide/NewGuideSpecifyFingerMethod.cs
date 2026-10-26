namespace FusionContinent.Guide
{
    /// <summary>
    /// 指定引导卡牌
    /// </summary>
    public class NewGuideSpecifyFingerMethod : NewGuideWeakCardMethod
    {
        private int _cardId;
        private const string constURl = "GameLand/Canvas/FriendlySlot/SoldierCard(Clone)";
        public NewGuideSpecifyFingerMethod(int cardId)
        {
            _cardId = cardId;
        }

        public override void Enter()
        {
            base.Enter();
            this._context.Panel.Mask.gameObject.SetActive(true);
        }

        protected override string GetGuideUIPath()
        {
            string guideToUIPath = _stepListStruct.GuideUI + "/card_" + _cardId;
            if (_cardId == 400011)
            {
                guideToUIPath = constURl;
            }

            return guideToUIPath;
        }
    }
}