namespace Burntwax
{
    // Branch B, step 1: charge pulse. Stop-sign pose; movement slows as the charge builds (capped at
    // 1s in the recognizer). Releasing the button fires the blast.
    public class GloveChargeBlastState : GloveBaseState
    {
        public GloveChargeBlastState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
           
            InitializeSubState();
        }

        public override void UpdateState()
        {
            Ctx.SetChargeSlow(InputManager.Instance.gloveChargeNormalized);
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            Ctx.ClearChargeSlow();
        }

        public override void CheckSwitchStates()
        {
            if (InputManager.Instance.gloveBlastFired)
            {
                SwitchState(Factory.Blast());
            }
            else if (!InputManager.Instance.gloveCharging)
            {
                // Safety net: charge ended without a fire pulse — return to idle.
                SwitchState(Factory.Idle());
            }
        }

        public override void InitializeSubState() { }
    }
}
