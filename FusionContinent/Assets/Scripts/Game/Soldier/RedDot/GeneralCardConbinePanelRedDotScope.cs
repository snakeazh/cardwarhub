using Config;
using Game.RedDot;

namespace Game.Soldier.RedDot
{
    public sealed class GeneralCardConbinePanelRedDotScope : LocalRedDotScopeBase
    {
        public const string ScopeName = "GeneralCardConbinePanel";

        public static class Paths
        {
            public static string Card(QualityType quality) => $"Card/{(int)quality}";
        }

        public override string Scope => ScopeName;

        public static void RegisterRules(RedDotRuleRegistry registry)
        {
            RegisterRule(registry, ScopeName, () => new GeneralCardConbinePanelLocalRedDotRule());
        }

        public void NotifyDataChanged()
        {
            MarkAllDirty();
            Service.MarkDirty(RedNodeType.ArcCardCombine);
        }

        protected override void BuildNodes(RedDotTree tree)
        {
            QualityType[] qualities = ArcCardCombineRedDotHelper.ComposeQualities;
            for (int i = 0; i < qualities.Length; i++)
            {
                EnsureNode(tree, Paths.Card(qualities[i]));
            }
        }
    }
}
