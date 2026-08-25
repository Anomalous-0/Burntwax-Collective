namespace Burntwax
{
    /// <summary>
    /// Initial Movement root. Falling, Jumping, Flying, and Swimming can later
    /// become sibling roots, while locomotion choices remain its substates.
    /// </summary>
    public sealed class EnemyGroundedState : EnemyBaseState
    {
        public override string DebugName => "Grounded";

        public EnemyGroundedState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Movement)
        {
            IsRootState = true;
        }

        public override void EnterState() { }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void FixedUpdateState() { }
        public override void ExitState() { }
        public override void CheckSwitchStates() { }

        public override void InitializeSubState()
        {
            SetSubState(Factory.Stationary());
        }
    }
}
