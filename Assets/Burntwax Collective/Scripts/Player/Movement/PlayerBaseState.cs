namespace Burntwax
{
    public abstract class PlayerBaseState
    {

        private bool isRootState = false;
        private bool usesSubState = true;
        private PlayerStateMachine ctx;
        private PlayerStateFactory factory;
        private PlayerBaseState currentSubState;
        private PlayerBaseState currentSuperState;

        protected bool IsRootState { set { isRootState = value; } }
        protected bool UsesSubState { set { usesSubState = value; } }
        protected PlayerStateMachine Ctx { get { return ctx; } }
        protected PlayerStateFactory Factory { get { return factory; } }

        protected bool TryStartDash()
        {
            if (isRootState || InputManager.Instance == null
                || !InputManager.Instance.dashIsPressed)
                return false;

            DualBladeStateMachine dualBlade = Ctx.DualBladeMachine;
            if (dualBlade == null || !dualBlade.CanDash)
                return false;

            InputManager.Instance.ConsumeDashInput();
            SwitchState(Factory.Dash());
            return true;
        }

        protected bool TryStartMantle()
        {
            if (!isRootState || InputManager.Instance == null
                || !InputManager.Instance.jumpIsPressed
                || !Ctx.TryGetMantleTarget(out PlayerMantleTarget target))
                return false;

            PlayerMantleState mantleState = Factory.Mantle();
            mantleState.SetTarget(target);
            SwitchState(mantleState);
            return true;
        }


        public PlayerBaseState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        {
            ctx = currentContext;
            factory = playerStateFactory;
        }


        public PlayerBaseState CurrentSubstate()
        {
            return currentSubState;
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

        protected void SwitchState(PlayerBaseState newState)
        {

            PlayerBaseState currentSubState = ctx.CurrentState.currentSubState;
            bool enteringActionRoot = isRootState && !newState.usesSubState;

            if (enteringActionRoot && currentSubState != null)
            {
                currentSubState.ExitState();
                ctx.CurrentState.currentSubState = null;
            }

            // current state exists state
            ExitState();

            newState.EnterState();

            if (isRootState)
            {
                // switch current state of context
                ctx.CurrentState = newState;

                if (!newState.usesSubState)
                {
                    ctx.CurrentState.currentSubState = null;
                }
                else if (usesSubState && currentSubState != null)
                {
                    ctx.CurrentState.SetSubState(currentSubState);
                }
                else
                {
                    ctx.CurrentState.currentSubState = null;
                    ctx.CurrentState.InitializeSubState();

                    if (ctx.CurrentState.currentSubState != null)
                        ctx.CurrentState.currentSubState.EnterState();
                }
            }
            else if (currentSuperState != null)
            {
                // set the current super state's substate to the new state
                currentSuperState.SetSubState(newState);
            }

        }

        protected void SetSuperState(PlayerBaseState newSuperState)
        {
            currentSuperState = newSuperState;
        }

        protected void SetSubState(PlayerBaseState newSubState)
        {
            currentSubState = newSubState;
            newSubState.SetSuperState(this);
        }
    }

}
