using Config;
using Game.Challenge.RedDot;
using Game.RedDot;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Business.Rules
{
    public sealed class MonsterAtlasRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.MonsterAtlasBtn;

        protected override bool CanWatchItemAllChange => false;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MonsterAtlasBtn, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.MONSTER_ATLAS_STATUS_CHANGED, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MonsterAtlasBtn, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.MONSTER_ATLAS_STATUS_CHANGED, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.MonsterAtlas, false))
            {
                return 0;
            }

            return MonsterAtlasRedDotHelper.CountCanReceive();
        }
    }
}
