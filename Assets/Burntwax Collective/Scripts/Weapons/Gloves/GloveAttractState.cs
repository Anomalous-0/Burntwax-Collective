using UnityEngine;

namespace Burntwax
{
    // Branch A, step 1: stop-sign pose, pulling object(s) toward the hand. The primary latches into
    // Hold; a tap or (with katana equipped) a slash launches the incoming object early.
    public class GloveAttractState : GloveBaseState
    {
        private Rigidbody attractedBody;
        private float distanceTravelled;
        private Vector3 previousPosition;
        private Vector3 previousDirectionToRest;
        private Vector3 curveDirection;

        public GloveAttractState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            Ctx.PlayGloveAnimation(Ctx.attractHash);
            Ctx.heldObject = Ctx.detectedObject;
            Ctx.IgnoreHeldObjectPlayerCollisions();
            attractedBody = Ctx.heldObject != null
                ? Ctx.heldObject.GetComponent<Rigidbody>()
                : null;

            if (attractedBody != null && Ctx.restPos != null)
            {
                distanceTravelled = 0f;
                previousPosition = attractedBody.position;
                previousDirectionToRest =
                    (Ctx.restPos.transform.position - attractedBody.position).normalized;

                Camera playerCamera = Camera.main;
                curveDirection = playerCamera != null ? playerCamera.transform.up : Vector3.up;
            }

        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void FixedUpdateState()
        {
            AttractObject();
        }

        public override void ExitState() { }



        public override void CheckSwitchStates()
        {
            if (attractedBody == null || Ctx.restPos == null)
            {
                Ctx.RestoreHeldObjectPlayerCollisions();
                Ctx.heldObject = null;
                SwitchState(Factory.Idle());
                return;
            }

            if (Vector3.Distance(attractedBody.position, Ctx.restPos.transform.position) <=
                Ctx.RestDistance)
            {
                attractedBody.linearVelocity = Vector3.zero;
                attractedBody.angularVelocity = Vector3.zero;
                SwitchState(Factory.Hold());
            }
        }

        public override void InitializeSubState() { }

        private void AttractObject()
        {
            if (attractedBody == null || Ctx.restPos == null)
                return;

            Vector3 toRestPosition = Ctx.restPos.transform.position - attractedBody.position;
            float distance = toRestPosition.magnitude;
            if (distance <= 0f)
                return;

            // Only count movement made by the object toward the previous hand position. The live
            // remaining distance stays in the denominator, so moving the player closer advances
            // the profile and moving away rolls it back without introducing a speed discontinuity.
            Vector3 movementSinceLastStep = attractedBody.position - previousPosition;
            distanceTravelled += Mathf.Max(
                0f,
                Vector3.Dot(movementSinceLastStep, previousDirectionToRest));

            float livePathLength = distanceTravelled + distance;
            float travelProgress = livePathLength > 0f
                ? Mathf.Clamp01(distanceTravelled / livePathLength)
                : 1f;

            float curveOffset = Mathf.Sin(travelProgress * Mathf.PI) * Ctx.AttractionCurveHeight;
            Vector3 curvedTarget = Ctx.restPos.transform.position + curveDirection * curveOffset;
            Vector3 displacement = curvedTarget - attractedBody.position;

            float velocityCurveValue = Ctx.AttractionVelocityCurve != null
                ? Mathf.Clamp01(Ctx.AttractionVelocityCurve.Evaluate(travelProgress))
                : 1f;
            float profileSpeed = Mathf.Lerp(
                Ctx.MinimumAttractionSpeed,
                Ctx.MaxAttractionSpeed,
                velocityCurveValue);

            // The distance-proportional limit is the spring portion; damping smoothly tracks the
            // requested profile velocity instead of snapping the Rigidbody to it.
            float springSpeedLimit = distance * Ctx.AttractionSpringStrength;
            float desiredSpeed = Mathf.Min(profileSpeed, springSpeedLimit);
            Vector3 desiredVelocity = displacement.sqrMagnitude > 0f
                ? displacement.normalized * desiredSpeed
                : Vector3.zero;
            Vector3 acceleration =
                (desiredVelocity - attractedBody.linearVelocity) * Ctx.AttractionDamping;
            acceleration = Vector3.ClampMagnitude(acceleration, Ctx.MaxAttractionAcceleration);

            attractedBody.AddForce(acceleration, ForceMode.Acceleration);

            previousPosition = attractedBody.position;
            previousDirectionToRest = toRestPosition / distance;
        }
    }
}
