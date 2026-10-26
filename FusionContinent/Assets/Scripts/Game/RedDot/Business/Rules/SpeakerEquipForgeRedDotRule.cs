using Game.Equip.RedDot;
using Game.RedDot;

namespace Game.RedDot.Business.Rules
{
    public sealed class SpeakerEquipForgeRedDotRule : SpeakerEquipRedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerEquipForgeRedDot;

        protected override int PageIndex => EquipForgeRedDotHelper.ForgePageIndex;
    }
}
