using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;

namespace Game.Soldier.RedDot
{
    public sealed class GeneralCardConbinePanelLocalRedDotRule : LocalRedDotRuleBase
    {
        public override string Scope => GeneralCardConbinePanelRedDotScope.ScopeName;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnDataChanged);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_ITEM_CHANGE, OnDataChanged);
        }

        void OnDataChanged(object _) => MarkAllDirty();

        public override int Evaluate(LocalRedDotKey key)
        {
            if (!TryParseCardPath(key.Path, out int qualityValue))
            {
                return 0;
            }

            QualityType quality = (QualityType)qualityValue;
            return ArcCardCombineRedDotHelper.CanCombine(quality) ? 1 : 0;
        }

        static bool TryParseCardPath(string path, out int qualityValue)
        {
            qualityValue = 0;
            const string prefix = "Card/";
            if (!path.StartsWith(prefix))
            {
                return false;
            }

            return int.TryParse(path.Substring(prefix.Length), out qualityValue);
        }
    }
}
