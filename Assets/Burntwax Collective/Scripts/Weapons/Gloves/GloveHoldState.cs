using UnityEngine;

namespace Burntwax
{
    // Branch A, step 2: the primary object orbits in the hand until a tap or katana parry throws
    // it out. Ignores charge input — the two ability branches never overlap.
    public class GloveHoldState : GloveBaseState
    {
        private Rigidbody heldBody;
        private bool wasKinematic;
        private Quaternion rotationOffsetFromHoldPoint;
        private bool hasRotationOffset;

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

                if (Ctx.restPos != null)
                {
                    rotationOffsetFromHoldPoint =
                        Quaternion.Inverse(Ctx.restPos.transform.rotation)
                        * heldBody.rotation;
                    hasRotationOffset = true;
                }
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
            // A katana parry hurls the held object along the camera forward. Glove gestures keep
            // their existing release and blast behaviors.
            // if (Ctx.KatanaParrying)
            // {
            //     Ctx.LaunchHeldObjectForward();
            //     SwitchState(Factory.Idle());
            // }
            if (InputManager.Instance.gloveTap)
            {
                Ctx.LaunchHeldObjectForward();
                SwitchState(Factory.Idle());
            }
            else if (InputManager.Instance.gloveDoubleTap)
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

            if (!hasRotationOffset)
            {
                rotationOffsetFromHoldPoint =
                    Quaternion.Inverse(Ctx.restPos.transform.rotation)
                    * Ctx.heldObject.transform.rotation;
                hasRotationOffset = true;
            }

            // Keep the grabbed orientation relative to the player while preserving
            // the slow local hover spin that makes the object read as suspended.
            rotationOffsetFromHoldPoint *= Quaternion.AngleAxis(
                Ctx.HoldSpinSpeed * Time.deltaTime,
                Vector3.up);
            Ctx.heldObject.transform.rotation =
                Ctx.restPos.transform.rotation * rotationOffsetFromHoldPoint;
        }
    }
}
