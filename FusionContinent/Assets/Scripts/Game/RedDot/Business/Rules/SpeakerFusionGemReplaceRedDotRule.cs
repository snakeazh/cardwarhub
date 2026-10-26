using Config;
using Game.Equip.RedDot;
using Game.RedDot;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 轮语者融合石可替换红点：背包存在比当前部位镶嵌更高战力的融合石时亮。
    /// 聚合到 NavBarSpeaker，驱动底部导航栏轮语者 Tab 红点。
    /// </summary>
    public sealed class SpeakerFusionGemReplaceRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerFusionGemReplaceRedDot;

        protected override bool CanWatchItemAllChange => false;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, OnDirty);
            EquipRedDotRefreshEvents.Subscribe(OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, OnDirty);
            EquipRedDotRefreshEvents.Unsubscribe(OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.FusionGemSlot, false))
            {
                return 0;
            }

            int wearerId = DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId;
            return FusionGemReplaceRedDotHelper.CountReplaceableGemParts(wearerId);
        }
    }
}
