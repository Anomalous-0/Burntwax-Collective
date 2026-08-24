using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Designer-authored fall-damage response. Landing speed is measured from
    /// the player's downward pre-collision velocity, independently of generic
    /// Rigidbody impact damage.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Player Fall Damage Config",
        menuName = "Combat/Player Fall Damage Config")]
    public sealed class PlayerFallDamageConfig : ScriptableObject
    {
        [Header("Landing speed")]
        [Tooltip("Downward landing speed at or below which the player takes no fall damage.")]
        [SerializeField, Min(0f)] private float safeLandingSpeed = 12f;

        [Tooltip("Downward landing speed at which Maximum Damage is reached.")]
        [SerializeField, Min(0f)] private float fullDamageSpeed = 32f;

        [Tooltip("Controls how damage grows from Safe Landing Speed (0) to Full Damage Speed (1).")]
        [SerializeField] private AnimationCurve damageBySpeed =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Damage")]
        [Tooltip("Fall damage dealt at or above Full Damage Speed.")]
        [SerializeField, Min(0f)] private float maximumDamage = 100f;

        [Header("Valid landing surfaces")]
        [Tooltip("Minimum upward alignment required for a contact to count as ground. Higher values reject steeper slopes.")]
        [SerializeField, Range(0f, 1f)] private float minimumGroundUpDot = 0.6f;

        public float SafeLandingSpeed => safeLandingSpeed;
        public float FullDamageSpeed => fullDamageSpeed;
        public float MaximumDamage => maximumDamage;
        public float MinimumGroundUpDot => minimumGroundUpDot;

        public float CalculateDamage(float downwardSpeed)
        {
            if (downwardSpeed <= safeLandingSpeed || maximumDamage <= 0f)
                return 0f;

            if (fullDamageSpeed <= safeLandingSpeed)
                return maximumDamage;

            float normalizedSpeed = Mathf.InverseLerp(
                safeLandingSpeed,
                fullDamageSpeed,
                downwardSpeed);

            float damageScale = damageBySpeed == null
                || damageBySpeed.length == 0
                ? normalizedSpeed
                : damageBySpeed.Evaluate(normalizedSpeed);

            return maximumDamage * Mathf.Clamp01(damageScale);
        }

        private void OnValidate()
        {
            safeLandingSpeed = Mathf.Max(0f, safeLandingSpeed);
            fullDamageSpeed = Mathf.Max(safeLandingSpeed, fullDamageSpeed);
            maximumDamage = Mathf.Max(0f, maximumDamage);
            minimumGroundUpDot = Mathf.Clamp01(minimumGroundUpDot);

            if (damageBySpeed == null || damageBySpeed.length == 0)
                damageBySpeed = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }
}
