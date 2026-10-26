using Config;
using Game.Equip.RedDot;
using Game.RedDot;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 轮语者装备可替换红点：背包中存在比当前穿戴装备战力更高的同部位装备时亮。
    /// 聚合到 NavBarSpeaker，驱动底部导航栏轮语者 Tab 红点。
    /// </summary>
    public sealed class SpeakerEquipReplaceRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerEquipReplaceRedDot;

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
            return EquipReplaceRedDotHelper.CountReplaceableParts(wearerId);
        }
    }
}
