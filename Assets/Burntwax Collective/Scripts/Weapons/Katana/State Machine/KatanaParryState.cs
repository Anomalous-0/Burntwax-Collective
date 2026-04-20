using Codice.Client.Common;

namespace Burntwax
{
    public class KatanaParryState : KatanaBaseState
    {

        public KatanaParryState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;

        }
        int timeout;
        public override void EnterState()
        {
            timeout = Ctx.parryTimeout;
            UnityEngine.Debug.Log("Entered parry state");
            InputManager.Instance.parry = false;
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
                if (InputManager.Instance.slash)
                {
                    SwitchState(Factory.Slash());
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