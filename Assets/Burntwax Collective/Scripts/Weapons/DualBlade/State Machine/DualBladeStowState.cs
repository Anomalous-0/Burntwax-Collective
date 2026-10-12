using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
namespace Burntwax
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaStowState")]
    public class DualBladeStowState : DualBladeBaseState
    {

        public DualBladeStowState(DualBladeStateMachine currentContext, DualBladeStateFactory dualBladeStateFactory)
        : base(currentContext, dualBladeStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {

            Ctx.PlayDualBladeAnimation(DualBladeAnimationType.Stow);
            Ctx.fullyEquipped = true;
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
            if (InputManager.Instance.attackIsPressed && InputPrioritySorter.Instance.AttackIsPriority())
            {
                SwitchState(Factory.Ready());
            }
            else if (InputPrioritySorter.Instance.AimIsPriority())
            {
                SwitchState(Factory.Parry());
            }

            else if (!Ctx.IsSprintOrDashActive)
            {
                SwitchState(Factory.Idle());
            }

        }


        public override void InitializeSubState()
        {


        }

    }
}
