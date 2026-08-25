namespace Burntwax
{
    /// <summary>
    /// Brain-level wrapper around EnemyMeleeAttack. The executor continues to
    /// own animation damage windows, hit resolution, recovery, and parry events.
    /// This state decides when the action starts and when another action may run.
    /// </summary>
    public sealed class EnemyMeleeState : EnemyBaseState
    {
        private bool attackStarted;

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
            attackStarted = Ctx.MeleeAttack != null
                && Ctx.MeleeAttack.TryBeginAttack();
        }

        public override void UpdateState()
        {
            EnemyMeleeAttack melee = Ctx.MeleeAttack;
            if (!attackStarted || melee == null)
            {
                SwitchState(Factory.FaceTarget());
                return;
            }

            if (Ctx.FaceTargetDuringMeleeWindup
                && melee.CurrentState == EnemyMeleeAttackState.Windup)
            {
                Ctx.RequestFacingTarget();
            }

            CheckSwitchStates();
        }

        public override void FixedUpdateState() { }

        public override void ExitState()
        {
            attackStarted = false;
        }

        public override void CheckSwitchStates()
        {
            EnemyMeleeAttack melee = Ctx.MeleeAttack;
            if (melee == null)
            {
                SwitchState(Factory.FaceTarget());
            }
            else if (melee.CurrentState == EnemyMeleeAttackState.Parried)
            {
                SwitchState(Factory.Staggered());
            }
            else if (melee.CurrentState == EnemyMeleeAttackState.Ready)
            {
                SwitchState(Factory.FaceTarget());
            }
        }

        public override void InitializeSubState() { }
    }
}
