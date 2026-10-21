namespace Burntwax
{
    /// <summary>
    /// Executes a speed-qualified held reversal using heavy combat data.
    /// Heavy-specific animation, recovery, hit-stop, and stamina behavior can
    /// be added to this lifecycle without changing the light state.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaHeavySlashState")]
    public sealed class DualBladeHeavySlashState : DualBladeSlashStateBase
    {
        public DualBladeHeavySlashState(
            DualBladeStateMachine currentContext,
            DualBladeStateFactory dualBladeStateFactory)
            : base(currentContext, dualBladeStateFactory)
        {
        }

        public override void EnterState()
        {
            // This explicit value is written to DualBladeStateMachine before the
            // damage window opens. BladeDetector then selects heavy damage and
            // heavy impact values from the dualBlade configs.
            EnterSlash(DualBladeAttackStrength.Heavy);
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
