using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Short action lock entered when this katana's active slash is perfectly
    /// parried. Animation playback can be added without changing the gameplay
    /// state or parry-reaction contract.
    /// </summary>
    public sealed class KatanaParriedRecoilState : KatanaBaseState
    {
        private float recoveryEndTime;

        public KatanaParriedRecoilState(
            KatanaStateMachine currentContext,
            KatanaStateFactory katanaStateFactory)
            : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            Ctx.CloseDamageWindow();
            Ctx.katanaDriver.EndSwing();
            Ctx.ResetGestureRecognition();
            Ctx.camStateMachine.SlowCam(false);
            Ctx.anim.SetBool(Ctx.slashHash, false);
            Ctx.isSlashing = false;
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
