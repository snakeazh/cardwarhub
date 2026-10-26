using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.HeroSeat;
using Game.RedDot;
using UIFramework.Services;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 机关可激活：未激活且材料足够。聚合到 SpeakerHeroSeatBtn / NavBarSpeaker。
    /// </summary>
    public sealed class SpeakerHeroSeatActivateRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerHeroSeatActivateBtn;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Speaker, false))
            {
                return 0;
            }

            SeatService seatService = GameServiceManager.Get<SeatService>();
            if (seatService == null)
            {
                return 0;
            }

            return seatService.CountActivatableSkills();
        }
    }

    /// <summary>
    /// 机关可升级：已激活、未满级且材料足够。聚合到 SpeakerHeroSeatBtn / NavBarSpeaker。
    /// </summary>
    public sealed class SpeakerHeroSeatUpgradeRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerHeroSeatUpgradeBtn;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Speaker, false))
            {
                return 0;
            }

            SeatService seatService = GameServiceManager.Get<SeatService>();
            if (seatService == null)
            {
                return 0;
            }

            return seatService.CountUpgradableSkills();
        }
    }

    /// <summary>
    /// 城墙可升级：未满级且材料足够。驱动 HeroSeatPanel UpGradeBtn，聚合到 SpeakerHeroSeatBtn / NavBarSpeaker。
    /// </summary>
    public sealed class SpeakerHeroSeatWallUpgradeRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerHeroSeatWallUpgradeBtn;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.CITY_WALL_DATA_CHANGED, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Speaker, false))
            {
                return 0;
            }

            SeatService seatService = GameServiceManager.Get<SeatService>();
            if (seatService == null)
            {
                return 0;
            }

            return seatService.CanUpgradeWall() ? 1 : 0;
        }
    }
}
