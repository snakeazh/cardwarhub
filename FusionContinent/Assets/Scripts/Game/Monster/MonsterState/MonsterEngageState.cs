using FusionContinent;

namespace Game.StateMachine
{
    public class MonsterEngageState : State
    {
        public MonsterEngageState(StateMachine sm, MonsterCard monster) : base(sm, monster)
        {
        }

        public override void Enter()
        {
            monster.Iswall = monster.targetSolidierCard == null;
            monster.MoveBehaviour.StopMove();
            monster.ResetEngageTarget();
        }

        public override void Update()
        {
            if (monster.targetSolidierCard == null || monster.targetSolidierCard.isDie)
            {
                monster.monsterData.monsterState = MonsterState.move;
                monster.Tick();
            }

            if (monster.hasEngagedTarget)
            {
                return;
            }

            if (monster.AttackBehaviour == null)
            {
                monster.hasEngagedTarget = true;
                return;
            }

            if (!GamePlayManager.Default.IsPaused())
            {
                monster.AttackBehaviour.BeginEngage(monster);
            }
            else
            {
                monster.AttackBehaviour.PauseEngage(monster);
            }

            monster.hasEngagedTarget = true;
        }

        public override void Exit()
        {
            if (monster.AttackBehaviour != null)
            {
                monster.AttackBehaviour.StopEngage(monster);
            }
        }
    }
}
