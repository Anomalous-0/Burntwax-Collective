using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Short action lock entered when this dualBlade's active slash is perfectly
    /// parried. Animation playback can be added without changing the gameplay
    /// state or parry-reaction contract.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaParriedRecoilState")]
    public sealed class DualBladeParriedRecoilState : DualBladeBaseState
    {
        private float recoveryEndTime;

        public DualBladeParriedRecoilState(
            DualBladeStateMachine currentContext,
            DualBladeStateFactory dualBladeStateFactory)
            : base(currentContext, dualBladeStateFactory)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            Ctx.CloseDamageWindow();
            Ctx.dualBladeDriver.EndSwing();
            Ctx.ResetGestureRecognition();
            Ctx.camStateMachine.SlowCam(false);
            Ctx.isSlashing = false;
            Ctx.PlayDualBladeAnimation(DualBladeAnimationType.ParriedRecoil);
            Ctx.ableToSlashInMiddleOfAnimation = false;

            recoveryEndTime = Time.time
                + Ctx.ParriedAttackRecoveryDuration;

            InitializeSubState();
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void ExitState()
        {
        }

        public override void CheckSwitchStates()
        {
            if (Time.time < recoveryEndTime)
                return;

            if (InputManager.Instance.aimIsPressed)
                SwitchState(Factory.Parry());
            else if (InputManager.Instance.attackIsPressed)
                SwitchState(Factory.Ready());
            else
                SwitchState(Factory.Idle());
        }

        public override void InitializeSubState()
        {
        }
    }
}
