using FusionContinent;
using FusionContinent.Model;
using Game.RedDot;

namespace Game.Equip.RedDot
{
    public static class EquipGlobalRedDotRefresh
    {
        public static void PublishAll()
        {
            PublishSpeakerEntry();
        }

        public static void PublishSpeakerEntry()
        {
            Publish(RedNodeType.SpeakerEquipForgeRedDot);
            Publish(RedNodeType.SpeakerEquipRefineRedDot);
        }

        static void Publish(RedNodeType type)
        {
            Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE + type, null);
        }
    }
}
