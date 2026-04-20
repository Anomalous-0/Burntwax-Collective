using Debug = UnityEngine.Debug;

namespace Burntwax
{
    public class KatanaReadyState : KatanaBaseState
    {
        public KatanaReadyState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }


        public override void EnterState()
        {

            Debug.Log("Ready state");


            Ctx.camStateMachine.SetCameraInputActive(false);
            Ctx.anim.SetBool(Ctx.readyHash, true);


            InitializeSubState();
        }

        public override void UpdateState()
        {





            CheckSwitchStates();
        }

        public override void ExitState()
        {

            Ctx.camStateMachine.SetCameraInputActive(true);
            Ctx.anim.SetBool(Ctx.readyHash, false);


        }

        public override void CheckSwitchStates()
        {
            if (InputManager.Instance.slash)
            {
                SwitchState(Factory.Slash());
            }
            else if (InputManager.Instance.parry)
            {
                SwitchState(Factory.Parry());
            }
            else if (InputPrioritySorter.Instance.SprintIsPriority() && Ctx.stateMachine.currentState.CurrentSubstate() != Ctx.stateMachine.states.Idle())
            {
                SwitchState(Factory.Stow());
            }
            else if (!InputManager.Instance.aimIsPressed && !InputManager.Instance.attackIsPressed)
            {
                SwitchState(Factory.Idle());
            }

        }

        public override void InitializeSubState() { }
    }
}