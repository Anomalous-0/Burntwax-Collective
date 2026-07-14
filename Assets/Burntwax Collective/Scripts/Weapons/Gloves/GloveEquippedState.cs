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
                SwitchState(Factory.Unequipped());
            }
        }

        public override void InitializeSubState()
        {
            GloveBaseState initialState;

            if (Ctx.EnterAttract)
            {
                Ctx.EnterAttract = false;
                initialState = Factory.Attract();
            }
            else if (Ctx.EnterCharge)
            {
                Ctx.EnterCharge = false;
                initialState = Factory.ChargeBlast();
            }
            else
            {
                initialState = Factory.Idle();
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
