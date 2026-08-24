using UnityEngine;

namespace Burntwax
{
    // Branch A, step 1: stop-sign pose, pulling object(s) toward the hand. The primary latches into
    // Hold unless a correctly timed katana parry bats it while it is close enough.
    public class GloveAttractState : GloveBaseState
    {
        private Rigidbody attractedBody;
        private Vector3 pathStart;
        private Vector3 arcDirection;
        private float arcHeight;
        private float pathDistance;
        private float pathProgress;
        private float currentTargetVelocity;
        private bool originalUseGravity;
        private bool gravityOverrideActive;
        private RigidbodyInterpolation originalInterpolation;

        public GloveAttractState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            gravityOverrideActive = false;
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
            // Everything else in reach gets a gentler tug in behind the primary.
            if (Ctx.restPos != null)
                Ctx.PullSecondaryObjects(Ctx.heldObject, Ctx.restPos.transform.position);
        }

        public override void ExitState()
        {
            if (attractedBody != null && gravityOverrideActive)
            {
                attractedBody.useGravity = originalUseGravity;
                attractedBody.interpolation = originalInterpolation;
            }

            // Whichever way the pull ended, let the objects that trailed in behind the primary
            // settle instead of coasting into the player.
            Ctx.SettleSecondaryObjects();
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

            float distanceToRest = Vector3.Distance(
                attractedBody.position,
                Ctx.restPos.transform.position);

            // Baseball interaction: entering the stance is not enough. The
            // animation's perfect-parry window must overlap the object's arrival.
            if (Ctx.KatanaPerfectParryActive
                && distanceToRest <= Ctx.BaseballBatDistance)
            {
                Ctx.LaunchHeldObjectForward();
                SwitchState(Factory.Idle());
                return;
            }

            if (distanceToRest <= Ctx.RestDistance)
            {
                attractedBody.linearVelocity = Vector3.zero;
                attractedBody.angularVelocity = Vector3.zero;
                // Camera kick the moment the object actually reaches the glove, scaled by its mass.
                Ctx.ShakeGlove(attractedBody.mass);
                Ctx.PlayObjectCatchFovKick();
                SwitchState(Factory.Hold());
            }
        }

        public override void InitializeSubState() { }

        private void AttractObject()
        {
            if (attractedBody == null || Ctx.restPos == null)
                return;

            float maximumCruiseVelocity = Mathf.Min(
                Ctx.AttractionCruiseVelocity,
                Ctx.InitialAttractionVelocity);
            float minimumCruiseVelocity = Mathf.Min(
                Ctx.MinimumAttractionVelocity,
                maximumCruiseVelocity);
            float currentDistance = Vector3.Distance(
                attractedBody.position,
                Ctx.restPos.transform.position);
            float distanceScale = GetDistanceVelocityScale(currentDistance);
            float cruiseVelocity = Mathf.Lerp(
                minimumCruiseVelocity,
                maximumCruiseVelocity,
                distanceScale);
            currentTargetVelocity = Mathf.MoveTowards(
                currentTargetVelocity,
                cruiseVelocity,
                Ctx.AttractionSlowdownRate * Time.fixedDeltaTime);
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

            Vector3 pullDirection = toRestPosition / pathDistance;
            arcDirection = Vector3.ProjectOnPlane(Vector3.up, pullDirection).normalized;

            arcHeight = Mathf.Clamp(
                pathDistance * Ctx.ArcHeightPerMeter,
                Ctx.MinimumArcHeight,
                Mathf.Max(Ctx.MinimumArcHeight, Ctx.MaximumArcHeight));
        }

        private void ApplyInitialVelocity()
        {
            float maximumInitialVelocity = Mathf.Max(
                Ctx.InitialAttractionVelocity,
                Ctx.AttractionCruiseVelocity);
            float minimumInitialVelocity = Mathf.Min(
                Ctx.MinimumAttractionVelocity,
                maximumInitialVelocity);
            currentTargetVelocity = Mathf.Lerp(
                minimumInitialVelocity,
                maximumInitialVelocity,
                GetDistanceVelocityScale(pathDistance));

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
            attractedBody.angularVelocity = GetRandomAttractionSpin();
            attractedBody.AddForce(
                launchDirection * currentTargetVelocity,
                ForceMode.VelocityChange);
        }

        private float GetDistanceVelocityScale(float distance)
        {
            return Mathf.Clamp01(
                distance / Mathf.Max(
                    Ctx.FullAttractionVelocityDistance,
                    0.01f));
        }

        private Vector3 GetRandomAttractionSpin()
        {
            float minimumSpin = Mathf.Max(0f, Ctx.MinimumAttractionSpin);
            float maximumSpin = Mathf.Max(
                minimumSpin,
                Ctx.MaximumAttractionSpin);

            if (maximumSpin <= Mathf.Epsilon)
                return Vector3.zero;

            float massMultiplier = 1f / Mathf.Max(1f, attractedBody.mass);

            return Random.onUnitSphere
                * Random.Range(minimumSpin, maximumSpin)
                * massMultiplier;
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
