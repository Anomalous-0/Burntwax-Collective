namespace Burntwax
{
    /// <summary>
    /// Identifies which concurrent root branch a state belongs to.
    ///
    /// The player currently needs one root state at a time. Enemies need an
    /// additional layer because they must be able to move and perform an action
    /// simultaneously. Inside each branch, states still use the same familiar
    /// root/superstate/substate structure as PlayerBaseState.
    /// </summary>
    public enum EnemyStateRegion
    {
        Life,
        Movement,
        Action
    }

    /// <summary>
    /// Base class shared by every enemy state.
    ///
    /// Its public shape intentionally mirrors PlayerBaseState:
    /// EnterState, UpdateState, ExitState, CheckSwitchStates,
    /// InitializeSubState, CurrentSubstate, SwitchState, SetSuperState, and
    /// SetSubState. The tree-entry helpers only centralize recursive setup and
    /// cleanup so cached factory states cannot retain stale children.
    /// </summary>
    public abstract class EnemyBaseState
    {
        private readonly EnemyStateMachine context;
        private readonly EnemyStateFactory factory;
        private readonly EnemyStateRegion region;

        private bool isRootState;
        private EnemyBaseState currentSubState;
        private EnemyBaseState currentSuperState;

        protected bool IsRootState
        {
            set => isRootState = value;
        }

        protected EnemyStateMachine Ctx => context;
        protected EnemyStateFactory Factory => factory;

        internal EnemyStateRegion Region => region;
        public virtual string DebugName => GetType().Name;

        protected EnemyBaseState(
            EnemyStateMachine currentContext,
            EnemyStateFactory enemyStateFactory,
            EnemyStateRegion stateRegion)
        {
            context = currentContext;
            factory = enemyStateFactory;
            region = stateRegion;
        }

        public EnemyBaseState CurrentSubstate()
        {
            return currentSubState;
        }

        public abstract void EnterState();
        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void ExitState();
        public abstract void CheckSwitchStates();
        public abstract void InitializeSubState();

        /// <summary>
        /// Updates the superstate first and then its deepest active substate,
        /// matching the update order used by the player hierarchy.
        /// </summary>
        public void UpdateStates()
        {
            UpdateState();
            currentSubState?.UpdateStates();
        }

        public void FixedUpdateStates()
        {
            FixedUpdateState();
            currentSubState?.FixedUpdateStates();
        }

        /// <summary>
        /// Root states replace their branch root. Substates replace themselves
        /// beneath their existing superstate. Callers therefore do not need to
        /// know where in the hierarchy they currently live.
        /// </summary>
        protected void SwitchState(EnemyBaseState newState)
        {
            if (newState == null || ReferenceEquals(this, newState))
                return;

            if (isRootState)
            {
                Ctx.ChangeRootState(region, newState);
            }
            else if (currentSuperState != null)
            {
                currentSuperState.ReplaceSubState(newState);
            }
        }

        protected void SetSuperState(EnemyBaseState newSuperState)
        {
            currentSuperState = newSuperState;
        }

        protected void SetSubState(EnemyBaseState newSubState)
        {
            currentSubState = newSubState;
            newSubState?.SetSuperState(this);
        }

        internal void EnterStateTree()
        {
            EnterState();
            InitializeSubState();
            currentSubState?.EnterStateTree();
        }

        internal void ExitStateTree()
        {
            if (currentSubState != null)
            {
                currentSubState.ExitStateTree();
                currentSubState = null;
            }

            ExitState();
            currentSuperState = null;
        }

        private void ReplaceSubState(EnemyBaseState newSubState)
        {
            if (ReferenceEquals(currentSubState, newSubState))
                return;

            currentSubState?.ExitStateTree();
            SetSubState(newSubState);
            currentSubState?.EnterStateTree();
        }
    }
}
