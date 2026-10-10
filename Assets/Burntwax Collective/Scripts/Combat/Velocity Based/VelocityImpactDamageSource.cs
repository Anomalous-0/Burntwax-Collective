using System;
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
    /// dualBlade blade detection from generic velocity damage.
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
        public GameObject LastArmedInstigator { get; private set; }
        public float LastImpactClosingSpeed { get; private set; }
        public float LastImpactSpeed { get; private set; }
        public VelocityImpactActivation Activation => activation;
        public VelocityImpactBehavior Behavior => behavior;

        /// <summary>
        /// A qualifying impact has finished defense, damage, self-damage and
        /// throw bookkeeping. Optional physical responses (such as embedding)
        /// can use the outcome without running a second damage/effects path.
        /// </summary>
        public event Action<RigidbodyImpact, HitDefenseResult> ImpactResolved;

        private RigidbodyImpactReporter impactReporter;
        private Rigidbody sourceBody;
        private Collider selfDamageCollider;
        private int remainingImpacts;
        private float disarmTime = -1f;
        private GameObject pendingArmInstigator;

        private void Awake()
        {
            impactReporter = GetComponent<RigidbodyImpactReporter>();
            sourceBody = GetComponent<Rigidbody>();
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
            LastArmedInstigator = instigator;
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

        /// <summary>
        /// Transfers a just-parried object's next damaging impact to a new
        /// instigator after the collision currently being resolved is consumed.
        /// </summary>
        public void ArmAfterCurrentImpact(GameObject instigator)
        {
            pendingArmInstigator = instigator;
        }

        private void FixedUpdate()
        {
            if (IsArmed && disarmTime >= 0f && Time.time >= disarmTime)
                Disarm();
        }

        private void OnImpactReported(RigidbodyImpact impact)
        {
            // Held or embedded objects do not become new attacks just because
            // another body collides with them.
            if (sourceBody == null || sourceBody.isKinematic)
                return;

            LastImpactClosingSpeed = impact.SourceClosingSpeed;
            LastImpactSpeed = impact.SourceVelocity.magnitude;

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
            HitResolver.ResolveHit(
                hitCollider,
                damage,
                out HitDefenseResult defenseResult);
            ApplyDefensePhysics(impact, hitCollider, defenseResult);

            // A perfect parry cancels this contact as an attack and returns the
            // object. Do not break the object from the contact that was denied.
            if (defenseResult.Outcome != HitDefenseOutcome.Parried)
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

            if (pendingArmInstigator != null)
            {
                GameObject newInstigator = pendingArmInstigator;
                pendingArmInstigator = null;
                Arm(newInstigator);
            }

            if (this != null && isActiveAndEnabled)
                ImpactResolved?.Invoke(impact, defenseResult);
        }

        private void ApplyDefensePhysics(
            RigidbodyImpact impact,
            Collider hitCollider,
            HitDefenseResult defenseResult)
        {
            if (defenseResult.Outcome != HitDefenseOutcome.Blocked
                && defenseResult.Outcome != HitDefenseOutcome.Parried)
            {
                return;
            }

            Vector3 contactNormal = impact.Normal.sqrMagnitude > Mathf.Epsilon
                ? impact.Normal.normalized
                : -impact.Direction;
            if (contactNormal.sqrMagnitude <= Mathf.Epsilon)
                return;

            Rigidbody defenderBody = hitCollider.attachedRigidbody;
            if (defenderBody != null
                && !defenderBody.isKinematic)
            {
                RigidbodyImpactReporter defenderReporter =
                    defenderBody.GetComponent<RigidbodyImpactReporter>();
                if (defenderReporter != null)
                {
                    Vector3 velocityBeforeCollision =
                        defenderReporter.VelocityBeforePhysics;
                    Vector3 velocityAfterCollision =
                        defenderBody.linearVelocity;
                    float normalSpeedBefore = Vector3.Dot(
                        velocityBeforeCollision,
                        contactNormal);
                    float normalSpeedAfter = Vector3.Dot(
                        velocityAfterCollision,
                        contactNormal);
                    float retainedNormalSpeed = Mathf.Lerp(
                        normalSpeedBefore,
                        normalSpeedAfter,
                        defenseResult.CollisionImpulseMultiplier);

                    defenderBody.linearVelocity = velocityAfterCollision
                        + contactNormal
                        * (retainedNormalSpeed - normalSpeedAfter);
                }
            }

            if (defenseResult.SourceMotionHandled)
                return;

            if (sourceBody == null)
                sourceBody = GetComponent<Rigidbody>();

            if (sourceBody == null || sourceBody.isKinematic)
                return;

            Vector3 tangentialVelocity = Vector3.ProjectOnPlane(
                sourceBody.linearVelocity,
                contactNormal);
            float reboundSpeed = impact.SourceClosingSpeed
                * defenseResult.SourceReboundSpeedMultiplier;
            sourceBody.linearVelocity = tangentialVelocity
                + contactNormal * reboundSpeed;
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
            pendingArmInstigator = null;
            LastArmedInstigator = null;
            LastImpactClosingSpeed = 0f;
            LastImpactSpeed = 0f;
        }
    }
}
