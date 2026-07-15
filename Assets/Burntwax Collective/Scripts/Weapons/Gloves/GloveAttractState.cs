using UnityEngine;

namespace Burntwax
{
    // Branch A, step 1: stop-sign pose, pulling object(s) toward the hand. The primary latches into
    // Hold; a tap or (with katana equipped) a slash launches the incoming object early.
    public class GloveAttractState : GloveBaseState
    {
        private Rigidbody attractedBody;
        private Vector3 pathStart;
        private Vector3 arcDirection;
        private float arcHeight;
        private float pathDistance;
        private float pathProgress;
        private float currentTargetVelocity;
        private float velocityDistanceScale;
        private bool originalUseGravity;
        private bool gravityOverrideActive;
        private RigidbodyInterpolation originalInterpolation;

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
                originalUseGravity = attractedBody.useGravity;
                originalInterpolation = attractedBody.interpolation;
                attractedBody.useGravity = false;
                attractedBody.interpolation = RigidbodyInterpolation.Interpolate;
                gravityOverrideActive = true;
                InitializePath();
                ApplyInitialVelocity();
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

        public override void ExitState()
        {
            if (attractedBody != null && gravityOverrideActive)
            {
                attractedBody.useGravity = originalUseGravity;
                attractedBody.interpolation = originalInterpolation;
            }
        }



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

            float cruiseVelocity = Mathf.Min(
                Ctx.AttractionCruiseVelocity,
                Ctx.InitialAttractionVelocity) * velocityDistanceScale;
            currentTargetVelocity = Mathf.MoveTowards(
                currentTargetVelocity,
                cruiseVelocity,
                Ctx.AttractionSlowdownRate * velocityDistanceScale * Time.fixedDeltaTime);
            pathProgress = Mathf.MoveTowards(
                pathProgress,
                1f,
                currentTargetVelocity * Time.fixedDeltaTime / pathDistance);

            Vector3 pathEnd = Ctx.restPos.transform.position;
            Vector3 pathTarget = EvaluateArcPath(
                pathStart,
                pathEnd,
                arcDirection,
                arcHeight,
                Ctx.ArcPeakPosition,
                pathProgress);

            Vector3 toPathTarget = pathTarget - attractedBody.position;
            Vector3 desiredVelocity = toPathTarget.sqrMagnitude > 0f
                ? toPathTarget.normalized * currentTargetVelocity
                : Vector3.zero;
            Vector3 acceleration =
                (desiredVelocity - attractedBody.linearVelocity) / Time.fixedDeltaTime;
            acceleration = Vector3.ClampMagnitude(acceleration, Ctx.AttractionForce);

            attractedBody.AddForce(
                acceleration,
                ForceMode.Acceleration);
        }

        private void InitializePath()
        {
            pathStart = attractedBody.position;
            Vector3 toRestPosition = Ctx.restPos.transform.position - pathStart;
            pathDistance = Mathf.Max(toRestPosition.magnitude, 0.01f);
            pathProgress = 0f;
            velocityDistanceScale = Mathf.Clamp01(
                pathDistance / Ctx.FullAttractionVelocityDistance);

            Vector3 pullDirection = toRestPosition / pathDistance;
            arcDirection = Vector3.ProjectOnPlane(Vector3.up, pullDirection).normalized;

            arcHeight = Mathf.Clamp(
                pathDistance * Ctx.ArcHeightPerMeter,
                Ctx.MinimumArcHeight,
                Mathf.Max(Ctx.MinimumArcHeight, Ctx.MaximumArcHeight));
        }

        private void ApplyInitialVelocity()
        {
            currentTargetVelocity = Mathf.Max(
                Ctx.InitialAttractionVelocity,
                Ctx.AttractionCruiseVelocity) * velocityDistanceScale;

            float launchSampleProgress = Mathf.Clamp(
                currentTargetVelocity * Time.fixedDeltaTime / pathDistance,
                0.01f,
                0.1f);
            pathProgress = launchSampleProgress;
            Vector3 launchTarget = EvaluateArcPath(
                pathStart,
                Ctx.restPos.transform.position,
                arcDirection,
                arcHeight,
                Ctx.ArcPeakPosition,
                launchSampleProgress);
            Vector3 launchDirection = (launchTarget - pathStart).normalized;

            attractedBody.linearVelocity = Vector3.zero;
            attractedBody.angularVelocity = Vector3.zero;
            attractedBody.AddForce(
                launchDirection * currentTargetVelocity,
                ForceMode.VelocityChange);
        }

        private static Vector3 EvaluateArcPath(
            Vector3 start,
            Vector3 end,
            Vector3 arcDirection,
            float height,
            float peakPosition,
            float progress)
        {
            peakPosition = Mathf.Clamp(peakPosition, 0.05f, 0.95f);
            float arcWeight;

            if (progress <= peakPosition)
            {
                float riseProgress = progress / peakPosition;
                arcWeight = Mathf.Sin(riseProgress * Mathf.PI * 0.5f);
            }
            else
            {
                float fallProgress = (progress - peakPosition) / (1f - peakPosition);
                arcWeight = Mathf.Cos(fallProgress * Mathf.PI * 0.5f);
            }

            return Vector3.Lerp(start, end, progress) + arcDirection * height * arcWeight;
        }
    }
}
