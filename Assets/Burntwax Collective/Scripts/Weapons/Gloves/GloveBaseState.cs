namespace Burntwax
{
    public abstract class GloveBaseState
    {

        private bool isRootState = false;
        private GloveStateMachine ctx;
        private GloveStateFactory factory;
        private GloveBaseState currentSubState;
        private GloveBaseState currentSuperState;

        protected bool IsRootState { set { isRootState = value; } }
        protected GloveStateMachine Ctx { get { return ctx; } }
        protected GloveStateFactory Factory { get { return factory; } }


        public GloveBaseState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        {
            ctx = currentContext;
            factory = gloveStateFactory;
        }


        public GloveBaseState CurrentSubstate()
        {
            return currentSubState;
        }

        public GloveBaseState CurrentSuperstate()
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

        protected void SwitchState(GloveBaseState newState)
        {
            ExitState();

            if (isRootState)
            {
                ctx.CurrentState = newState;
                newState.EnterState();
            }
            else if (currentSuperState != null)
            {
                currentSuperState.EnterSubState(newState);
            }
        }

        protected void SetSuperState(GloveBaseState newSuperState)
        {
            currentSuperState = newSuperState;
        }

        protected void EnterSubState(GloveBaseState newSubState)
        {
            currentSubState = newSubState;
            newSubState.SetSuperState(this);
            newSubState.EnterState();
        }

       
    }
}
