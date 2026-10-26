using Config;
using Game.Equip.RedDot;
using Game.RedDot;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Business.Rules
{
    public abstract class SpeakerEquipRedDotRuleBase : RedDotRuleBase
    {
        protected abstract int PageIndex { get; }

        protected override bool CanWatchItemAllChange => false;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, OnDirty);
            EquipRedDotRefreshEvents.Subscribe(OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, OnDirty);
            EquipRedDotRefreshEvents.Unsubscribe(OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Equip, false))
            {
                return 0;
            }

            int wearerId = DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId;
            return EquipForgeRedDotHelper.CountSoldierWearEquipRedPoints(
                wearerId, PageIndex, 0, 0);
        }
    }
}
