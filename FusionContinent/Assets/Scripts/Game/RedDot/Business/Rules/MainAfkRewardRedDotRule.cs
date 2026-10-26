using Config;
using Game.RedDot;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Business.Rules
{
    public sealed class MainAfkRewardRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.MainPageAFKBtn;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MainPageAFKBtn, OnRedTypeChange);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MainPageAFKBtn, OnRedTypeChange);
        }

        void OnRedTypeChange(object obj)
        {
            Context.MarkDirty();
        }

        public override int Evaluate()
        {
            return Global.PlayerBattle.CheckAfkProduce() ? 1 : 0;
        }
    }
}
