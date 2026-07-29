using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// The plasma blast fired by the glove's charge branch. It zips along the camera forward and
    /// carries TWO concentric trigger fields:
    ///   • outer field (large)  → light knockback (the physical stand-in for a stun),
    ///   • inner field (small)  → Damage + stronger knockback.
    /// Reactions go through the same <see cref="Destructable"/> + Rigidbody channels the guns use, so
    /// the blast is functional now; dedicated enemy/stun behaviour can layer on later. Damage / force /
    /// radii are scaled by how long the blast was charged (passed in via <see cref="Launch"/>).
    ///
    /// The two fields are separate <see cref="SphereCollider"/> triggers; a small <see cref="BlastField"/>
    /// forwarder on each tells this script which tier was entered. If you don't assign them, they're
    /// created at runtime so the placeholder blast still works.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
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

        // Payload set at launch (already charge-scaled by the glove).
        public float Damage { get; private set; }
        public float Force { get; private set; }
        public float Charge { get; private set; }

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;

            if (outerField == null) outerField = CreateField("BlastOuterField", Tier.Outer, outerRadius);
            if (innerField == null) innerField = CreateField("BlastInnerField", Tier.Inner, innerRadius);
        }

        /// <param name="velocity">World velocity (camera forward * speed).</param>
        /// <param name="chargeNormalized">0..1 charge at release.</param>
        /// <param name="damage">Already charge-scaled damage.</param>
        /// <param name="force">Already charge-scaled knockback force.</param>
        public void Launch(Vector3 velocity, float chargeNormalized, float damage, float force)
        {
            Charge = chargeNormalized;
            Damage = damage;
            Force = force;

            float grow = 1f + radiusChargeBonus * chargeNormalized;
            if (outerField != null) outerField.SetRadius(outerRadius * grow);
            if (innerField != null) innerField.SetRadius(innerRadius * grow);

            rb.linearVelocity = velocity;
            Destroy(gameObject, lifetime);
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

        // A body can sit inside both fields at once; track who we've already handled so nothing is
        // damaged or shoved twice by the same blast.
        private readonly HashSet<GameObject> processedTargets = new HashSet<GameObject>();

        // ── Field reactions ─────────────────────────────────────────
        public void OnFieldEnter(Tier tier, Collider other)
        {
            if (other == null)
                return;

            Rigidbody body = other.attachedRigidbody;
            GameObject target = body != null ? body.gameObject : other.gameObject;

            // Never let the blast damage or launch the player who fired it.
            if (target.GetComponentInParent<PlayerStateMachine>() != null)
                return;

            if (!processedTargets.Add(target))
                return;

            Vector3 pushDir = (target.transform.position - transform.position).normalized;
            if (pushDir == Vector3.zero)
                pushDir = transform.forward;

            if (tier == Tier.Inner)
            {
                // Inner field: full charge-scaled damage plus a solid shove.
                Destructable destructable = other.GetComponentInParent<Destructable>();
                if (destructable != null)
                    destructable.TakeDamage(Damage);

                if (body != null)
                    body.AddForce(pushDir * Force, ForceMode.Impulse);
            }
            else
            {
                // Outer field: lighter shove only (stand-in for the eventual stun).
                if (body != null)
                    body.AddForce(pushDir * (Force * 0.5f), ForceMode.Impulse);
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
