using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Reusable constant-speed Rigidbody guidance along a quadratic curve to a
    /// captured world position. The target is intentionally not followed, so
    /// actors can dodge after the flight begins.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CurvedRigidbodyFlight : MonoBehaviour
    {
        private const int ArcLengthSamples = 12;

        private Rigidbody body;
        private Vector3 start;
        private Vector3 control;
        private Vector3 end;
        private float speed;
        private float approximateArcLength;
        private float progress;
        private float ignoreCollisionsAtFixedTime = -1f;
        private bool enableGravityAfterFlight;
        private bool flightActive;

        public static bool Launch(
            Rigidbody body,
            Vector3 capturedTargetPosition,
            float speed,
            float arcHeight,
            bool ignoreCurrentCollision = false,
            bool enableGravityAfterFlight = true)
        {
            if (body == null || body.isKinematic || speed <= Mathf.Epsilon)
                return false;

            CurvedRigidbodyFlight flight =
                body.GetComponent<CurvedRigidbodyFlight>();
            if (flight == null)
            {
                flight = body.gameObject.AddComponent<
                    CurvedRigidbodyFlight>();
            }

            flight.BeginFlight(
                body,
                capturedTargetPosition,
                speed,
                arcHeight,
                ignoreCurrentCollision,
                enableGravityAfterFlight);
            return true;
        }

        private void BeginFlight(
            Rigidbody flightBody,
            Vector3 capturedTargetPosition,
            float flightSpeed,
            float arcHeight,
            bool ignoreCurrentCollision,
            bool shouldEnableGravityAfterFlight)
        {
            if (flightActive)
                RestorePhysicsSettings();

            body = flightBody;
            start = body.worldCenterOfMass;
            end = capturedTargetPosition;
            speed = Mathf.Max(0f, flightSpeed);
            progress = 0f;

            Vector3 path = end - start;
            Vector3 pathDirection = path.sqrMagnitude > Mathf.Epsilon
                ? path.normalized
                : transform.forward;
            Vector3 arcDirection = Vector3.ProjectOnPlane(
                Vector3.up,
                pathDirection);
            if (arcDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                arcDirection = Vector3.Cross(pathDirection, Vector3.right);
                if (arcDirection.sqrMagnitude <= Mathf.Epsilon)
                    arcDirection = Vector3.Cross(pathDirection, Vector3.forward);
            }

            arcDirection.Normalize();
            // Multiplying by two makes the midpoint of a quadratic curve reach
            // the requested height above the straight start/end midpoint.
            control = Vector3.Lerp(start, end, 0.5f)
                + arcDirection * Mathf.Max(0f, arcHeight) * 2f;
            approximateArcLength = CalculateApproximateArcLength();

            enableGravityAfterFlight = shouldEnableGravityAfterFlight;
            body.useGravity = false;
            flightActive = true;
            enabled = true;
            ignoreCollisionsAtFixedTime = ignoreCurrentCollision
                ? Time.fixedTime
                : -1f;

            SetVelocityTowardProgress(
                Mathf.Min(
                    1f,
                    speed * Time.fixedDeltaTime / approximateArcLength));
        }

        private void FixedUpdate()
        {
            if (!flightActive || body == null)
                return;

            // Launching can occur before an attraction state restores its own
            // temporary gravity override. Guidance owns gravity until flight ends.
            body.useGravity = false;

            float progressStep = speed
                * Time.fixedDeltaTime
                / approximateArcLength;
            float nextProgress = Mathf.Min(1f, progress + progressStep);
            SetVelocityTowardProgress(nextProgress);
            progress = nextProgress;

            if (progress >= 1f)
                StopFlight();
        }

        private void SetVelocityTowardProgress(float targetProgress)
        {
            Vector3 target = EvaluateCurve(targetProgress);
            Vector3 direction = target - body.worldCenterOfMass;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = EvaluateCurveTangent(targetProgress);
            }

            if (direction.sqrMagnitude > Mathf.Epsilon)
                body.linearVelocity = direction.normalized * speed;
        }

        private Vector3 EvaluateCurve(float value)
        {
            value = Mathf.Clamp01(value);
            float inverse = 1f - value;
            return inverse * inverse * start
                + 2f * inverse * value * control
                + value * value * end;
        }

        private Vector3 EvaluateCurveTangent(float value)
        {
            value = Mathf.Clamp01(value);
            return 2f * (1f - value) * (control - start)
                + 2f * value * (end - control);
        }

        private float CalculateApproximateArcLength()
        {
            float length = 0f;
            Vector3 previous = start;

            for (int i = 1; i <= ArcLengthSamples; i++)
            {
                Vector3 current = EvaluateCurve(
                    i / (float)ArcLengthSamples);
                length += Vector3.Distance(previous, current);
                previous = current;
            }

            return Mathf.Max(length, 0.01f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!flightActive)
                return;

            if (ignoreCollisionsAtFixedTime >= 0f
                && Mathf.Approximately(
                    Time.fixedTime,
                    ignoreCollisionsAtFixedTime))
            {
                return;
            }

            StopFlight();
        }

        private void StopFlight()
        {
            if (!flightActive)
                return;

            RestorePhysicsSettings();
            flightActive = false;
            enabled = false;
        }

        /// <summary>
        /// Releases this Rigidbody from an active guided path immediately.
        /// Used when another system, such as glove attraction, takes control.
        /// </summary>
        public void CancelFlight()
        {
            StopFlight();
        }

        private void RestorePhysicsSettings()
        {
            if (body != null)
                body.useGravity = enableGravityAfterFlight;
        }

        private void OnDisable()
        {
            if (!flightActive)
                return;

            RestorePhysicsSettings();
            flightActive = false;
        }
    }
}
