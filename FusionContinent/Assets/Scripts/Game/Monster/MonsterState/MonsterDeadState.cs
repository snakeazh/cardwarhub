using DG.Tweening;
using FusionContinent;

namespace Game.StateMachine
{
    public class MonsterDeadState : State
    {
        public MonsterDeadState(StateMachine sm, MonsterCard monster) : base(sm, monster)
        {
        }

        public override void Enter()
        {
            
            MonsterDeathHandler.HandleDeath(monster);
            // 触发死亡逻辑
            monster.Die();
        }

        public override void Update()
        {
            // 死亡后不需要每帧更新
        }

        public override void Exit()
        {
        }
    }
}
