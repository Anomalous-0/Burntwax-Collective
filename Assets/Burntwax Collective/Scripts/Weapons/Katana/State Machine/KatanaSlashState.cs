using System.Diagnostics;
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

            Debug.Log("Entered Slash State");
            

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

            if(!InputManager.Instance.slash)
            {
                SwitchState(Factory.Idle());
            }
            else if (InputManager.Instance.parry)
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