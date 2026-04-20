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
            if (InputManager.Instance.aimIsPressed || InputManager.Instance.attackIsPressed)
            {
                SwitchState(Factory.Ready());
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