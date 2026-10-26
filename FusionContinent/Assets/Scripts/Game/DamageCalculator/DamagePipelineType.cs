namespace FusionContinent
{
    /// <summary>
    /// 伤害计算管线的类型，决定使用哪些 Modifier 组合
    /// </summary>
    public enum DamagePipelineType
    {
        /// <summary>
        /// 怪物伤害计算 - 怪物攻击人物
        /// 特点：无闪避、无暴击、有抗性计算、有精英/首领修正
        /// </summary>
        MonsterDamage = 0,

        /// <summary>
        /// 人物伤害计算 - 人物攻击怪物
        /// 特点：有暴击、有闪避、有combo、有破盾增伤
        /// </summary>
        SoldierDamage = 1,

        /// <summary>
        /// 场景技能计算 - 场景技能造成的伤害
        /// 特点：特殊规则，可配置
        /// </summary>
        SceneSkillDamage = 2,
        
        /// <summary>
        /// 燃烧等特殊状态伤害 - buff造成的伤害
        /// 特点：特殊规则，可配置
        /// </summary>
        BuffSkillDamage = 3
    }
}
