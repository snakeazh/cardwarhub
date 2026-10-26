namespace FusionContinent.Monster.MoveBehaviour
{
    /// <summary>
    /// 飞行移动（MoveType.Flying）。
    ///
    /// 当前位移规则与直行相同（原 MonsterTweens.FlyMove 逻辑一致）。
    /// 作为独立策略类存在，便于后续在此覆写：
    ///   - ApplyDownwardMove：飞行高度/阴影
    ///   - ApplyTowardTargetMove：贴地飞行等
    ///
    /// 示例：
    ///   protected override void ApplyDownwardMove(...) { base.ApplyDownwardMove(...); }
    /// </summary>
    public class FlyMoveBehaviour : StraightMoveBehaviour
    {
    }
}
