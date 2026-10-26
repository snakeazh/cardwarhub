using Game.Equip.RedDot;
using Game.RedDot;

namespace Game.RedDot.Business.Rules
{
    public sealed class SpeakerEquipRefineRedDotRule : SpeakerEquipRedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerEquipRefineRedDot;

        protected override int PageIndex => EquipForgeRedDotHelper.RefinePageIndex;
    }
}
