namespace FusionContinent.Monster.AttackBehaviour
{
    /// <summary>
    /// 运行时攻击行为类型，由 Factory 根据 MonsterBattleConfig 现有字段解析。
    /// </summary>
    public enum AttackBehaviourType
    {
        None = 0,
        MeleeImpact = 1,
        RangedBullet = 2,
        FlameBreath = 3,
        AlienLaser = 4,
        HamadryadParticle = 5,
        JungleKnightDual = 6,
        MushroomBossMelee = 7,
        /// <summary>
        /// 中心点目标位置：在 (AtkLocation.x, 人物Y) 直接生成攻击特效。
        /// </summary>
        TargetCenterStrike = 8,
    }
}
