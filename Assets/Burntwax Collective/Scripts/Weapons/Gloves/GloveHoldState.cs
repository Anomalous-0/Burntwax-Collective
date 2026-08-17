using UnityEngine;

namespace Burntwax
{
    // Branch A, step 2: the primary object orbits in the hand until a tap or katana parry throws
    // it out. Ignores charge input — the two ability branches never overlap.
    public class GloveHoldState : GloveBaseState
    {
        private Rigidbody heldBody;
        private bool wasKinematic;
        private RigidbodyInterpolation originalInterpolation;
        private GloveHoldProfile holdProfile;
        private Quaternion rotationOffsetFromHoldPoint;
        private bool hasRotationOffset;
        private float accumulatedHoverSpin;

        public GloveHoldState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            hasRotationOffset = false;
            accumulatedHoverSpin = 0f;
            holdProfile = Ctx.heldObject != null
                ? Ctx.heldObject.GetComponent<GloveHoldProfile>()
                : null;
            Ctx.PlayHoldAnimation(
                holdProfile != null
                    ? holdProfile.HoldPose
                    : GloveHoldPose.Small);
            heldBody = Ctx.heldObject != null
                ? Ctx.heldObject.GetComponent<Rigidbody>()
                : null;

            if (heldBody != null)
            {
                wasKinematic = heldBody.isKinematic;
                originalInterpolation = heldBody.interpolation;
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
                heldBody.interpolation = originalInterpolation;
            }

        }

        public override void CheckSwitchStates()
        {
            if (InputManager.Instance.gloveTap)
            {
                SwitchState(Factory.BlastProjectile());
            }
            else if (InputManager.Instance.gloveDoubleTap)
            {
                SwitchState(Factory.ReleaseObject());
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
            Vector3 localPositionOffset = holdProfile != null
                ? holdProfile.LocalPositionOffset
                : Vector3.zero;
            Vector3 targetPosition = Ctx.restPos.transform.TransformPoint(
                localPositionOffset);
            Ctx.heldObject.transform.position = Vector3.Lerp(
                Ctx.heldObject.transform.position,
                targetPosition,
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
            float hoverSpinMultiplier = holdProfile != null
                ? holdProfile.HoverSpinMultiplier
                : 1f;

            if (hoverSpinMultiplier > 0f)
            {
                accumulatedHoverSpin = Mathf.Repeat(
                    accumulatedHoverSpin
                        + Ctx.HoldSpinSpeed
                        * hoverSpinMultiplier
                        * Time.deltaTime,
                    360f);
            }
            else
            {
                accumulatedHoverSpin = 0f;
            }

            Quaternion baseRotationOffset = holdProfile != null
                && holdProfile.OverrideRotation
                    ? holdProfile.LocalHeldRotation
                    : rotationOffsetFromHoldPoint;
            Quaternion hoverSpin = Quaternion.AngleAxis(
                accumulatedHoverSpin,
                Vector3.up);
            Ctx.heldObject.transform.rotation =
                Ctx.restPos.transform.rotation
                * baseRotationOffset
                * hoverSpin;
        }
    }
}
