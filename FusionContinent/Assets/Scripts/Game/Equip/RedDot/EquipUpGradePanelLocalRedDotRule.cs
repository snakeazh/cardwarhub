using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;

namespace Game.Equip.RedDot
{
    public sealed class EquipUpGradePanelLocalRedDotRule : LocalRedDotRuleBase
    {
        public override string Scope => EquipUpGradePanelRedDotScope.ScopeName;

        protected override void OnBind()
        {
            EquipRedDotRefreshEvents.Subscribe(OnDataChanged);
            Global.Publisher.Subscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, OnDataChanged);
        }

        protected override void OnUnbind()
        {
            EquipRedDotRefreshEvents.Unsubscribe(OnDataChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, OnDataChanged);
        }

        void OnDataChanged(object _) => MarkAllDirty();

        public override int Evaluate(LocalRedDotKey key)
        {
            EquipUpGradePanelRedDotScope scope = GetScope<EquipUpGradePanelRedDotScope>();
            if (scope == null)
            {
                return 0;
            }

            if (!TryParseWearSlotPath(key.Path, out int equipInstanceId))
            {
                return 0;
            }

            if (!Global.PlayerEquips.TryGetValue(equipInstanceId, out PlayerEquip equip))
            {
                return 0;
            }

            if (equip.WearerId != scope.SoldierId)
            {
                return 0;
            }

            return EquipForgeRedDotHelper.EvaluateWearEquipSlotRedDot(
                equip,
                EquipWearSlotRedDotContext.ByPage(
                    scope.CurrentPage, scope.SelectedEquipId, scope.LockedEffectCount));
        }

        static bool TryParseWearSlotPath(string path, out int equipInstanceId)
        {
            equipInstanceId = 0;
            string prefix = EquipUpGradePanelRedDotScope.Paths.WearPrefix;
            if (!path.StartsWith(prefix))
            {
                return false;
            }

            return int.TryParse(path.Substring(prefix.Length), out equipInstanceId);
        }
    }
}
