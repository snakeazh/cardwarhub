namespace FusionContinent.Monster.MoveBehaviour
{
    /// <summary>
    /// 静止不动（MoveType.Motionless）。
    ///
    /// 此类怪物在 MonsterCard.Init 末尾直接进入 engage 状态，
    /// 通常不会进入 MonsterMoveState，本类所有方法为空实现。
    /// </summary>
    public class MotionlessMoveBehaviour : MonsterMoveBehaviourBase
    {
        public override void Enter(MonsterCard monster)
        {
        }

        public override void Update(MonsterCard monster, float deltaTime)
        {
        }

        public override void StopMove()
        {
        }
    }
}
