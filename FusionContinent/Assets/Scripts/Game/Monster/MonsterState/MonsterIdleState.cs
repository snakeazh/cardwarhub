using System.Collections.Generic;
using FusionContinent;

namespace Game.StateMachine
{
    public class MonsterIdleState : State
    {
        List<Transition> transitions = new List<Transition>();

        public MonsterIdleState(StateMachine sm, MonsterCard monster) : base(sm, monster)
        {
        }

        public override void Enter()
        {
            // 待机动画
            monster.monsterData.isMoveEngaged = false;
        }

        public override void Update()
        {
            // 待机状态可以做一些待机逻辑
        }

        public override void Exit()
        {
        }
    }
}
