using System.Diagnostics;

namespace Burntwax
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaIdleState")]
    public class DualBladeIdleState : DualBladeBaseState
    {

        public DualBladeIdleState(DualBladeStateMachine currentContext, DualBladeStateFactory dualBladeStateFactory)
        : base(currentContext, dualBladeStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {
            if(Ctx.ActiveDualBladeScriptable != null)
            {
                Ctx.camStateMachine.SlowCam(false);
                if (Ctx.fullyEquipped)
                    Ctx.PlayDualBladeAnimation(DualBladeAnimationType.Idle);
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
