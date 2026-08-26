namespace Burntwax
{
    /// <summary>
    /// Prevents a new action while the melee executor's parry reaction runs.
    /// Animation-authored stagger timing remains in the melee executor.
    /// </summary>
    public sealed class EnemyStaggeredState : EnemyBaseState
    {
        public override string DebugName => "Staggered";

        public EnemyStaggeredState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Action)
        {
        }

        public override void EnterState() { }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void FixedUpdateState() { }
        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            EnemyMeleeExecutor executor = Ctx.MeleeExecutor;
            if (executor == null
                || executor.CurrentPhase == EnemyMeleeExecutionPhase.Ready)
            {
                SwitchState(Factory.FaceTarget());
            }
        }

        public override void InitializeSubState() { }
    }
}
