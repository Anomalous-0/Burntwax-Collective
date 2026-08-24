using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Converts valid player landings reported by RigidbodyImpactReporter into
    /// fall damage. It never damages the ground or other collision target.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(RigidbodyImpactReporter))]
    public sealed class PlayerFallDamage : MonoBehaviour
    {
        [SerializeField] private PlayerFallDamageConfig config;

        [Tooltip("Prevents one landing from damaging the player more than once through rapid contacts.")]
        [SerializeField, Min(0f)] private float landingCooldown = 0.2f;

        private RigidbodyImpactReporter impactReporter;
        private Collider playerCollider;
        private float nextAllowedDamageTime;

        private void Awake()
        {
            impactReporter = GetComponent<RigidbodyImpactReporter>();
            playerCollider = FindOwnedCollider();
        }

        private void OnEnable()
        {
            if (impactReporter == null)
                impactReporter = GetComponent<RigidbodyImpactReporter>();

            if (impactReporter != null)
                impactReporter.ImpactReported += OnImpactReported;

            if (config == null)
            {
                Debug.LogWarning(
                    $"[{nameof(PlayerFallDamage)}] '{name}' has no fall-damage config.",
                    this);
            }

            if (GetComponent<VelocityImpactDamageSource>() != null)
            {
                Debug.LogWarning(
                    $"[{nameof(PlayerFallDamage)}] '{name}' also has a generic {nameof(VelocityImpactDamageSource)}. Remove it unless the player should damage objects by body impact.",
                    this);
            }
        }

        private void OnImpactReported(RigidbodyImpact impact)
        {
            if (config == null || Time.time < nextAllowedDamageTime)
                return;

            float groundUpDot = Vector3.Dot(impact.Normal, Vector3.up);
            if (groundUpDot < config.MinimumGroundUpDot)
                return;

            float downwardSpeed = Mathf.Max(0f, -impact.SourceVelocity.y);
            float damageAmount = config.CalculateDamage(downwardSpeed);
            if (damageAmount <= 0f)
                return;

            if (playerCollider == null)
                playerCollider = FindOwnedCollider();

            if (playerCollider == null)
                return;

            DamageInfo damage = new DamageInfo(
                damageAmount,
                DamageType.Impact,
                impact.Point,
                impact.Normal,
                impact.Direction,
                impact.HitCollider != null
                    ? impact.HitCollider.gameObject
                    : null,
                null);

            if (DamageResolver.TryApplyDamage(playerCollider, damage))
                nextAllowedDamageTime = Time.time + landingCooldown;

            // Presentation remains unclaimed so RigidbodyImpactReporter can play
            // the landing surface's normal impact VFX and audio once.
        }

        private Collider FindOwnedCollider()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            foreach (Collider candidate in colliders)
            {
                if (candidate != null
                    && !candidate.isTrigger
                    && candidate.attachedRigidbody == body)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void OnDisable()
        {
            if (impactReporter != null)
                impactReporter.ImpactReported -= OnImpactReported;
        }

        private void OnValidate()
        {
            landingCooldown = Mathf.Max(0f, landingCooldown);
        }
    }
}
