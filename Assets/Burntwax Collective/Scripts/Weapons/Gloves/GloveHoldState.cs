using UnityEngine;

namespace Burntwax
{
    // Branch A, step 2: the primary object orbits in the hand until a tap fires it
    // or a double-tap drops it. Katana parries leave an already-held object alone.
    public class GloveHoldState : GloveBaseState
    {
        private Rigidbody heldBody;
        private bool wasKinematic;
        private RigidbodyInterpolation originalInterpolation;
        private Breakable heldBreakable;
        private bool controlsHeldBodyPhysics;
        private GloveHoldProfile holdProfile;
        private Quaternion rotationOffsetFromHoldPoint;
        private bool hasRotationOffset;
        private float accumulatedHoverSpin;
        private float bobPhase;
        private float baseballForgivenessEndTime;

        public GloveHoldState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            heldBreakable = null;
            controlsHeldBodyPhysics = false;
            hasRotationOffset = false;
            accumulatedHoverSpin = 0f;
            bobPhase = 0f;
            baseballForgivenessEndTime = Time.time
                + Ctx.BaseballHoldForgivenessDuration;
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
                controlsHeldBodyPhysics = true;

                if (Ctx.restPos != null)
                {
                    rotationOffsetFromHoldPoint =
                        Quaternion.Inverse(Ctx.restPos.transform.rotation)
                        * heldBody.rotation;
                    hasRotationOffset = true;
                }

                Ctx.AttachHeldObjectToGlove();
            }

            heldBreakable = Ctx.heldObject != null
                ? Ctx.heldObject.GetComponent<Breakable>()
                : null;
            if (heldBreakable != null)
                heldBreakable.Broken += OnHeldObjectBroken;

            InitializeSubState();
        }

        public override void UpdateState()
        {
            if (!Ctx.HasUsableHeldObject)
            {
                Ctx.ClearLostHeldObject();
                SwitchState(Factory.Idle());
                return;
            }

            HoldObject();
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            StopListeningForBreak();
            Ctx.RestoreHeldObjectHierarchy();
            RestoreHeldBodyPhysics();
        }

        public override void CheckSwitchStates()
        {
            bool canUseBaseballForgiveness =
                Ctx.BaseballHoldForgivenessDuration > 0f
                && Time.time <= baseballForgivenessEndTime;

            if (canUseBaseballForgiveness
                && Ctx.KatanaPerfectParryActive)
            {
                Ctx.LaunchHeldObjectAsBaseball();
                SwitchState(Factory.Idle());
            }
            else if (InputManager.Instance.gloveTap)
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

        private void OnHeldObjectBroken(DamageInfo cause)
        {
            StopListeningForBreak();
            Ctx.RestoreHeldObjectHierarchy();
            RestoreHeldBodyPhysics();
            Ctx.ClearLostHeldObject();
        }

        private void StopListeningForBreak()
        {
            if (heldBreakable != null)
                heldBreakable.Broken -= OnHeldObjectBroken;

            heldBreakable = null;
        }

        private void RestoreHeldBodyPhysics()
        {
            if (!controlsHeldBodyPhysics)
                return;

            controlsHeldBodyPhysics = false;
            if (heldBody == null)
                return;

            heldBody.isKinematic = wasKinematic;
            heldBody.interpolation = originalInterpolation;
        }

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

            // Subtle vertical bob so the object reads as floating in a field rather than pinned.
            // Added to the target (not the object) so the hold lerp smooths it automatically.
            float bobMultiplier = holdProfile != null
                ? holdProfile.HoverBobMultiplier
                : 1f;
            float bobAmplitude = Ctx.HoldBobAmplitude * bobMultiplier;
            if (bobAmplitude > 0f && Ctx.HoldBobFrequency > 0f)
            {
                bobPhase = Mathf.Repeat(
                    bobPhase + Ctx.HoldBobFrequency * Time.deltaTime,
                    1f);
                float bobOffset = Mathf.Sin(bobPhase * Mathf.PI * 2f) * bobAmplitude;
                targetPosition += Vector3.up * bobOffset;
            }

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
