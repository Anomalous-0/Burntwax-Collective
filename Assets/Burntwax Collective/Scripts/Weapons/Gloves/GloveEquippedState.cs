using System.Collections;
using UnityEngine;

namespace Burntwax
{
    public class GloveEquippedState : GloveBaseState
    {
        private bool canChooseSubstate;
        private bool canCheckForUnequip;
        private Coroutine substateDelayCoroutine;
        private Coroutine unequipDelayCoroutine;

        public GloveEquippedState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            canChooseSubstate = false;
            canCheckForUnequip = false;
            substateDelayCoroutine = Ctx.StartCoroutine(BeginSubstateSelectionAfterDelay());
            unequipDelayCoroutine = Ctx.StartCoroutine(BeginUnequipCheckAfterDelay());

        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            canChooseSubstate = false;
            canCheckForUnequip = false;

            if (substateDelayCoroutine != null)
            {
                Ctx.StopCoroutine(substateDelayCoroutine);
                substateDelayCoroutine = null;
            }

            if (unequipDelayCoroutine != null)
            {
                Ctx.StopCoroutine(unequipDelayCoroutine);
                unequipDelayCoroutine = null;
            }
        }

        public override void CheckSwitchStates()
        {
            if (CurrentSubstate() != Ctx.states.Hold() && !InputManager.Instance.useGloves && canCheckForUnequip)
            {
                SwitchState(Factory.Unequipped());
            }
        }

        public override void InitializeSubState()
        {
            if (canChooseSubstate)
            {
                if (Ctx.CanAttractObject && InputManager.Instance.useGloves)
                {
                    SetSubState(Factory.ChargeAttract());
                }
                else if (InputManager.Instance.useGloves)
                {
                    SetSubState(Factory.ChargeBlast());
                }
                else
                {
                    SetSubState(Factory.Idle());
                }
            }


        }

        private IEnumerator BeginSubstateSelectionAfterDelay()
        {
            yield return new WaitForSeconds(Ctx.SubstateWaitTime);
            canChooseSubstate = true;
            InitializeSubState();
            substateDelayCoroutine = null;
        }

        private IEnumerator BeginUnequipCheckAfterDelay()
        {
            yield return new WaitForSeconds(Ctx.EquipWaitTime);
            canCheckForUnequip = true;
            unequipDelayCoroutine = null;
        }
    }
}
