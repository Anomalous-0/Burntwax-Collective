using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Shared random angular-velocity calculation for launched Rigidbodies.
    /// Objects heavier than mass one receive proportionally less spin.
    /// </summary>
    public static class RigidbodySpinUtility
    {
        public static Vector3 GetMassWeightedRandomSpin(
            Rigidbody body,
            float minimumSpin,
            float maximumSpin)
        {
            minimumSpin = Mathf.Max(0f, minimumSpin);
            maximumSpin = Mathf.Max(minimumSpin, maximumSpin);
            if (body == null || maximumSpin <= Mathf.Epsilon)
                return Vector3.zero;

            float massMultiplier = 1f / Mathf.Max(1f, body.mass);
            return Random.onUnitSphere
                * Random.Range(minimumSpin, maximumSpin)
                * massMultiplier;
        }

        public static void ApplyMassWeightedRandomSpin(
            Rigidbody body,
            float minimumSpin,
            float maximumSpin)
        {
            if (body == null)
                return;

            body.angularVelocity = GetMassWeightedRandomSpin(
                body,
                minimumSpin,
                maximumSpin);
        }
    }
}
