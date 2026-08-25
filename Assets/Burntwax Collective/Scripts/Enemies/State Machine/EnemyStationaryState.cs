namespace Burntwax
{
    /// <summary>
    /// Stationary stops translation, but still consumes facing intent produced
    /// by Action states. Future Walk and Sprint states will use the same boundary.
    /// </summary>
    public sealed class EnemyStationaryState : EnemyBaseState
    {
        public override string DebugName => "Stationary";

        public EnemyStationaryState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Movement)
        {
        }

        public override void EnterState() { }

        public override void UpdateState()
        {
            Ctx.ApplyFacingRequest();
            CheckSwitchStates();
        }

        public override void FixedUpdateState() { }
        public override void ExitState() { }
        public override void CheckSwitchStates() { }
        public override void InitializeSubState() { }
    }
}
