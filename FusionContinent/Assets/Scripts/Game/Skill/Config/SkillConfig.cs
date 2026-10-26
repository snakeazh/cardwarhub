using UnityEngine;

namespace FusionContinent.Skill.Config
{
    /// <summary>
    /// 技能配置（兼容 Luban 表）
    /// </summary>
    [System.Serializable]
    public class SkillConfig
    {
        public int Id;
        public string Name;
        public string Icon;
        public int TargetType;        // 0=None, 1=Enemy, 2=Ally, 3=Self, 4=Point
        public float BaseCooldown;
        public int BaseCost;

        // 兼容旧系统
        public string LogicType;
    }
}
