using System.Collections.Generic;

namespace FusionContinent.Model
{
    public class PlayerCityWall
    {
        public int Level { get; set; }
    }

    public class PlayerCityWallSkill
    {
        public int SkillId { get; set; }
        public int Level { get; set; }
    }

    public class PlayerCityWallArchive
    {
        public PlayerCityWall Wall { get; set; } = new PlayerCityWall();
        public Dictionary<int, PlayerCityWallSkill> Skills { get; set; } = new Dictionary<int, PlayerCityWallSkill>();
        public List<int> SlotSkills { get; set; } = new List<int>();
    }
}
