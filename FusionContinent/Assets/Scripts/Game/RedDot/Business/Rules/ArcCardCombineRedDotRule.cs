using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;
using Game.Soldier.RedDot;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 奥术卡可合成红点：任一品质 ComposeRewards 材料足够时亮。
    /// 聚合到 NavBarGeneralSkill，驱动 GameMainPanel Btn(1) 与 GeneralSkillPanel CombineBtn。
    /// </summary>
    public sealed class ArcCardCombineRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.ArcCardCombine;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.GeneralSkill, false))
            {
                return 0;
            }

            return ArcCardCombineRedDotHelper.CountCombinable();
        }
    }
}
