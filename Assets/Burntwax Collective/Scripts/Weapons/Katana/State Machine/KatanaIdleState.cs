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
            {
                Ctx.camStateMachine.SlowCam(false);
                if (Ctx.fullyEquipped)
                    Ctx.PlayKatanaAnimation(KatanaAnimationType.Idle);
            }
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
            // The equip clip must finish before Idle accepts combat input.
            if (!Ctx.fullyEquipped)
                return;

            if (InputManager.Instance.attackIsPressed)
            {
                SwitchState(Factory.Ready());
            }
            else if (InputManager.Instance.aimIsPressed)
            {
                SwitchState(Factory.Parry());
            }
            else if (InputPrioritySorter.Instance.SprintIsPriority())
            {
                SwitchState(Factory.Stow());
            }
        }


        public override void InitializeSubState()
        {


        }

    }
}
