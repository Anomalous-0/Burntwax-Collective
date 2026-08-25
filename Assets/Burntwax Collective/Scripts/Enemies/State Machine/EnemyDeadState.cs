namespace Burntwax
{
    /// <summary>
    /// Top-level Life root. Ragdoll, dismemberment, corpse, or loot states can
    /// later be added beneath this root without putting presentation in health.
    /// </summary>
    public sealed class EnemyDeadState : EnemyBaseState
    {
        public override string DebugName => "Dead";

        public EnemyDeadState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Life)
        {
            IsRootState = true;
        }

        public override void EnterState() { }
        public override void UpdateState() { }
        public override void FixedUpdateState() { }
        public override void ExitState() { }
        public override void CheckSwitchStates() { }
        public override void InitializeSubState() { }
    }
}
