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
            if ((InputManager.Instance.attackIsPressed || InputManager.Instance.aimIsPressed) && (InputPrioritySorter.Instance.AttackIsPriority() || InputPrioritySorter.Instance.AimIsPriority()))
            {
                SwitchState(Factory.Ready());
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