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

        [Header("Rigidbody defense")]
        [Tooltip("Fraction of a Rigidbody collision impulse that reaches the defender during an ordinary block.")]
        [SerializeField, Range(0f, 1f)]
        private float guardCollisionImpulseMultiplier = 0.25f;

        [Tooltip("Fraction of incoming closing speed used to rebound a Rigidbody after an ordinary block.")]
        [SerializeField, Min(0f)]
        private float guardReboundSpeedMultiplier = 0.25f;

        [Tooltip("Fallback rebound used only when the targeted perfect-parry launcher cannot run.")]
        [InspectorName("Fallback Perfect Parry Rebound Multiplier")]
        [SerializeField, Min(0f)]
        private float perfectParryReboundSpeedMultiplier = 1f;

        [Header("Perfect parry return")]
        [Tooltip("Multiplies the Rigidbody's actual incoming speed for a perfect-parry return. 1 preserves speed, 2 doubles it, and 0.5 halves it.")]
        [SerializeField, Min(0f)]
        private float perfectParryReturnSpeedMultiplier = 1f;

        [Tooltip("Return-arc height added per metre between the object and the recorded thrower's aim target.")]
        [SerializeField, Min(0f)]
        private float perfectParryArcHeightPerMeter = 0.06f;

        [SerializeField, Min(0f)]
        private float perfectParryMinimumArcHeight = 0.15f;

        [SerializeField, Min(0f)]
        private float perfectParryMaximumArcHeight = 1.25f;

        [Tooltip("Minimum random angular speed applied to a returned Rigidbody. Heavier objects receive proportionally less spin.")]
        [SerializeField, Min(0f)]
        private float perfectParryMinimumSpin = 1f;

        [Tooltip("Maximum random angular speed applied to a returned Rigidbody. Heavier objects receive proportionally less spin.")]
        [SerializeField, Min(0f)]
        private float perfectParryMaximumSpin = 4f;

        [Header("Guard capacity")]
        [Tooltip("Number of consecutive held blocks allowed before the next block attempt breaks the guard.")]
        [SerializeField, Min(1)] private int maximumConsecutiveBlocks = 3;

        [Tooltip("Continuous time outside the guard stance required to restore one spent guard use.")]
        [SerializeField, Min(0f)] private float guardRecoveryDelay = 1.5f;

        [Tooltip("If enabled, a successful perfect parry immediately restores one spent guard use.")]
        [SerializeField] private bool perfectParryRestoresGuard = true;

        [Header("Parried attacker reaction")]
        [Tooltip("Seconds this katana cannot act after one of its slashes is perfectly parried.")]
        [SerializeField, Min(0f)]
        private float parriedAttackRecoveryDuration = 0.35f;

        [Header("Feedback")]
        [Tooltip("Audio and VFX used for blocks, perfect parries, and guard breaks.")]
        [SerializeField]
        private KatanaDefenseFeedbackConfig defenseFeedback;

        public float DefenseArc => defenseArc;
        public float GuardDamageMultiplier => guardDamageMultiplier;
        public float GuardImpulseMultiplier => guardImpulseMultiplier;
        public float GuardCollisionImpulseMultiplier =>
            guardCollisionImpulseMultiplier;
        public float GuardReboundSpeedMultiplier =>
            guardReboundSpeedMultiplier;
        public float PerfectParryReboundSpeedMultiplier =>
            perfectParryReboundSpeedMultiplier;
        public float PerfectParryReturnSpeedMultiplier =>
            perfectParryReturnSpeedMultiplier;
        public float PerfectParryArcHeightPerMeter =>
            perfectParryArcHeightPerMeter;
        public float PerfectParryMinimumArcHeight =>
            perfectParryMinimumArcHeight;
        public float PerfectParryMaximumArcHeight =>
            perfectParryMaximumArcHeight;
        public float PerfectParryMinimumSpin => perfectParryMinimumSpin;
        public float PerfectParryMaximumSpin => perfectParryMaximumSpin;
        public int MaximumConsecutiveBlocks => maximumConsecutiveBlocks;
        public float GuardRecoveryDelay => guardRecoveryDelay;
        public bool PerfectParryRestoresGuard => perfectParryRestoresGuard;
        public float ParriedAttackRecoveryDuration =>
            parriedAttackRecoveryDuration;
        public KatanaDefenseFeedbackConfig DefenseFeedback =>
            defenseFeedback;

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
            guardCollisionImpulseMultiplier = Mathf.Clamp01(
                guardCollisionImpulseMultiplier);
            guardReboundSpeedMultiplier = Mathf.Max(
                0f,
                guardReboundSpeedMultiplier);
            perfectParryReboundSpeedMultiplier = Mathf.Max(
                0f,
                perfectParryReboundSpeedMultiplier);
            perfectParryReturnSpeedMultiplier = Mathf.Max(
                0f,
                perfectParryReturnSpeedMultiplier);
            perfectParryArcHeightPerMeter = Mathf.Max(
                0f,
                perfectParryArcHeightPerMeter);
            perfectParryMinimumArcHeight = Mathf.Max(
                0f,
                perfectParryMinimumArcHeight);
            perfectParryMaximumArcHeight = Mathf.Max(
                perfectParryMinimumArcHeight,
                perfectParryMaximumArcHeight);
            perfectParryMinimumSpin = Mathf.Max(
                0f,
                perfectParryMinimumSpin);
            perfectParryMaximumSpin = Mathf.Max(
                perfectParryMinimumSpin,
                perfectParryMaximumSpin);
            maximumConsecutiveBlocks = Mathf.Max(
                1,
                maximumConsecutiveBlocks);
            guardRecoveryDelay = Mathf.Max(0f, guardRecoveryDelay);
            parriedAttackRecoveryDuration = Mathf.Max(
                0f,
                parriedAttackRecoveryDuration);
        }
    }
}
