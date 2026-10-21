namespace Burntwax
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaParryState")]
    public class DualBladeParryState : DualBladeBaseState
    {
        public DualBladeParryState(DualBladeStateMachine currentContext, DualBladeStateFactory dualBladeStateFactory)
        : base(currentContext, dualBladeStateFactory)
        {
            IsRootState = true;
        }



        public override void EnterState()
        {
            // Ctx.camStateMachine.SetCameraInputActive(false);

            Ctx.PlayDualBladeAnimation(DualBladeAnimationType.Parry);
            Ctx.isParrying = true;
            InitializeSubState();
        }

        public override void UpdateState()
        {

            
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            Ctx.dualBladeDriver.EndSwing();
            Ctx.camStateMachine.SlowCam(false);
            Ctx.EndDefenseAnimation();


        }

        public override void CheckSwitchStates()
        {
            if (!Ctx.isParrying && !InputManager.Instance.aimIsPressed)
            {
                if(InputManager.Instance.attackIsPressed)
                {
                    SwitchState(Factory.Ready());
                }
                else
                {
                    SwitchState(Factory.Idle());
                }
            }
        }

        public override void InitializeSubState() { }
    }
}
