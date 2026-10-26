using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.Equip.RedDot;
using Game.RedDot;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 轮语者融合石可突破红点：存在未满阶且有可吞噬次石的融合石时亮。
    /// 聚合到 NavBarSpeaker。
    /// </summary>
    public sealed class SpeakerFusionGemBreakthroughRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerFusionGemBreakthroughRedDot;

        protected override bool CanWatchItemAllChange => false;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.FusionGemSlot, false))
            {
                return 0;
            }

            return FusionGemBreakthroughRedDotHelper.CountBreakthroughGems();
        }
    }
}
