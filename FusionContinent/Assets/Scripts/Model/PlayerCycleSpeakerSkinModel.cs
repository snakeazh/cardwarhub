namespace FusionContinent.Model
{
    public class PlayerCycleSpeakerSkin
    {
        public int Id { get; set; }
        public int SkinId { get; set; }
        public int Level { get; set; }

        public PlayerCycleSpeakerSkin()
        {
        }

        public PlayerCycleSpeakerSkin(PlayerCycleSpeakerSkin source)
        {
            Id = source.Id;
            SkinId = source.SkinId;
            Level = source.Level;
        }
    }
}
