namespace FusionContinent.Target
{
    /// <summary>
    /// 英灵→怪物选敌规则。不同场景使用不同规则，不可混用。
    /// </summary>
    public enum EnemySelectionRule
    {
        /// <summary>
        /// 攻击范围内 Y 坐标最小的敌人（最靠前）。
        /// 保留兼容；现网战士索敌改走 <see cref="EnemyTargetFinder.TrySelectFrontmostInRange"/>。
        /// </summary>
        LowestYInRange,

        /// <summary>
        /// 攻击范围内距离最近的敌人（比较时含 monsterPosOffset）。
        /// 用于非战士普攻回退；实际普攻入口优先走 Boss / 职业优先级。
        /// </summary>
        NearestInRange,

        /// <summary>
        /// 全场距离最近的敌人（不受攻击距离限制，比较时含 monsterPosOffset）。
        /// 用于多目标弹道填充等。
        /// </summary>
        Nearest,

        /// <summary>
        /// 邻域内敌人数量最多的目标（平手比距施法点更近）。
        /// 保留实现；主动技能已改为方向密度 <see cref="EnemyTargetFinder.TrySelectFanCrowdTarget"/>。
        /// </summary>
        MostCrowded,

        /// <summary>
        /// 扇形内偏离中心轴角度最小的敌人。
        /// 需 <see cref="FanSectorParams"/> + <see cref="EnemyTargetFinder.TrySelectEnemyInFan"/>。
        /// </summary>
        FanSmallestAngle,

        /// <summary>
        /// 扇形内距离扇形顶点最近的敌人。
        /// 保留实现；主动技能已改为朝敌人最密集方向 <see cref="EnemyTargetFinder.TrySelectFanCrowdTarget"/>。
        /// </summary>
        FanNearest,

        /// <summary>
        /// 扇形内综合评分：角度 45% + 距离 45%（在候选集内归一化后加权，得分越低越优先）。
        /// </summary>
        FanAngleDistanceWeighted,
    }
}
