namespace Burntwax
{
    /// <summary>
    /// Top-level Life root. Movement and Action branches exist only while this
    /// state is active, so death shuts both systems down in one place.
    /// </summary>
    public sealed class EnemyAliveState : EnemyBaseState
    {
        public override string DebugName => "Alive";

        public EnemyAliveState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Life)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            Ctx.StartAliveRegions();
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void FixedUpdateState() { }

        public override void ExitState()
        {
            Ctx.StopAliveRegions();
        }

        public override void CheckSwitchStates()
        {
            // The health event normally changes state immediately. This is a
            // fallback for loading an enemy that was already dead.
            if (Ctx.Health.IsDead)
                SwitchState(Factory.Dead());
        }

        public override void InitializeSubState() { }
    }
}
