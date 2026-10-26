namespace FusionContinent.Model
{
    public class PlayerWeapon
    {
        public int Id { get; set; }
        public int WeaponId { get; set; }
        public int Level { get; set; }

        public PlayerWeapon()
        {
        }

        public PlayerWeapon(PlayerWeapon source)
        {
            Id = source.Id;
            WeaponId = source.WeaponId;
            Level = source.Level;
        }
    }
}
