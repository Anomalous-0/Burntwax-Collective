using UnityEngine;

namespace Burntwax
{
    // Branch A, step 2: the primary object orbits in the hand until a tap (or katana slash) throws
    // it out. Ignores charge input — the two ability branches never overlap.
    public class GloveHoldState : GloveBaseState
    {
        private Rigidbody heldBody;
        private bool wasKinematic;

        public GloveHoldState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            Ctx.PlayGloveAnimation(Ctx.holdHash);
            heldBody = Ctx.heldObject != null
                ? Ctx.heldObject.GetComponent<Rigidbody>()
                : null;

            if (heldBody != null)
            {
                wasKinematic = heldBody.isKinematic;
                heldBody.linearVelocity = Vector3.zero;
                heldBody.angularVelocity = Vector3.zero;
                heldBody.isKinematic = true;
                heldBody.interpolation = RigidbodyInterpolation.None;
            }

            InitializeSubState();
        }

        public override void UpdateState()
        {
            HoldObject();
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            if (heldBody != null)
                heldBody.isKinematic = wasKinematic;
        }

        public override void CheckSwitchStates()
        {
            if (InputManager.Instance.gloveTap)
            {
                SwitchState(Factory.ReleaseObject());
            }
            else if(InputManager.Instance.gloveCharging)
            {
                SwitchState(Factory.BlastProjectile());
            }
            
        }

        public override void InitializeSubState() { }

        private void HoldObject()
        {
            if (Ctx.heldObject == null || Ctx.restPos == null)
                return;

            float lerpAmount = Mathf.Clamp01(Ctx.HoldLerpSpeed * Time.deltaTime);
            Ctx.heldObject.transform.position = Vector3.Lerp(
                Ctx.heldObject.transform.position,
                Ctx.restPos.transform.position,
                lerpAmount);
        }
    }
}
