using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Shared profile for an opted-in Rigidbody. The source's closing speed
    /// into the contact scales authored target and self-damage between the
    /// minimum and full-damage speeds.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Velocity Impact Damage Config",
        menuName = "Combat/Velocity Impact Damage Config")]
    public sealed class VelocityImpactDamageConfig : ScriptableObject
    {
        [Header("Impact gate")]
        [InspectorName("Minimum Damage Speed")]
        [Tooltip("Source Rigidbody speed into the contact below which the collision remains harmless.")]
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 6f;

        [Tooltip("Source closing speed at which the configured target and self-damage reach their full values.")]
        [SerializeField, Min(0f)] private float fullDamageSpeed = 25f;

        [Tooltip("Controls how damage grows from the minimum speed (0) to the full-damage speed (1).")]
        [SerializeField] private AnimationCurve damageBySpeed =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Authored damage")]
        [InspectorName("Damage At Full Speed")]
        [Tooltip("Damage dealt to the struck target at or above Full Damage Speed.")]
        [SerializeField, Min(0f)] private float damage = 20f;

        [InspectorName("Self-Damage At Full Speed")]
        [Tooltip("Impact damage this object receives at or above Full Damage Speed. Set to 0 to disable self-damage.")]
        [SerializeField, Min(0f)] private float damageTakenOnImpact = 20f;

        [Header("Armed-only behavior")]
        [Tooltip("Seconds before a thrown object becomes harmless. Set to 0 to remain armed until its impacts are spent.")]
        [SerializeField, Min(0f)] private float armedLifetime = 5f;

        [Tooltip("Number of above-threshold collisions that may produce damage/effects before the object disarms.")]
        [SerializeField, Min(1)] private int maximumImpacts = 1;

        public float MinimumImpactSpeed => minimumImpactSpeed;
        public float FullDamageSpeed => fullDamageSpeed;
        public float DamageAtFullSpeed => damage;
        public float SelfDamageAtFullSpeed => damageTakenOnImpact;
        public float ArmedLifetime => armedLifetime;
        public int MaximumImpacts => maximumImpacts;

        public bool MeetsDamageThreshold(float impactSpeed)
        {
            return impactSpeed >= minimumImpactSpeed;
        }

        public float EvaluateDamageScale(float impactSpeed)
        {
            if (!MeetsDamageThreshold(impactSpeed))
                return 0f;

            if (fullDamageSpeed <= minimumImpactSpeed)
                return 1f;

            float normalizedSpeed = Mathf.InverseLerp(
                minimumImpactSpeed,
                fullDamageSpeed,
                impactSpeed);

            if (damageBySpeed == null || damageBySpeed.length == 0)
                return normalizedSpeed;

            return Mathf.Clamp01(damageBySpeed.Evaluate(normalizedSpeed));
        }

        private void OnValidate()
        {
            minimumImpactSpeed = Mathf.Max(0f, minimumImpactSpeed);
            fullDamageSpeed = Mathf.Max(minimumImpactSpeed, fullDamageSpeed);

            if (damageBySpeed == null || damageBySpeed.length == 0)
                damageBySpeed = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            damage = Mathf.Max(0f, damage);
            damageTakenOnImpact = Mathf.Max(0f, damageTakenOnImpact);
            armedLifetime = Mathf.Max(0f, armedLifetime);
            maximumImpacts = Mathf.Max(1, maximumImpacts);
        }
    }
}
