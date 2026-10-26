using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;
using Game.Station;
using UIFramework.Services;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 驿站可领取体力红点。聚合到 StationBtn / NavBarBigWord。
    /// </summary>
    public sealed class StationReceiveRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.StationReceiveBtn;

        protected override bool CanWatchItemAllChange => false;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.StationReceiveBtn, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.StationBtn, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.StationReceiveBtn, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.StationBtn, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            StationService service = GameServiceManager.GetOrCreate<StationService>();
            return service.HasClaimableStamina ? 1 : 0;
        }
    }
}
