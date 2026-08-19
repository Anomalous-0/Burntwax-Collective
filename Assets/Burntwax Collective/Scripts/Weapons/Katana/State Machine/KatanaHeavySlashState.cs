namespace Burntwax
{
    /// <summary>
    /// Executes a speed-qualified held reversal using heavy combat data.
    /// Heavy-specific animation, recovery, hit-stop, and stamina behavior can
    /// be added to this lifecycle without changing the light state.
    /// </summary>
    public sealed class KatanaHeavySlashState : KatanaSlashStateBase
    {
        public KatanaHeavySlashState(
            KatanaStateMachine currentContext,
            KatanaStateFactory katanaStateFactory)
            : base(currentContext, katanaStateFactory)
        {
        }

        public override void EnterState()
        {
            // This explicit value is written to KatanaStateMachine before the
            // damage window opens. BladeDetector then selects heavy damage and
            // heavy impact values from the katana configs.
            EnterSlash(KatanaAttackStrength.Heavy);
            InitializeSubState();
        }

        public override void UpdateState()
        {
            UpdateSlash();
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            ExitSlash();
        }

        public override void CheckSwitchStates()
        {
            CheckSlashTransitions();
        }

        public override void InitializeSubState()
        {
        }
    }
}
