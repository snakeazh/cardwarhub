using System;

namespace Game.StateMachine
{
    public class Transition
    {
        public Func<bool> Condition;

        public State TargetState;

        public Transition(Func<bool> condition, State target)
        {
            Condition = condition;
            TargetState = target;
        }
    }
}