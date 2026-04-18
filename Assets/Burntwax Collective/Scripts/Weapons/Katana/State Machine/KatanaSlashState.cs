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

        int timeout;

        public override void EnterState()
        {
            timeout = Ctx.slashTimeout;
            InputManager.Instance.slash = false;
            Debug.Log("Entered slash state");
            InitializeSubState();
        }

        public override void UpdateState()
        {
            timeout--;
            CheckSwitchStates();

        }

        public override void ExitState()
        {
        }

        public override void CheckSwitchStates()
        {
            if (timeout <= 0)
            {
                if (InputManager.Instance.parry)
                {
                    SwitchState(Factory.Parry());
                }
                else if (InputPrioritySorter.Instance.SprintIsPriority() && Ctx.stateMachine.currentState.CurrentSubstate() != Ctx.stateMachine.states.Idle())
                {
                    SwitchState(Factory.Stow());
                }
                else
                {
                    SwitchState(Factory.Idle());
                }
            }
        }


        public override void InitializeSubState()
        {


        }

    }
}