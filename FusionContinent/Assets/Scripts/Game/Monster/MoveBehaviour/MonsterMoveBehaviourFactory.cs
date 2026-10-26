using Config;

namespace FusionContinent.Monster.MoveBehaviour
{
    /// <summary>
    /// 根据配置 MoveType 创建对应的移动行为策略实例。
    /// 在 MonsterCard.Init 末尾调用，结果赋值给 MonsterCard.MoveBehaviour。
    /// </summary>
    public static class MonsterMoveBehaviourFactory
    {
        public static IMonsterMoveBehaviour Create(MoveType moveType)
        {
            switch (moveType)
            {
                // 直行 / 斜行：共用直线位移
                case MoveType.ZhiXing:
                case MoveType.SXing:
                    return new StraightMoveBehaviour();

                // 跳跃：默认直行，runStage.jumpmove 时走跳跃循环
                case MoveType.Jumping:
                    return new JumpMoveBehaviour();

                // 钻地：位移同直行，出场位置由 MonsterCard.BurrowCreate 处理
                case MoveType.ZuanDi:
                    return new BurrowMoveBehaviour();

                // 飞行：位移同直行，后续可在 FlyMoveBehaviour 覆写表现
                case MoveType.Flying:
                    return new FlyMoveBehaviour();

                // 静止：初始化后直接进入 engage，通常不走移动状态
                case MoveType.Motionless:
                    return new MotionlessMoveBehaviour();

                default:
                    return new StraightMoveBehaviour();
            }
        }
    }
}
