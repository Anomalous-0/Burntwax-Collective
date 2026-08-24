using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Describes how an intentionally thrown object turns its authored impact strength
    /// into gameplay damage. Dull is zero so existing serialized components retain
    /// their current behavior when this setting is introduced.
    /// </summary>
    public enum VelocityImpactBehavior
    {
        Dull = 0,
        Sharp = 1,
        NoDamage = 2
    }

    /// <summary>
    /// Controls whether an opted-in Rigidbody is dangerous only after a
    /// deliberate throw or whenever its own collision speed passes the gate.
    /// ArmedOnly is zero so existing serialized components preserve behavior.
    /// </summary>
    public enum VelocityImpactActivation
    {
        ArmedOnly = 0,
        AlwaysActive = 1
    }

    /// <summary>
    /// Opt-in damage source for ordinary physics objects. It can be dangerous
    /// only after <see cref="Arm"/> or on every qualifying collision. Keeping
    /// this behavior component-based excludes guns, energy projectiles, and
    /// katana blade detection from generic velocity damage.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(RigidbodyImpactReporter))]
    public sealed class VelocityImpactDamageSource : MonoBehaviour
    {
        [SerializeField] private VelocityImpactDamageConfig config;

        [Tooltip("Armed Only requires Arm() (for example, a glove throw). Always Active evaluates every collision using this Rigidbody's own speed.")]
        [SerializeField] private VelocityImpactActivation activation =
            VelocityImpactActivation.ArmedOnly;

        [Tooltip("Dull deals Impact damage, Sharp adds Slash reactions, and No Damage leaves only passive collision effects.")]
        [SerializeField] private VelocityImpactBehavior behavior = VelocityImpactBehavior.Dull;

        public bool IsArmed { get; private set; }
        public GameObject Instigator { get; private set; }
        public VelocityImpactActivation Activation => activation;

        private RigidbodyImpactReporter impactReporter;
        private Collider selfDamageCollider;
        private int remainingImpacts;
        private float disarmTime = -1f;

        private void Awake()
        {
            impactReporter = GetComponent<RigidbodyImpactReporter>();
            selfDamageCollider = FindOwnedCollider();
        }

        private void OnEnable()
        {
            if (impactReporter == null)
                impactReporter = GetComponent<RigidbodyImpactReporter>();

            if (impactReporter != null)
                impactReporter.ImpactReported += OnImpactReported;

            if (activation == VelocityImpactActivation.AlwaysActive
                && behavior != VelocityImpactBehavior.NoDamage
                && config == null)
            {
                Debug.LogWarning(
                    $"[{nameof(VelocityImpactDamageSource)}] Always-active source '{name}' has no damage config.",
                    this);
            }
        }

        /// <summary>
        /// Makes this object a temporary impact-damage source and records who threw it.
        /// The instigator is ignored as a target and is forwarded through DamageInfo.
        /// </summary>
        public void Arm(GameObject instigator)
        {
            if (behavior == VelocityImpactBehavior.NoDamage)
            {
                Disarm();
                return;
            }

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
            bool canDealDamage = behavior != VelocityImpactBehavior.NoDamage
                && config != null
                && (IsArmed
                    || activation == VelocityImpactActivation.AlwaysActive);

            if (!canDealDamage)
                return;

            Collider hitCollider = impact.HitCollider;
            if (hitCollider == null || IsInstigatorCollider(hitCollider))
                return;

            float damageScale = config.EvaluateDamageScale(
                impact.SourceClosingSpeed);
            if (damageScale <= 0f)
                return;

            float targetDamage = config.DamageAtFullSpeed * damageScale;
            float selfDamage = config.SelfDamageAtFullSpeed * damageScale;

            bool isSharp = behavior == VelocityImpactBehavior.Sharp;
            DamageType gameplayTypes = isSharp
                ? DamageType.Impact | DamageType.Slash
                : DamageType.Impact;
            DamageType presentationType = isSharp
                ? DamageType.Slash
                : DamageType.Impact;

            DamageInfo damage = new DamageInfo(
                targetDamage,
                gameplayTypes,
                impact.Point,
                impact.Normal,
                impact.Direction,
                gameObject,
                Instigator,
                Vector3.zero,
                presentationType);

            // Unity's collision has already transferred physical momentum. A zero gameplay
            // impulse prevents HitResolver from applying that force a second time.
            // Claim presentation before resolving the hit so the reporter cannot add a second,
            // generic Rigidbody effect after this profile's selected VFX/audio has played.
            impact.MarkPresentationHandled();
            HitResolver.ResolveHit(hitCollider, damage);

            ApplySelfDamage(impact, hitCollider, selfDamage);

            // Always-active objects remain physically dangerous after their
            // temporary throw attribution expires. Impact limits only consume
            // the armed throw, never the general collision behavior.
            if (IsArmed)
            {
                remainingImpacts--;
                if (remainingImpacts <= 0)
                    Disarm();
            }
        }

        private bool IsInstigatorCollider(Collider other)
        {
            if (Instigator == null || other == null)
                return false;

            Transform instigatorTransform = Instigator.transform;
            return other.gameObject == Instigator
                || other.transform.IsChildOf(instigatorTransform);
        }

        private void ApplySelfDamage(
            RigidbodyImpact impact,
            Collider hitCollider,
            float selfDamageAmount)
        {
            if (selfDamageAmount <= 0f)
                return;

            if (selfDamageCollider == null)
                selfDamageCollider = FindOwnedCollider();

            if (selfDamageCollider == null)
                return;

            DamageInfo selfDamage = new DamageInfo(
                selfDamageAmount,
                DamageType.Impact,
                impact.Point,
                -impact.Normal,
                -impact.Direction,
                hitCollider.gameObject,
                Instigator);

            // The outward hit already owns presentation and Unity already resolved
            // collision momentum. Only route gameplay damage back into this object.
            DamageResolver.TryApplyDamage(selfDamageCollider, selfDamage);
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

            Disarm();
        }
    }
}
