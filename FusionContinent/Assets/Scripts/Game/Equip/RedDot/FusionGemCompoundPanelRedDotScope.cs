using Game.RedDot;

namespace Game.Equip.RedDot
{
    public sealed class FusionGemCompoundPanelRedDotScope : LocalRedDotScopeBase
    {
        public const string ScopeName = "FusionGemCompoundPanel";

        public static class Paths
        {
            public static string Secondary(int gemId) => $"Secondary/{gemId}";
        }

        public int MainGemId { get; private set; }

        public override string Scope => ScopeName;

        public static void RegisterRules(RedDotRuleRegistry registry)
        {
            RegisterRule(registry, ScopeName, () => new FusionGemCompoundPanelLocalRedDotRule());
        }

        public void Open(int mainGemId)
        {
            MainGemId = mainGemId;
            Open();
        }

        public void SetMainGemId(int mainGemId)
        {
            MainGemId = mainGemId;
            MarkAllDirty();
            Service.FlushNow();
        }

        public void NotifyDataChanged()
        {
            MarkAllDirty();
            Service.MarkDirty(RedNodeType.SpeakerFusionGemBreakthroughRedDot);
        }

        protected override void BuildNodes(RedDotTree tree)
        {
        }

        protected override void ResetState()
        {
            MainGemId = 0;
        }
    }
}
