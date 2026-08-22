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
            if(Ctx.ActiveKatanaScriptable != null)
                Ctx.camStateMachine.SlowCam(false);
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
            if (InputManager.Instance.attackIsPressed)
            {
                SwitchState(Factory.Ready());
            }
            else if (InputManager.Instance.aimIsPressed)
            {
                SwitchState(Factory.Parry());
            }
        }


        public override void InitializeSubState()
        {


        }

    }
}
