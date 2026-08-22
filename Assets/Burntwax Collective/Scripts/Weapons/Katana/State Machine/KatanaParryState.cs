namespace Burntwax
{
    public class KatanaParryState : KatanaBaseState
    {
        public KatanaParryState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }



        public override void EnterState()
        {
            // Ctx.camStateMachine.SetCameraInputActive(false);

            Ctx.anim.SetBool(Ctx.parryHash, true);
            Ctx.isParrying = true;
            InitializeSubState();
        }

        public override void UpdateState()
        {

            
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            Ctx.katanaDriver.EndSwing();
            Ctx.camStateMachine.SlowCam(false);
            Ctx.anim.SetBool(Ctx.parryHash, false);
            Ctx.isParryWindowActive = false;


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
