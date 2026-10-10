namespace Burntwax
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaBaseState")]
    public abstract class DualBladeBaseState
    {

        private bool isRootState = false;
        private DualBladeStateMachine ctx;
        private DualBladeStateFactory factory;
        private DualBladeBaseState currentSubState;
        private DualBladeBaseState currentSuperState;

        protected bool IsRootState { set { isRootState = value; } }
        protected DualBladeStateMachine Ctx { get { return ctx; } }
        protected DualBladeStateFactory Factory { get { return factory; } }


        public DualBladeBaseState(DualBladeStateMachine currentContext, DualBladeStateFactory dualBladeStateFactory)
        {
            ctx = currentContext;
            factory = dualBladeStateFactory;
        }


        public DualBladeBaseState CurrentSubstate()
        {
            return currentSubState;
        }

        public DualBladeBaseState CurrentSuperstate()
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

        protected void SwitchState(DualBladeBaseState newState)
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

        protected void SetSuperState(DualBladeBaseState newSuperState)
        {
            currentSuperState = newSuperState;
        }

        protected void SetSubState(DualBladeBaseState newSubState)
        {
            currentSubState = newSubState;
            newSubState.SetSuperState(this);
        }

       
    }
}