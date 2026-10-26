using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;

namespace Game.Challenge.RedDot
{
    public sealed class MonsterAtlasPanelLocalRedDotRule : LocalRedDotRuleBase
    {
        public override string Scope => MonsterAtlasPanelRedDotScope.ScopeName;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.MONSTER_ATLAS_STATUS_CHANGED, OnDataChanged);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MonsterAtlasBtn, OnDataChanged);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.MONSTER_ATLAS_STATUS_CHANGED, OnDataChanged);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MonsterAtlasBtn, OnDataChanged);
        }

        void OnDataChanged(object _) => MarkAllDirty();

        public override int Evaluate(LocalRedDotKey key)
        {
            if (TryParseQualityPath(key.Path, out int qualityValue))
            {
                return MonsterAtlasRedDotHelper.CountCanReceiveByQuality((MonsterQuality)qualityValue);
            }

            if (TryParseMonsterPath(key.Path, out int monsterId))
            {
                return MonsterAtlasRedDotHelper.CanReceive(monsterId) ? 1 : 0;
            }

            return 0;
        }

        static bool TryParseQualityPath(string path, out int qualityValue)
        {
            qualityValue = 0;
            const string prefix = "Quality/";
            if (!path.StartsWith(prefix))
            {
                return false;
            }

            return int.TryParse(path.Substring(prefix.Length), out qualityValue);
        }

        static bool TryParseMonsterPath(string path, out int monsterId)
        {
            monsterId = 0;
            const string prefix = "Monster/";
            if (!path.StartsWith(prefix))
            {
                return false;
            }

            return int.TryParse(path.Substring(prefix.Length), out monsterId);
        }
    }
}
