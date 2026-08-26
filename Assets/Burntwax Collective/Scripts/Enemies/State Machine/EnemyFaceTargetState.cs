namespace Burntwax
{
    /// <summary>
    /// Faces the target and asks the generic combat selector for an action.
    /// Concrete action configs route into their matching cached action state
    /// without changing loadouts, evolution traits, or scoring rules.
    /// </summary>
    public sealed class EnemyFaceTargetState : EnemyBaseState
    {
        public override string DebugName => "FaceTarget";

        public EnemyFaceTargetState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Action)
        {
        }

        public override void EnterState()
        {
            Ctx.PlayDefaultAnimation();
        }

        public override void UpdateState()
        {
            Ctx.RequestFacingTarget();
            CheckSwitchStates();
        }

        public override void FixedUpdateState() { }
        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (!Ctx.TrySelectCombatAction(
                    out EnemyCombatActionConfig selectedAction))
            {
                return;
            }

            switch (selectedAction)
            {
                case EnemyMeleeAttackConfig meleeAttack:
                    EnemyMeleeState meleeState = Factory.Melee();
                    meleeState.QueueAttack(meleeAttack);
                    SwitchState(meleeState);
                    break;

                case EnemyProjectileThrowConfig projectileThrow:
                    EnemyThrowState throwState = Factory.Throw();
                    throwState.QueueThrow(projectileThrow);
                    SwitchState(throwState);
                    break;
            }
        }

        public override void InitializeSubState() { }
    }
}
