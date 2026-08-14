using System.Diagnostics;
using UnityEngine;
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

            Ctx.anim.SetBool(Ctx.stowHash, true);
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
            Ctx.anim.SetBool(Ctx.stowHash, false);
        }

        public override void CheckSwitchStates()
        {
            if (InputManager.Instance.attackIsPressed && InputPrioritySorter.Instance.AttackIsPriority())
            {
                SwitchState(Factory.Ready());
            }
            else if (InputManager.Instance.aimIsPressed && InputPrioritySorter.Instance.AimIsPriority())
            {
                SwitchState(Factory.Parry());
            }

            else if (Ctx.stateMachine.currentState.CurrentSubstate() != Ctx.stateMachine.states.Sprint() && !InputPrioritySorter.Instance.SprintIsPriority())
            {
                SwitchState(Factory.Idle());
            }

        }


        public override void InitializeSubState()
        {


        }

    }
}