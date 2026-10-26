using System.Collections.Generic;
using FusionContinent;

namespace Game.StateMachine
{
    public abstract class State
    {
        protected StateMachine stateMachine;
        protected MonsterCard monster;  // 添加 MonsterCard 引用
        protected SoldierCard soldier;
        
        List<Transition> transitions = new List<Transition>();

        public State(StateMachine sm, MonsterCard monster)
        {
            stateMachine = sm;
            this.monster = monster;
        }
        
        public State(StateMachine sm, SoldierCard soldier)
        {
            stateMachine = sm;
            this.soldier = soldier;
        }

        public void AddTransition(Transition transition)
        {
            transitions.Add(transition);
        }

        public virtual State CheckTransition()
        {
            foreach (var transition in transitions)
            {
                if (transition.Condition())
                {
                    return transition.TargetState;
                }
            }

            return null;
        }

        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }
}
