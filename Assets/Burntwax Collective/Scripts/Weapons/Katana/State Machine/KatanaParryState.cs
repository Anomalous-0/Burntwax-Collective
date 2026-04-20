namespace Burntwax
{
    public class KatanaParryState : KatanaBaseState
    {
        public KatanaParryState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }


        public bool released = false;

        public override void EnterState()
        {
            released = false;
            InputManager.Instance.parry = false;
            Ctx.katanaDriver.BeginSwing(InputManager.Instance.slashStartDirection, "Parry");

            Ctx.camStateMachine.SetCameraInputActive(false);

            Ctx.anim.SetBool(Ctx.parryHash, true);
            Ctx.isParrying = true;
            InitializeSubState();
        }

        public override void UpdateState()
        {

            // ── NEW: Feed live mouse input ──
            if (InputManager.Instance.isTrackingParry)
            {
                Ctx.katanaDriver.FeedInput(InputManager.Instance.LookInput);
            }
            else if (!released)
            {
                released = true;
                Ctx.camStateMachine.SetCameraInputActive(true);
            }
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            Ctx.katanaDriver.EndSwing();
            Ctx.camStateMachine.SetCameraInputActive(true);
            Ctx.anim.SetBool(Ctx.parryHash, false);

        }

        public override void CheckSwitchStates()
        {
            if (!Ctx.isParrying && !InputManager.Instance.aimIsPressed)
            {
                if (InputManager.Instance.slash)
                {
                    SwitchState(Factory.Slash());
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