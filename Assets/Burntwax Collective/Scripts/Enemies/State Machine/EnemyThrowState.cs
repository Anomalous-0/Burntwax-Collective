namespace Burntwax
{
    /// <summary>
    /// Brain-level wrapper around StraightLinePickupEmitter. This state owns
    /// the decision lifecycle; the emitter is only a reusable launch executor.
    /// </summary>
    public sealed class EnemyThrowState : EnemyBaseState
    {
        private EnemyProjectileThrowConfig queuedThrow;
        private EnemyProjectileThrowConfig selectedThrow;
        private bool throwAttempted;

        public override string DebugName => "Throw";

        public EnemyThrowState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Action)
        {
        }

        public void QueueThrow(EnemyProjectileThrowConfig throwConfig)
        {
            queuedThrow = throwConfig;
        }

        public override void EnterState()
        {
            selectedThrow = queuedThrow;
            queuedThrow = null;
            throwAttempted = true;

            StraightLinePickupEmitter executor =
                Ctx.ProjectileThrowExecutor;
            if (executor == null
                || selectedThrow == null
                || !executor.TryEmit(
                    selectedThrow,
                    Ctx.Target,
                    Ctx.gameObject))
            {
                return;
            }

            Ctx.MarkCombatActionStarted(selectedThrow);
            Ctx.PlayCombatActionAnimation(selectedThrow);
        }

        public override void UpdateState()
        {
            if (throwAttempted)
                SwitchState(Factory.FaceTarget());
        }

        public override void FixedUpdateState() { }

        public override void ExitState()
        {
            selectedThrow = null;
            throwAttempted = false;
        }

        public override void CheckSwitchStates() { }
        public override void InitializeSubState() { }
    }
}
