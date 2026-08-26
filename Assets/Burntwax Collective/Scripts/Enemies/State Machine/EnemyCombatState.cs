namespace Burntwax
{
    /// <summary>
    /// Combat Action root. Target validity belongs here so future melee, ranged,
    /// blocking, and special-attack substates do not duplicate that concern.
    /// </summary>
    public sealed class EnemyCombatState : EnemyBaseState
    {
        public override string DebugName => "Combat";

        public EnemyCombatState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Action)
        {
            IsRootState = true;
        }

        public override void EnterState() { }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void FixedUpdateState() { }

        public override void ExitState()
        {
            EnemyMeleeExecutor executor = Ctx.MeleeExecutor;
            if (executor != null
                && executor.CurrentPhase != EnemyMeleeExecutionPhase.Ready
                && executor.CurrentPhase != EnemyMeleeExecutionPhase.Disabled)
            {
                executor.CancelAttack();
            }
        }

        public override void CheckSwitchStates()
        {
            if (!Ctx.HasValidTarget)
                SwitchState(Factory.Unaware());
        }

        public override void InitializeSubState()
        {
            SetSubState(Factory.FaceTarget());
        }
    }
}
