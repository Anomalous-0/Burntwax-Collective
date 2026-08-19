namespace Burntwax
{
    /// <summary>
    /// Executes a qualified flick released without a heavy reversal.
    /// </summary>
    public sealed class KatanaLightSlashState : KatanaSlashStateBase
    {
        public KatanaLightSlashState(
            KatanaStateMachine currentContext,
            KatanaStateFactory katanaStateFactory)
            : base(currentContext, katanaStateFactory)
        {
        }

        public override void EnterState()
        {
            EnterSlash(KatanaAttackStrength.Light);
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
