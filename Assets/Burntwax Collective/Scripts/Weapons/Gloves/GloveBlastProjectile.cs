using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// The plasma blast fired by the glove's charge branch. It zips along the camera forward and
    /// carries TWO concentric trigger fields for future enemy reactions:
    ///   • outer field (large)  → stun,
    ///   • inner field (small)  → extra stun + damage.
    /// Enemies don't exist yet, so the field callbacks are stubs. Damage / force / radii are scaled
    /// by how long the blast was charged (passed in via <see cref="Launch"/>).
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

        // ── Future enemy reactions (wired once enemies exist) ────
        public void OnFieldEnter(Tier tier, Collider other)
        {
            // TODO(enemies): if `other` is an enemy →
            //   Outer: apply stun (duration scaled by Charge)
            //   Inner: apply stronger stun + Damage, and knockback along (other - transform).normalized * Force
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
