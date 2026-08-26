namespace Burntwax
{
    public sealed class EnemyIdleState : EnemyBaseState
    {
        public override string DebugName => "Idle";

        public EnemyIdleState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Action)
        {
        }

        public override void EnterState()
        {
            Ctx.PlayDefaultAnimation();
        }
        public override void UpdateState() { }
        public override void FixedUpdateState() { }
        public override void ExitState() { }
        public override void CheckSwitchStates() { }
        public override void InitializeSubState() { }
    }
}
