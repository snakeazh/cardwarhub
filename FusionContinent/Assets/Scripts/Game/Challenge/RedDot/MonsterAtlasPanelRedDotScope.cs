using Config;
using FusionContinent;
using Game.RedDot;

namespace Game.Challenge.RedDot
{
    public sealed class MonsterAtlasPanelRedDotScope : LocalRedDotScopeBase
    {
        public const string ScopeName = "MonsterAtlasPanel";

        public static class Paths
        {
            public static string Quality(MonsterQuality quality) => $"Quality/{(int)quality}";

            public static string Monster(int monsterId) => $"Monster/{monsterId}";
        }

        public override string Scope => ScopeName;

        public static void RegisterRules(RedDotRuleRegistry registry)
        {
            RegisterRule(registry, ScopeName, () => new MonsterAtlasPanelLocalRedDotRule());
        }

        public void NotifyDataChanged()
        {
            MarkAllDirty();
            Service.MarkDirty(RedNodeType.MonsterAtlasBtn);
        }

        protected override void BuildNodes(RedDotTree tree)
        {
            EnsureNode(tree, Paths.Quality(MonsterQuality.Ordinary));
            EnsureNode(tree, Paths.Quality(MonsterQuality.Elite));
            EnsureNode(tree, Paths.Quality(MonsterQuality.Boss));

            var monsterList = DataConfigManager.Tables.TbMonsterBattleConfig.DataList;
            for (int i = 0; i < monsterList.Count; i++)
            {
                EnsureNode(tree, Paths.Monster(monsterList[i].Id));
            }
        }
    }
}
