using Debug = UnityEngine.Debug;

namespace Burntwax
{
    public class KatanaSlashState : KatanaBaseState
    {
        public KatanaSlashState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }


        public override void EnterState()
        {
    
            InputManager.Instance.slash = false;
            Ctx.katanaDriver.BeginSwing(InputManager.Instance.slashStartDirection, "Slash");

            Ctx.camStateMachine.SetCameraInputActive(false);

            Ctx.anim.SetBool(Ctx.slashHash, true);
            Ctx.isSlashing = true;

            InitializeSubState();
        }

        public override void UpdateState()
        {
            if (InputManager.Instance.isTrackingSlash)
            {
                Ctx.katanaDriver.FeedInput(InputManager.Instance.LookInput);
            }
           

            CheckSwitchStates();
        }

        public override void ExitState()
        {
            Ctx.katanaDriver.EndSwing();
            Ctx.camStateMachine.SetCameraInputActive(true);
            Ctx.anim.SetBool(Ctx.slashHash, false);


        }

        public override void CheckSwitchStates()
        {
            if (!Ctx.isSlashing && !InputManager.Instance.attackIsPressed)
            {
                if (InputManager.Instance.parry)
                {
                    SwitchState(Factory.Parry());
                }
                else if (InputPrioritySorter.Instance.SprintIsPriority()
                    && Ctx.stateMachine.currentState.CurrentSubstate()
                        != Ctx.stateMachine.states.Idle())
                {
                    SwitchState(Factory.Stow());
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