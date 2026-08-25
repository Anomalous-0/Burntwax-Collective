using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Reusable authored rules for one enemy melee attack. Animation, AI, and
    /// hit detection consume this data without owning or duplicating it.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Enemy Melee Attack Config",
        menuName = "Enemies/Combat/Melee Attack Config",
        order = 0)]
    public sealed class EnemyMeleeAttackConfig : ScriptableObject
    {
        [Header("Damage")]
        [SerializeField, Min(0f)] private float baseDamage = 10f;

        [Tooltip("Gameplay categories carried by this attack, such as Impact for a punch or Slash for a claw or blade.")]
        [SerializeField] private DamageType damageType = DamageType.Impact;

        [Tooltip("Category used for surface audio and VFX. None falls back to Damage Type.")]
        [SerializeField] private DamageType presentationType =
            DamageType.Impact;

        [Tooltip("Impulse magnitude directed from the attack source toward the target.")]
        [SerializeField, Min(0f)] private float impactForce = 2f;

        [Header("Prototype / Fallback Timing")]
        [Tooltip("Used by the automatic prototype timeline. Animation events can open the same combat phases later.")]
        [SerializeField, Min(0f)] private float windupDuration = 0.4f;

        [Tooltip("Used by the automatic prototype timeline. Animation events will define the real hit window later.")]
        [SerializeField, Min(0.01f)] private float activeDuration = 0.15f;

        [Tooltip("Used by the automatic prototype timeline. Animation events can finish recovery later.")]
        [SerializeField, Min(0f)] private float recoveryDuration = 0.6f;

        [Tooltip("How long the attacker remains unable to act after this attack is perfectly parried.")]
        [SerializeField, Min(0f)] private float parriedStaggerDuration = 0.8f;

        [Header("Prototype Hit Volume")]
        [Tooltip("Forward distance covered by the basic capsule hit volume. Animation-driven traces can replace only the detector later.")]
        [SerializeField, Min(0f)] private float reach = 1.5f;

        [Tooltip("Radius of the basic capsule hit volume.")]
        [SerializeField, Min(0.01f)] private float radius = 0.35f;

        [Tooltip("Layers this attack is allowed to query for targets.")]
        [SerializeField] private LayerMask targetLayers = ~0;

        [Tooltip("Maximum unique actors hit by one attack. Use 1 for focused attacks and larger values for sweeping attacks.")]
        [SerializeField, Min(1)] private int maximumTargets = 1;

        public float BaseDamage => baseDamage;
        public DamageType DamageType => damageType;
        public DamageType PresentationType => presentationType !=
            DamageType.None
                ? presentationType
                : damageType;
        public float ImpactForce => impactForce;
        public float WindupDuration => windupDuration;
        public float ActiveDuration => activeDuration;
        public float RecoveryDuration => recoveryDuration;
        public float ParriedStaggerDuration => parriedStaggerDuration;
        public float Reach => reach;
        public float Radius => radius;
        public LayerMask TargetLayers => targetLayers;
        public int MaximumTargets => maximumTargets;

        private void OnValidate()
        {
            baseDamage = Mathf.Max(0f, baseDamage);
            impactForce = Mathf.Max(0f, impactForce);
            windupDuration = Mathf.Max(0f, windupDuration);
            activeDuration = Mathf.Max(0.01f, activeDuration);
            recoveryDuration = Mathf.Max(0f, recoveryDuration);
            parriedStaggerDuration = Mathf.Max(
                0f,
                parriedStaggerDuration);
            reach = Mathf.Max(0f, reach);
            radius = Mathf.Max(0.01f, radius);
            maximumTargets = Mathf.Max(1, maximumTargets);
        }
    }
}
