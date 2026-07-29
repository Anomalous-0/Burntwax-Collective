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
            {
                heldBody.isKinematic = wasKinematic;
                heldBody.interpolation = RigidbodyInterpolation.Interpolate;

            }

        }

        public override void CheckSwitchStates()
        {
            // Click with the katana equipped hurls the held object hard along the camera forward;
            // a plain F tap does the normal release throw.
            if (Ctx.KatanaEquipped && InputManager.Instance.attackIsPressed)
            {
                Ctx.LaunchHeldObjectForward();
                SwitchState(Factory.Idle());
            }
            else if (InputManager.Instance.gloveTap)
            {
                SwitchState(Factory.ReleaseObject());
            }
            else if (InputManager.Instance.gloveCharging)
            {
                SwitchState(Factory.BlastProjectile());
            }
            else if (heldBody == null || Ctx.heldObject == null)
            {

                SwitchState(Factory.Idle());
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

            // Slow orbit-in-hand spin so the held object reads as actively suspended.
            Ctx.heldObject.transform.Rotate(
                Vector3.up * (Ctx.HoldSpinSpeed * Time.deltaTime), Space.Self);
        }
    }
}
