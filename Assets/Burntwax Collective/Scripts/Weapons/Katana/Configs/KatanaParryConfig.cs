using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Tunable rules for one katana's perfect parry and held guard.
    /// Timing still comes from the parry animation event in KatanaStateMachine.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Katana Parry Config",
        menuName = "Katana/Parry Config",
        order = 5)]
    public sealed class KatanaParryConfig : ScriptableObject
    {
        [Header("Valid attacks")]
        [Tooltip("Damage categories this katana can defend. Explosions are excluded by default.")]
        [SerializeField] private DamageType defendableDamageTypes =
            DamageType.Bullet
            | DamageType.Impact
            | DamageType.Slash
            | DamageType.Energy;

        [Header("Direction")]
        [Tooltip("Total frontal defense arc in degrees. 120 means 60 degrees to either side of forward.")]
        [SerializeField, Range(0f, 360f)] private float defenseArc = 120f;

        [Header("Held guard")]
        [Tooltip("Fraction of incoming damage that passes through a held block. Perfect parries always take zero.")]
        [SerializeField, Range(0f, 1f)] private float guardDamageMultiplier = 0.35f;

        [Tooltip("Fraction of incoming impulse that passes through a held block. Perfect parries always take zero.")]
        [SerializeField, Range(0f, 1f)] private float guardImpulseMultiplier = 0.25f;

        [Header("Guard capacity")]
        [Tooltip("Number of consecutive held blocks allowed before the next block attempt breaks the guard.")]
        [SerializeField, Min(1)] private int maximumConsecutiveBlocks = 3;

        [Tooltip("Continuous time outside the guard stance required to restore all guard capacity.")]
        [SerializeField, Min(0f)] private float guardRecoveryDelay = 1.5f;

        [Tooltip("If enabled, a successful perfect parry immediately restores all guard capacity.")]
        [SerializeField] private bool perfectParryRestoresGuard = true;

        public float DefenseArc => defenseArc;
        public float GuardDamageMultiplier => guardDamageMultiplier;
        public float GuardImpulseMultiplier => guardImpulseMultiplier;
        public int MaximumConsecutiveBlocks => maximumConsecutiveBlocks;
        public float GuardRecoveryDelay => guardRecoveryDelay;
        public bool PerfectParryRestoresGuard => perfectParryRestoresGuard;

        public bool CanDefend(DamageType incomingTypes)
        {
            return incomingTypes != DamageType.None
                && (incomingTypes & defendableDamageTypes) != 0;
        }

        private void OnValidate()
        {
            defenseArc = Mathf.Clamp(defenseArc, 0f, 360f);
            guardDamageMultiplier = Mathf.Clamp01(guardDamageMultiplier);
            guardImpulseMultiplier = Mathf.Clamp01(guardImpulseMultiplier);
            maximumConsecutiveBlocks = Mathf.Max(
                1,
                maximumConsecutiveBlocks);
            guardRecoveryDelay = Mathf.Max(0f, guardRecoveryDelay);
        }
    }
}
