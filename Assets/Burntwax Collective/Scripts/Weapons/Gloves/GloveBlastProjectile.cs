using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// The plasma blast fired by the glove's charge branch. It zips along the camera forward and
    /// carries TWO concentric trigger fields:
    ///   • outer field (large)  → light knockback (the physical stand-in for a stun),
    ///   • inner field (small)  → Damage + stronger knockback.
    /// Inner hits go through the shared hit resolver. Outer hits use only the shared impact
    /// resolver because that field is a shove/stun and must not invoke damage reactions.
    /// Damage, force, and radii are scaled by how long the blast was charged (passed in via
    /// <see cref="Launch"/>).
    ///
    /// The two fields are separate <see cref="SphereCollider"/> triggers; a small <see cref="BlastField"/>
    /// forwarder on each tells this script which tier was entered. The root sphere is a nonphysical
    /// trigger used only to size a swept sphere cast, so only the configured gameplay impulse can
    /// move a target. If you don't assign the fields, they're created at runtime so the placeholder
    /// blast still works.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class GloveBlastProjectile : MonoBehaviour
    {
        public enum Tier { Outer, Inner }

        [Header("Fields (world-space radii)")]
        [SerializeField] private float outerRadius = 4f;   // stun
        [SerializeField] private float innerRadius = 1.5f; // extra stun + damage
        [Tooltip("How much the radii grow from 0% to 100% charge.")]
        [SerializeField] private float radiusChargeBonus = 1.5f;

        [Header("Trigger fields (auto-created if empty)")]
        [SerializeField] private BlastField outerField;
        [SerializeField] private BlastField innerField;

        [Header("Lifetime")]
        [SerializeField] private float lifetime = 5f;

        [Header("World collision")]
        [SerializeField] private LayerMask collisionLayers = ~0;
        [SerializeField, Min(0f)] private float collisionSkin = 0.01f;

        // Payload set at launch (already charge-scaled by the glove).
        public float Damage { get; private set; }
        public float Force { get; private set; }
        public float Charge { get; private set; }

        private Rigidbody rb;
        private SphereCollider collisionCollider;
        private GameObject instigator;
        private bool isTerminating;

        private const int MaxSweepHits = 16;
        private readonly RaycastHit[] sweepHits = new RaycastHit[MaxSweepHits];

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            collisionCollider = GetComponent<SphereCollider>();
            if (collisionCollider == null)
                collisionCollider = gameObject.AddComponent<SphereCollider>();
            collisionCollider.isTrigger = true;

            if (outerField == null) outerField = CreateField("BlastOuterField", Tier.Outer, outerRadius);
            if (innerField == null) innerField = CreateField("BlastInnerField", Tier.Inner, innerRadius);
        }

        /// <param name="velocity">World velocity (camera forward * speed).</param>
        /// <param name="chargeNormalized">0..1 charge at release.</param>
        /// <param name="damage">Already charge-scaled damage.</param>
        /// <param name="force">Already charge-scaled knockback force.</param>
        public void Launch(
            Vector3 velocity,
            float chargeNormalized,
            float damage,
            float force,
            GameObject instigator)
        {
            Charge = chargeNormalized;
            Damage = damage;
            Force = force;
            this.instigator = instigator;

            float grow = 1f + radiusChargeBonus * chargeNormalized;
            if (outerField != null) outerField.SetRadius(outerRadius * grow);
            if (innerField != null) innerField.SetRadius(innerRadius * grow);

            rb.linearVelocity = velocity;
            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            if (isTerminating || rb == null || collisionCollider == null)
                return;

            Vector3 movement = rb.linearVelocity * Time.fixedDeltaTime;
            float distance = movement.magnitude;
            if (distance <= Mathf.Epsilon)
                return;

            Vector3 direction = movement / distance;
            Vector3 castOrigin = transform.TransformPoint(collisionCollider.center);
            float radius = GetWorldCollisionRadius();

            int hitCount = Physics.SphereCastNonAlloc(
                castOrigin,
                radius,
                direction,
                sweepHits,
                distance,
                collisionLayers,
                QueryTriggerInteraction.Ignore);

            int nearestIndex = -1;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = sweepHits[i].collider;
                if (hitCollider == null || IsInstigatorCollider(hitCollider))
                    continue;

                if (sweepHits[i].distance < nearestDistance)
                {
                    nearestIndex = i;
                    nearestDistance = sweepHits[i].distance;
                }
            }

            if (nearestIndex >= 0)
                ResolveWorldImpact(sweepHits[nearestIndex], direction, radius, castOrigin);
        }

        private float GetWorldCollisionRadius()
        {
            Vector3 scale = transform.lossyScale;
            float largestAxis = Mathf.Max(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z));
            return collisionCollider.radius * largestAxis;
        }

        private void ResolveWorldImpact(
            RaycastHit hit,
            Vector3 travelDirection,
            float radius,
            Vector3 currentWorldCenter)
        {
            isTerminating = true;
            rb.linearVelocity = Vector3.zero;

            // Place the projectile core immediately outside the surface. This keeps any final
            // frame of the visual at the collision instead of one physics step behind it.
            Vector3 finalCenter = hit.point + hit.normal * (radius + collisionSkin);
            transform.position += finalCenter - currentWorldCenter;

            // A fast projectile can cross a trigger field between physics updates. Guarantee that
            // the directly struck target still receives its inner-tier damage and configured force.
            ApplyFieldEnter(Tier.Inner, hit.collider);

            // Field triggers may happen before the small core reaches a surface, so only the core
            // sweep owns presentation. Its cast supplies the exact contact point and normal.
            DamageInfo surfaceInfo = new DamageInfo(
                0f,
                DamageType.Energy | DamageType.Impact,
                hit.point,
                hit.normal,
                travelDirection,
                gameObject,
                instigator,
                Vector3.zero);

            SurfaceImpactSystem.Instance?.PlayImpact(hit.collider, surfaceInfo);

            collisionCollider.enabled = false;
            Destroy(gameObject);
        }

        private bool IsInstigatorCollider(Collider other)
        {
            return instigator != null
                && other != null
                && (other.gameObject == instigator
                    || other.transform.IsChildOf(instigator.transform));
        }

        private BlastField CreateField(string childName, Tier tier, float radius)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            var field = go.AddComponent<BlastField>();
            field.Init(this, tier);
            return field;
        }

        // The outer field normally fires first. Track force and damage independently so entering
        // the outer field cannot prevent the inner field from dealing damage.
        private readonly HashSet<GameObject> outerPushTargets = new HashSet<GameObject>();
        private readonly HashSet<GameObject> innerDamageTargets = new HashSet<GameObject>();

        // ── Field reactions ─────────────────────────────────────────
        public void OnFieldEnter(Tier tier, Collider other)
        {
            if (isTerminating)
                return;

            ApplyFieldEnter(tier, other);
        }

        private void ApplyFieldEnter(Tier tier, Collider other)
        {
            if (other == null)
                return;

            Rigidbody body = other.attachedRigidbody;
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            Component damageReceiver = damageable as Component;
            GameObject target = damageReceiver != null
                ? damageReceiver.gameObject
                : body != null ? body.gameObject : other.gameObject;

            // Never let the blast damage or launch the player who fired it.
            if (instigator != null
                && (target == instigator
                    || target.transform.IsChildOf(instigator.transform)))
            {
                return;
            }

            // Preserve the previous player-safety fallback if an older caller launches without
            // providing an instigator.
            if (instigator == null
                && target.GetComponentInParent<PlayerStateMachine>() != null)
                return;

            Vector3 pushDir = (target.transform.position - transform.position).normalized;
            if (pushDir == Vector3.zero)
                pushDir = transform.forward;

            if (tier == Tier.Inner)
            {
                if (!innerDamageTargets.Add(target))
                    return;

                bool outerForceAlreadyApplied = outerPushTargets.Contains(target);
                float remainingForce = outerForceAlreadyApplied
                    ? Force * 0.5f
                    : Force;

                // If the inner trigger happens first, reserve the outer tier so it cannot
                // add another half impulse afterward.
                outerPushTargets.Add(target);

                DamageInfo damageInfo = new DamageInfo(
                    Damage,
                    DamageType.Energy | DamageType.Impact,
                    other.ClosestPoint(transform.position),
                    -pushDir,
                    pushDir,
                    gameObject,
                    instigator,
                    pushDir * remainingForce);

                // The projectile core owns the one visible/audio impact at its precise swept
                // contact. The field still owns damage and configured impulse application.
                HitResolver.ResolveHit(
                    other,
                    damageInfo,
                    playImpactAudio: false,
                    playSurfaceImpact: false);
            }
            else
            {
                if (!outerPushTargets.Add(target))
                    return;

                // Outer field: lighter shove only (stand-in for the eventual stun).
                // Do not use HitResolver here: a zero-damage call could still trigger a
                // target-specific IDamageable reaction.
                DamageInfo impactInfo = new DamageInfo(
                    0f,
                    DamageType.Energy | DamageType.Impact,
                    other.ClosestPoint(transform.position),
                    -pushDir,
                    pushDir,
                    gameObject,
                    instigator,
                    pushDir * (Force * 0.5f));

                ImpactResolver.TryApplyImpact(other, impactInfo);
            }
        }
    }

    /// <summary>
    /// Sits on each of the blast's two trigger spheres and forwards trigger events to the parent
    /// <see cref="GloveBlastProjectile"/> tagged with which tier fired.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class BlastField : MonoBehaviour
    {
        private GloveBlastProjectile owner;
        private GloveBlastProjectile.Tier tier;
        private SphereCollider col;

        public void Init(GloveBlastProjectile owner, GloveBlastProjectile.Tier tier)
        {
            this.owner = owner;
            this.tier = tier;
            col = GetComponent<SphereCollider>();
            col.isTrigger = true;
        }

        public void SetRadius(float r)
        {
            if (col == null) col = GetComponent<SphereCollider>();
            col.radius = r;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null) owner.OnFieldEnter(tier, other);
        }
    }
}
