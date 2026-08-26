namespace Burntwax
{
    /// <summary>
    /// Faces the target and chooses the only action currently implemented. A
    /// future action scorer can choose melee, ranged, throws, blocks, or an
    /// evolution-granted action from this decision point.
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
            if (Ctx.CanBeginMeleeAttack())
                SwitchState(Factory.Melee());
        }

        public override void InitializeSubState() { }
    }
}
