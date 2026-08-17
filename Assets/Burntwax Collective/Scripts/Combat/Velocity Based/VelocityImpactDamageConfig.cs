using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Shared profile for an intentionally thrown Rigidbody. Speed only decides whether
    /// the collision qualifies; the object's authored damage is fixed.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Velocity Impact Damage Config",
        menuName = "Combat/Velocity Impact Damage Config")]
    public sealed class VelocityImpactDamageConfig : ScriptableObject
    {
        [Header("Impact gate")]
        [Tooltip("Closing speed below which an armed collision remains harmless.")]
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 6f;

        [Header("Authored damage")]
        [Tooltip("Fixed base damage dealt whenever an armed impact passes the speed gate.")]
        [SerializeField, Min(0f)] private float damage = 20f;

        [Header("Armed behavior")]
        [Tooltip("Seconds before a thrown object becomes harmless. Set to 0 to remain armed until its impacts are spent.")]
        [SerializeField, Min(0f)] private float armedLifetime = 5f;

        [Tooltip("Number of above-threshold collisions that may produce damage/effects before the object disarms.")]
        [SerializeField, Min(1)] private int maximumImpacts = 1;

        public float MinimumImpactSpeed => minimumImpactSpeed;
        public float Damage => damage;
        public float ArmedLifetime => armedLifetime;
        public int MaximumImpacts => maximumImpacts;

        public bool MeetsDamageThreshold(float impactSpeed)
        {
            return impactSpeed >= minimumImpactSpeed;
        }

        private void OnValidate()
        {
            minimumImpactSpeed = Mathf.Max(0f, minimumImpactSpeed);
            damage = Mathf.Max(0f, damage);
            armedLifetime = Mathf.Max(0f, armedLifetime);
            maximumImpacts = Mathf.Max(1, maximumImpacts);
        }
    }
}
