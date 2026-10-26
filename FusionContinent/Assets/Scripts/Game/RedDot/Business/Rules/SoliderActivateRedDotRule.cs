using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;
using Game.RedDot.Rules;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 英灵可解锁红点：存在未拥有且 ActivationCost 已满足的英灵时亮。
    /// 聚合到 NavBarSoldier，驱动 GameMainPanel Btn(3)。
    /// </summary>
    public sealed class SoliderActivateRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SoliderCanActivate;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_CARD_CHANGE, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_CARD_CHANGE, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.SoldierAtlas, false))
            {
                return 0;
            }

            return SoldierRedDotHelper.CountActivatableSoldiers();
        }
    }
}
