namespace Burntwax
{
    /// <summary>
    /// Brain-level wrapper around EnemyMeleeExecutor. The executor continues to
    /// own animation damage windows, hit resolution, recovery, and parry events.
    /// This state decides when the action starts and when another action may run.
    /// </summary>
    public sealed class EnemyMeleeState : EnemyBaseState
    {
        private bool attackStarted;
        private EnemyMeleeAttackConfig selectedAttack;

        public override string DebugName => "Melee";

        public EnemyMeleeState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Action)
        {
        }

        public override void EnterState()
        {
            // Cached states are reused, so entry-specific fields reset here.
            selectedAttack = Ctx.SelectMeleeAttack();
            attackStarted = Ctx.MeleeExecutor != null
                && Ctx.MeleeExecutor.TryBeginAttack(selectedAttack);

            if (attackStarted)
                Ctx.PlayMeleeAnimation(selectedAttack);
        }

        public override void UpdateState()
        {
            EnemyMeleeExecutor executor = Ctx.MeleeExecutor;
            if (!attackStarted || executor == null)
            {
                SwitchState(Factory.FaceTarget());
                return;
            }

            if (Ctx.FaceTargetDuringMeleeStartup
                && executor.CurrentPhase == EnemyMeleeExecutionPhase.Startup)
            {
                Ctx.RequestFacingTarget();
            }

            CheckSwitchStates();
        }

        public override void FixedUpdateState() { }

        public override void ExitState()
        {
            attackStarted = false;
            selectedAttack = null;
        }

        public override void CheckSwitchStates()
        {
            EnemyMeleeExecutor executor = Ctx.MeleeExecutor;
            if (executor == null)
            {
                SwitchState(Factory.FaceTarget());
            }
            else if (executor.CurrentPhase == EnemyMeleeExecutionPhase.Parried)
            {
                SwitchState(Factory.Staggered());
            }
            else if (executor.CurrentPhase == EnemyMeleeExecutionPhase.Ready)
            {
                SwitchState(Factory.FaceTarget());
            }
        }

        public override void InitializeSubState() { }
    }
}
