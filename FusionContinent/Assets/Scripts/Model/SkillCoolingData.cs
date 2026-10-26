namespace FusionContinent.Model
{
    /// <summary>
    /// 技能冷却数据
    /// </summary>
    public class SkillCoolingData
    {
        public int SkillId { get; set; }
        public float CooldownTime { get; set; }
        
        public SkillCoolingData(int skillId, float cooldownTime)
        {
            SkillId = skillId;
            CooldownTime = cooldownTime;
        }
    }
}
