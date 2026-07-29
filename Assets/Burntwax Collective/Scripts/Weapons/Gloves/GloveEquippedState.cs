using System.Collections;
using UnityEngine;

namespace Burntwax
{
    public class GloveEquippedState : GloveBaseState
    {
        private bool canCheckForUnequip;
        private Coroutine unequipDelayCoroutine;

        public GloveEquippedState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            Ctx.SetEquipped(true);
            canCheckForUnequip = false;

            unequipDelayCoroutine = Ctx.StartCoroutine(BeginUnequipCheckAfterDelay());
            InitializeSubState();
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            if (unequipDelayCoroutine != null)
            {
                Ctx.StopCoroutine(unequipDelayCoroutine);
                unequipDelayCoroutine = null;
            }
        }

        public override void CheckSwitchStates()
        {
            if (CurrentSubstate() == Ctx.states.Idle() && canCheckForUnequip)
            {
                SwitchState(Factory.Unequipping());
            }
        }

        public override void InitializeSubState()
        {
            GloveBaseState initialState;

            if (Ctx.EnterAttract)
            {
                Ctx.EnterAttract = false;
                // Route through the attract wind-up so the pull always has its own lead-in action.
                initialState = Factory.ReadyAttract();
            }
            else if (Ctx.EnterCharge)
            {
                Ctx.EnterCharge = false;
                initialState = Factory.ChargeBlast();
            }
            else
            {
                // Default equip: play the raise action, which settles into Idle once fully up.
                initialState = Factory.Equipping();
            }

            EnterSubState(initialState);
        }
        private IEnumerator BeginUnequipCheckAfterDelay()
        {
            yield return new WaitForSeconds(Ctx.EquipWaitTime);
            canCheckForUnequip = true;
            unequipDelayCoroutine = null;

        }
    }
}
