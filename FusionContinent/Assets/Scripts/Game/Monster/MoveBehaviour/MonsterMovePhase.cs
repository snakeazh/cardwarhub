namespace FusionContinent.Monster.MoveBehaviour
{
    /// <summary>
    /// 线性移动（StraightMoveBehaviour 及其子类）的内部阶段。
    /// </summary>
    public enum MonsterMovePhase
    {
        /// <summary>未在移动（StopMove 或 Enter 前）。</summary>
        None,

        /// <summary>出场后沿 Y 轴向下行进。</summary>
        Downward,

        /// <summary>到达索敌深度后，朝 targetSolidierCard 移动。</summary>
        TowardTarget,
    }
}
