using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Opt-in damage source for ordinary physics objects that have been deliberately thrown.
    /// A Rigidbody is harmless until a thrower calls <see cref="Arm"/>. This keeps generic
    /// velocity damage separate from guns, energy projectiles, and katana blade detection.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(RigidbodyImpactReporter))]
    public sealed class VelocityImpactDamageSource : MonoBehaviour
    {
        [SerializeField] private VelocityImpactDamageConfig config;

        public bool IsArmed { get; private set; }
        public GameObject Instigator { get; private set; }

        private RigidbodyImpactReporter impactReporter;
        private int remainingImpacts;
        private float disarmTime = -1f;

        private void Awake()
        {
            impactReporter = GetComponent<RigidbodyImpactReporter>();
        }

        private void OnEnable()
        {
            if (impactReporter == null)
                impactReporter = GetComponent<RigidbodyImpactReporter>();

            if (impactReporter != null)
                impactReporter.ImpactReported += OnImpactReported;
        }

        /// <summary>
        /// Makes this object a temporary impact-damage source and records who threw it.
        /// The instigator is ignored as a target and is forwarded through DamageInfo.
        /// </summary>
        public void Arm(GameObject instigator)
        {
            if (config == null)
            {
                Debug.LogWarning(
                    $"[{nameof(VelocityImpactDamageSource)}] Cannot arm '{name}' without a damage config.",
                    this);
                Disarm();
                return;
            }

            Instigator = instigator;
            remainingImpacts = config.MaximumImpacts;
            disarmTime = config.ArmedLifetime > 0f
                ? Time.time + config.ArmedLifetime
                : -1f;
            IsArmed = true;
        }

        public void Disarm()
        {
            IsArmed = false;
            Instigator = null;
            remainingImpacts = 0;
            disarmTime = -1f;
        }

        private void FixedUpdate()
        {
            if (IsArmed && disarmTime >= 0f && Time.time >= disarmTime)
                Disarm();
        }

        private void OnImpactReported(RigidbodyImpact impact)
        {
            if (!IsArmed || config == null)
                return;

            Collider hitCollider = impact.HitCollider;
            if (hitCollider == null || IsInstigatorCollider(hitCollider))
                return;

            if (!config.MeetsDamageThreshold(impact.ClosingSpeed))
                return;

            DamageInfo damage = new DamageInfo(
                config.Damage,
                config.DamageTypes,
                impact.Point,
                impact.Normal,
                impact.Direction,
                gameObject,
                Instigator,
                Vector3.zero,
                config.PresentationType);

            // Unity's collision has already transferred physical momentum. A zero gameplay
            // impulse prevents HitResolver from applying that force a second time.
            // Claim presentation before resolving the hit so the reporter cannot add a second,
            // generic Rigidbody effect after this profile's selected VFX/audio has played.
            impact.MarkPresentationHandled();
            HitResolver.ResolveHit(hitCollider, damage);

            remainingImpacts--;
            if (remainingImpacts <= 0)
                Disarm();
        }

        private bool IsInstigatorCollider(Collider other)
        {
            if (Instigator == null || other == null)
                return false;

            Transform instigatorTransform = Instigator.transform;
            return other.gameObject == Instigator
                || other.transform.IsChildOf(instigatorTransform);
        }

        private void OnDisable()
        {
            if (impactReporter != null)
                impactReporter.ImpactReported -= OnImpactReported;

            Disarm();
        }
    }
}
