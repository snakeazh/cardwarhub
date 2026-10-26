using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;

namespace Game.Equip.RedDot
{
    public sealed class FusionGemCompoundPanelLocalRedDotRule : LocalRedDotRuleBase
    {
        public override string Scope => FusionGemCompoundPanelRedDotScope.ScopeName;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, OnDataChanged);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, OnDataChanged);
        }

        void OnDataChanged(object _) => MarkAllDirty();

        public override int Evaluate(LocalRedDotKey key)
        {
            FusionGemCompoundPanelRedDotScope scope = GetScope<FusionGemCompoundPanelRedDotScope>();
            if (scope == null || scope.MainGemId <= 0)
            {
                return 0;
            }

            if (!TryParseSecondaryPath(key.Path, out int gemId))
            {
                return 0;
            }

            if (!Global.PlayerFusionGems.TryGetValue(scope.MainGemId, out PlayerFusionGem mainGem))
            {
                return 0;
            }

            if (!Global.PlayerFusionGems.TryGetValue(gemId, out PlayerFusionGem candidate))
            {
                return 0;
            }

            return FusionGemBreakthroughRedDotHelper.IsUnequippedConsumableSecondary(mainGem, candidate)
                ? 1
                : 0;
        }

        static bool TryParseSecondaryPath(string path, out int gemId)
        {
            gemId = 0;
            const string prefix = "Secondary/";
            if (!path.StartsWith(prefix))
            {
                return false;
            }

            return int.TryParse(path.Substring(prefix.Length), out gemId);
        }
    }
}
