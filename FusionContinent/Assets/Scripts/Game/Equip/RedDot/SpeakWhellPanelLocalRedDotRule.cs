using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;

namespace Game.Equip.RedDot
{
    public sealed class SpeakWhellPanelLocalRedDotRule : LocalRedDotRuleBase
    {
        public override string Scope => SpeakWhellPanelRedDotScope.ScopeName;

        protected override void OnBind()
        {
            EquipRedDotRefreshEvents.Subscribe(OnDataChanged);
            Global.Publisher.Subscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, OnDataChanged);
            Global.Publisher.Subscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, OnDataChanged);
        }

        protected override void OnUnbind()
        {
            EquipRedDotRefreshEvents.Unsubscribe(OnDataChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.PLAYER_EQUIP_DATA_CHANGED, OnDataChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, OnDataChanged);
        }

        void OnDataChanged(object _) => MarkAllDirty();

        public override int Evaluate(LocalRedDotKey key)
        {
            SpeakWhellPanelRedDotScope scope = GetScope<SpeakWhellPanelRedDotScope>();
            if (scope == null)
            {
                return 0;
            }

            if (TryParseWearSlotPath(key.Path, out int partType))
            {
                return scope.HasBetterBagEquipAtPart(partType) ? 1 : 0;
            }

            if (TryParseBagEquipPath(key.Path, out int equipId))
            {
                if (!Global.PlayerEquips.TryGetValue(equipId, out PlayerEquip bagEquip))
                {
                    return 0;
                }

                return scope.IsBagEquipBetter(bagEquip) ? 1 : 0;
            }

            if (TryParseBagFusionGemPath(key.Path, out int gemId))
            {
                if (!Global.PlayerFusionGems.TryGetValue(gemId, out PlayerFusionGem bagGem))
                {
                    return 0;
                }

                if (scope.IsBagFusionGemBetter(bagGem))
                {
                    return 1;
                }

                return FusionGemBreakthroughRedDotHelper.CanBreakthrough(bagGem) ? 1 : 0;
            }

            return 0;
        }

        static bool TryParseWearSlotPath(string path, out int partType)
        {
            partType = 0;
            string prefix = SpeakWhellPanelRedDotScope.Paths.WearPrefix;
            if (!path.StartsWith(prefix))
            {
                return false;
            }

            return int.TryParse(path.Substring(prefix.Length), out partType);
        }

        static bool TryParseBagEquipPath(string path, out int equipId)
        {
            equipId = 0;
            string prefix = SpeakWhellPanelRedDotScope.Paths.BagPrefix;
            if (!path.StartsWith(prefix))
            {
                return false;
            }

            return int.TryParse(path.Substring(prefix.Length), out equipId);
        }

        static bool TryParseBagFusionGemPath(string path, out int gemId)
        {
            gemId = 0;
            string prefix = SpeakWhellPanelRedDotScope.Paths.GemBagPrefix;
            if (!path.StartsWith(prefix))
            {
                return false;
            }

            return int.TryParse(path.Substring(prefix.Length), out gemId);
        }
    }
}
