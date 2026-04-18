using System.Diagnostics;
using Debug = UnityEngine.Debug;
namespace Burntwax
{
    public class KatanaStowState : KatanaBaseState
    {

        public KatanaStowState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {

            Debug.Log("Entered stow state");
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
            if (Ctx.stateMachine.currentState.CurrentSubstate() != Ctx.stateMachine.states.Sprint() && !InputPrioritySorter.Instance.AimIsPriority() || InputPrioritySorter.Instance.ShootIsPriority())
            {
                SwitchState(Factory.Idle());
            }
            else if (InputManager.Instance.slash)
            {
                SwitchState(Factory.Slash());
            }
            else if (InputManager.Instance.parry)
            {
                SwitchState(Factory.Parry());
            }
        }


        public override void InitializeSubState()
        {


        }

    }
}