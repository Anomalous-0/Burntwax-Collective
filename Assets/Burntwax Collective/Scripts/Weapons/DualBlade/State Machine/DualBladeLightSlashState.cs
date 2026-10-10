namespace Burntwax
{
    /// <summary>
    /// Executes a qualified flick released without a heavy reversal.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaLightSlashState")]
    public sealed class DualBladeLightSlashState : DualBladeSlashStateBase
    {
        public DualBladeLightSlashState(
            DualBladeStateMachine currentContext,
            DualBladeStateFactory dualBladeStateFactory)
            : base(currentContext, dualBladeStateFactory)
        {
        }

        public override void EnterState()
        {
            EnterSlash(DualBladeAttackStrength.Light);
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
