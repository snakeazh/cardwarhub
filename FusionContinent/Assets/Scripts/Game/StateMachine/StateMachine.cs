namespace Game.StateMachine
{
    public class StateMachine
    {
        public State CurrentState;

        public void ChangeState(State newState)
        {
            CurrentState?.Exit();

            CurrentState = newState;

            CurrentState.Enter();
        }

        public void Update()
        {
            var nextState = CurrentState.CheckTransition();

            if (nextState != null)
            {
                ChangeState(nextState);
            }

            CurrentState?.Update();
        }
    }
}