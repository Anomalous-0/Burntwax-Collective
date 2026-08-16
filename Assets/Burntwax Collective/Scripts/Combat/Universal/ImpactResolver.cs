using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Applies the physical part of a hit independently from health damage.
    /// A custom receiver takes ownership when present; otherwise an attached
    /// non-kinematic Rigidbody receives the impulse automatically.
    /// </summary>
    public static class ImpactResolver
    {
        public static bool TryApplyImpact(
            Collider hitCollider,
            DamageInfo impact)
        {
            if (hitCollider == null
                || impact.Impulse.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            IImpactReceiver customReceiver =
                hitCollider.GetComponentInParent<IImpactReceiver>();
            if (customReceiver != null)
            {
                customReceiver.ReceiveImpact(impact);
                return true;
            }

            Rigidbody body = hitCollider.attachedRigidbody;
            if (body == null || body.isKinematic)
                return false;

            body.AddForceAtPosition(
                impact.Impulse,
                impact.Point,
                ForceMode.Impulse);
            return true;
        }
    }
}
