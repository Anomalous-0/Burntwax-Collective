using System.Diagnostics;

namespace Burntwax
{
    public class KatanaIdleState : KatanaBaseState
    {

        public KatanaIdleState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {
            // Ctx.camStateMachine.SetCameraInputActive(true);
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
            if ( InputManager.Instance.attackIsPressed)
            {
                SwitchState(Factory.Ready());
            }
            else if(InputManager.Instance.aimIsPressed)
            {
                SwitchState(Factory.Parry());
            }
            else if (InputPrioritySorter.Instance.SprintIsPriority() && Ctx.stateMachine.currentState.CurrentSubstate() != Ctx.stateMachine.states.Idle())
            {
                SwitchState(Factory.Stow());
            }

        }


        public override void InitializeSubState()
        {


        }

    }
}