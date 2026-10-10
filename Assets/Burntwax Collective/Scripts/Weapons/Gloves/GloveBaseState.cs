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

        public virtual void FixedUpdateState() { }

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

            // Hold/Attract can go straight to the root Unequipping state.
            // Exit their Equipped parent too, stopping its delayed-idle timer.
            if (newState.isRootState)
            {
                GloveBaseState parent = currentSuperState;
                while (parent != null)
                {
                    parent.currentSubState = null;
                    parent.ExitState();
                    parent = parent.currentSuperState;
                }
                currentSuperState = null;
                newState.currentSuperState = null;
                ctx.CurrentState = newState;
                newState.EnterState();
            }
            else if (isRootState)
            {
                ctx.CurrentState = newState;
                newState.EnterState();
            }
            else if (currentSuperState != null)
            {
                currentSuperState.EnterSubState(newState);
            }
        }

        public void FixedUpdateStates()
        {
            FixedUpdateState();
            if (currentSubState != null)
                currentSubState.FixedUpdateStates();
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
