namespace FusionContinent.Monster.MoveBehaviour
{
    /// <summary>
    /// 钻地移动（MoveType.ZuanDi）。
    ///
    /// 当前位移规则与直行相同（原 MonsterTweens.BurrowMove 逻辑一致）。
    /// 出场位置由 MonsterCard.BurrowCreate 在 Init 时设置，不在本类处理。
    ///
    /// 作为独立策略类存在，便于后续在此覆写：
    ///   - ApplyDownwardMove：钻地特效/地下行进表现
    ///   - GetEngageDepthY：钻地怪物的索敌深度
    /// </summary>
    public class BurrowMoveBehaviour : StraightMoveBehaviour
    {
    }
}
