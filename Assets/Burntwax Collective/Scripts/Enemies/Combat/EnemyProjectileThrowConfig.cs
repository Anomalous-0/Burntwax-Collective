using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Authored rules for one enemy-thrown Rigidbody projectile. The combat
    /// loadout decides which enemies know this action; the throw executor only
    /// performs the launch selected by the enemy state machine.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Enemy Projectile Throw Config",
        menuName = "Enemies/Combat/Projectile Throw Config",
        order = 1)]
    public sealed class EnemyProjectileThrowConfig :
        EnemyCombatActionConfig
    {
        public override EnemyCombatActionCategory Category =>
            EnemyCombatActionCategory.Throw;

        [Header("Projectile")]
        [Tooltip("Rigidbody prefab launched by this action.")]
        [SerializeField] private GameObject projectilePrefab;

        [SerializeField, Min(0f)] private float launchSpeed = 12f;
        [SerializeField, Min(0.1f)] private float lifetime = 5f;

        [Header("Curved Flight")]
        [Tooltip("Launch toward the target using CurvedRigidbodyFlight. Disable for a direct straight-line shot.")]
        [SerializeField] private bool useCurvedFlight = true;
        [SerializeField, Min(0f)] private float arcHeightPerMeter = 0.12f;
        [SerializeField, Min(0f)] private float minimumArcHeight = 0.4f;
        [SerializeField, Min(0f)] private float maximumArcHeight = 2.5f;

        [Header("Launch Spin")]
        [Tooltip("Minimum random angular speed in radians per second. Heavier objects receive proportionally less spin.")]
        [SerializeField, Min(0f)] private float minimumLaunchSpin = 1f;

        [Tooltip("Maximum random angular speed in radians per second. Heavier objects receive proportionally less spin.")]
        [SerializeField, Min(0f)] private float maximumLaunchSpin = 4f;

        public GameObject ProjectilePrefab => projectilePrefab;
        public float LaunchSpeed => launchSpeed;
        public float Lifetime => lifetime;
        public bool UseCurvedFlight => useCurvedFlight;
        public float ArcHeightPerMeter => arcHeightPerMeter;
        public float MinimumArcHeight => minimumArcHeight;
        public float MaximumArcHeight => maximumArcHeight;
        public float MinimumLaunchSpin => minimumLaunchSpin;
        public float MaximumLaunchSpin => maximumLaunchSpin;

        protected override void OnValidate()
        {
            base.OnValidate();
            launchSpeed = Mathf.Max(0f, launchSpeed);
            lifetime = Mathf.Max(0.1f, lifetime);
            arcHeightPerMeter = Mathf.Max(0f, arcHeightPerMeter);
            minimumArcHeight = Mathf.Max(0f, minimumArcHeight);
            maximumArcHeight = Mathf.Max(
                minimumArcHeight,
                maximumArcHeight);
            minimumLaunchSpin = Mathf.Max(0f, minimumLaunchSpin);
            maximumLaunchSpin = Mathf.Max(
                minimumLaunchSpin,
                maximumLaunchSpin);
        }
    }
}
