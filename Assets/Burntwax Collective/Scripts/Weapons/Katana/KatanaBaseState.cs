namespace Burntwax
{
    public abstract class KatanaBaseState
    {

        private bool isRootState = false;
        private KatanaStateMachine ctx;
        private KatanaStateFactory factory;
        private KatanaBaseState currentSubState;
        private KatanaBaseState currentSuperState;

        protected bool IsRootState { set { isRootState = value; } }
        protected KatanaStateMachine Ctx { get { return ctx; } }
        protected KatanaStateFactory Factory { get { return factory; } }


        public KatanaBaseState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        {
            ctx = currentContext;
            factory = katanaStateFactory;
        }


        public KatanaBaseState CurrentSubstate()
        {
            return currentSubState;
        }

        public KatanaBaseState CurrentSuperstate()
        {
            return currentSuperState;
        }
        public abstract void EnterState();

        public abstract void UpdateState();

        public abstract void ExitState();

        public abstract void CheckSwitchStates();

        public abstract void InitializeSubState();

        public void UpdateStates()
        {
            UpdateState();
            if (currentSubState != null)
            {
                currentSubState.UpdateStates();
            }
        }

        protected void SwitchState(KatanaBaseState newState)
        {
            // current state exists state
            ExitState();

            // new state enters state
            newState.EnterState();

            if (isRootState)
            {
                // switch current state of context
                ctx.CurrentState = newState;
            }
            else if (currentSuperState != null)
            {
                // set the current super state's substate to the new state
                currentSuperState.SetSubState(newState);
            }

        }

        protected void SetSuperState(KatanaBaseState newSuperState)
        {
            currentSuperState = newSuperState;
        }

        protected void SetSubState(KatanaBaseState newSubState)
        {
            currentSubState = newSubState;
            newSubState.SetSuperState(this);
        }

       
    }
}