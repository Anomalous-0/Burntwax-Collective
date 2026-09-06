using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Immutable defense rules shared by enemies with the same defensive style.
    /// Runtime state such as "currently guarding" belongs to EnemyDefense, not
    /// this asset, so multiple spawned enemies can safely share one config.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Enemy Defense Config",
        menuName = "Enemies/Combat/Defense Config",
        order = 3)]
    public sealed class EnemyDefenseConfig : ScriptableObject
    {
        [Header("Eligibility")]
        [Tooltip("Only hits containing at least one of these damage types can be blocked or parried.")]
        [SerializeField] private DamageType defendedDamageTypes =
            DamageType.Slash;

        [Tooltip("Horizontal frontal arc in degrees. 180 protects the entire front half; 360 protects every direction.")]
        [SerializeField, Range(0f, 360f)] private float defenseArc = 140f;

        [Header("Normal Block")]
        [Tooltip("Fraction of incoming damage retained after a normal block.")]
        [SerializeField, Range(0f, 1f)] private float blockDamageMultiplier =
            0.25f;

        [Tooltip("Fraction of authored gameplay impulse retained after a normal block.")]
        [SerializeField, Range(0f, 1f)] private float blockImpulseMultiplier =
            0.25f;

        [Tooltip("Fraction of collision motion the defender retains when blocking a Rigidbody impact.")]
        [SerializeField, Range(0f, 1f)]
        private float blockCollisionImpulseMultiplier = 0.25f;

        [Tooltip("Multiplier used when a blocked Rigidbody rebounds away from the defender.")]
        [SerializeField, Min(0f)]
        private float blockSourceReboundSpeedMultiplier = 0.25f;

        [Header("Perfect Parry")]
        [Tooltip("Fraction of collision motion the defender retains during a perfect parry.")]
        [SerializeField, Range(0f, 1f)]
        private float parryCollisionImpulseMultiplier;

        [Tooltip("Multiplier used when a parried Rigidbody rebounds away from the defender.")]
        [SerializeField, Min(0f)]
        private float parrySourceReboundSpeedMultiplier = 1f;

        public DamageType DefendedDamageTypes => defendedDamageTypes;
        public float DefenseArc => defenseArc;
        public float BlockDamageMultiplier => blockDamageMultiplier;
        public float BlockImpulseMultiplier => blockImpulseMultiplier;
        public float BlockCollisionImpulseMultiplier =>
            blockCollisionImpulseMultiplier;
        public float BlockSourceReboundSpeedMultiplier =>
            blockSourceReboundSpeedMultiplier;
        public float ParryCollisionImpulseMultiplier =>
            parryCollisionImpulseMultiplier;
        public float ParrySourceReboundSpeedMultiplier =>
            parrySourceReboundSpeedMultiplier;

        public bool CanDefend(DamageType incomingTypes)
        {
            return (incomingTypes & defendedDamageTypes) != 0;
        }

        private void OnValidate()
        {
            defenseArc = Mathf.Clamp(defenseArc, 0f, 360f);
            blockDamageMultiplier = Mathf.Clamp01(blockDamageMultiplier);
            blockImpulseMultiplier = Mathf.Clamp01(blockImpulseMultiplier);
            blockCollisionImpulseMultiplier = Mathf.Clamp01(
                blockCollisionImpulseMultiplier);
            blockSourceReboundSpeedMultiplier = Mathf.Max(
                0f,
                blockSourceReboundSpeedMultiplier);
            parryCollisionImpulseMultiplier = Mathf.Clamp01(
                parryCollisionImpulseMultiplier);
            parrySourceReboundSpeedMultiplier = Mathf.Max(
                0f,
                parrySourceReboundSpeedMultiplier);
        }
    }
}
