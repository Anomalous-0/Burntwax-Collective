namespace Burntwax
{
    /// <summary>
    /// Non-combat Action root. A future perception system can add Suspicious and
    /// Alert siblings; the prototype currently enters Combat once a target exists.
    /// </summary>
    public sealed class EnemyUnawareState : EnemyBaseState
    {
        public override string DebugName => "Unaware";

        public EnemyUnawareState(
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
        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (Ctx.HasValidTarget)
                SwitchState(Factory.Combat());
        }

        public override void InitializeSubState()
        {
            SetSubState(Factory.Idle());
        }
    }
}
