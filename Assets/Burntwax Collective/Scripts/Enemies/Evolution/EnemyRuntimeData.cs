using System;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Features an enemy is currently allowed to use. State-machine code asks
    /// this set what is available instead of checking concrete species names.
    /// Evolution can therefore grant Flight or FeintAttack without adding an
    /// "if alien type X" branch to the state machine.
    /// </summary>
    [Flags]
    public enum EnemyCapability
    {
        None = 0,
        GroundMovement = 1 << 0,
        Jumping = 1 << 1,
        Flying = 1 << 2,
        MeleeAttack = 1 << 3,
        RangedAttack = 1 << 4,
        ThrowAttack = 1 << 5,
        Blocking = 1 << 6,
        FeintAttack = 1 << 7,
        SpecialAttack = 1 << 8
    }

    /// <summary>
    /// Fully resolved values used by one spawned enemy. This is runtime data,
    /// not the shared archetype asset. Difficulty and evolution should compose
    /// into this value rather than mutate ScriptableObjects during play.
    /// </summary>
    [Serializable]
    public struct EnemyRuntimeStats
    {
        [SerializeField, Min(0.01f)] private float maximumHealth;
        [SerializeField, Min(0f)] private float groundMoveSpeed;
        [SerializeField, Min(0f)] private float sprintSpeed;
        [SerializeField, Min(0f)] private float turnSpeed;
        [SerializeField, Min(0f)] private float meleeDamageMultiplier;
        [SerializeField, Min(0.01f)] private float attackSpeedMultiplier;
        [SerializeField, Min(0f)] private float staggerDurationMultiplier;

        public float MaximumHealth => maximumHealth;
        public float GroundMoveSpeed => groundMoveSpeed;
        public float SprintSpeed => sprintSpeed;
        public float TurnSpeed => turnSpeed;
        public float MeleeDamageMultiplier => meleeDamageMultiplier;

        /// <summary>
        /// Values above one make authored attack phases complete faster. The
        /// animation layer may eventually apply this as playback speed too.
        /// </summary>
        public float AttackSpeedMultiplier => attackSpeedMultiplier;

        public float StaggerDurationMultiplier =>
            staggerDurationMultiplier;

        public static EnemyRuntimeStats Default => new EnemyRuntimeStats(
            100f,
            3.5f,
            6f,
            360f,
            1f,
            1f,
            1f);

        public EnemyRuntimeStats(
            float maximumHealth,
            float groundMoveSpeed,
            float sprintSpeed,
            float turnSpeed,
            float meleeDamageMultiplier,
            float attackSpeedMultiplier,
            float staggerDurationMultiplier)
        {
            this.maximumHealth = Mathf.Max(0.01f, maximumHealth);
            this.groundMoveSpeed = Mathf.Max(0f, groundMoveSpeed);
            this.sprintSpeed = Mathf.Max(0f, sprintSpeed);
            this.turnSpeed = Mathf.Max(0f, turnSpeed);
            this.meleeDamageMultiplier = Mathf.Max(
                0f,
                meleeDamageMultiplier);
            this.attackSpeedMultiplier = Mathf.Max(
                0.01f,
                attackSpeedMultiplier);
            this.staggerDurationMultiplier = Mathf.Max(
                0f,
                staggerDurationMultiplier);
        }

        public EnemyRuntimeStats Apply(EnemyStatModifiers modifiers)
        {
            return new EnemyRuntimeStats(
                modifiers.ApplyMaximumHealth(maximumHealth),
                modifiers.ApplyGroundMoveSpeed(groundMoveSpeed),
                modifiers.ApplySprintSpeed(sprintSpeed),
                modifiers.ApplyTurnSpeed(turnSpeed),
                modifiers.ApplyMeleeDamage(meleeDamageMultiplier),
                modifiers.ApplyAttackSpeed(attackSpeedMultiplier),
                modifiers.ApplyStaggerDuration(
                    staggerDurationMultiplier));
        }

        public EnemyRuntimeStats Sanitized()
        {
            return new EnemyRuntimeStats(
                maximumHealth,
                groundMoveSpeed,
                sprintSpeed,
                turnSpeed,
                meleeDamageMultiplier,
                attackSpeedMultiplier,
                staggerDurationMultiplier);
        }
    }

    /// <summary>
    /// Additive and percentage changes contributed by one trait. Percentage
    /// fields are bonuses: 0 means unchanged, 0.25 means +25%, and -0.2 means
    /// -20%. This makes an unconfigured trait safely neutral.
    /// </summary>
    [Serializable]
    public struct EnemyStatModifiers
    {
        [Header("Health")]
        [SerializeField] private float maximumHealthAdditive;
        [SerializeField] private float maximumHealthMultiplierBonus;

        [Header("Movement percentage bonuses")]
        [SerializeField] private float groundMoveSpeedMultiplierBonus;
        [SerializeField] private float sprintSpeedMultiplierBonus;
        [SerializeField] private float turnSpeedMultiplierBonus;

        [Header("Combat percentage bonuses")]
        [SerializeField] private float meleeDamageMultiplierBonus;
        [SerializeField] private float attackSpeedMultiplierBonus;
        [SerializeField] private float staggerDurationMultiplierBonus;

        public float ApplyMaximumHealth(float value) => Mathf.Max(
            0.01f,
            ApplyPercentage(
                value + maximumHealthAdditive,
                maximumHealthMultiplierBonus));

        public float ApplyGroundMoveSpeed(float value) => Mathf.Max(
            0f,
            ApplyPercentage(value, groundMoveSpeedMultiplierBonus));

        public float ApplySprintSpeed(float value) => Mathf.Max(
            0f,
            ApplyPercentage(value, sprintSpeedMultiplierBonus));

        public float ApplyTurnSpeed(float value) => Mathf.Max(
            0f,
            ApplyPercentage(value, turnSpeedMultiplierBonus));

        public float ApplyMeleeDamage(float value) => Mathf.Max(
            0f,
            ApplyPercentage(value, meleeDamageMultiplierBonus));

        public float ApplyAttackSpeed(float value) => Mathf.Max(
            0.01f,
            ApplyPercentage(value, attackSpeedMultiplierBonus));

        public float ApplyStaggerDuration(float value) => Mathf.Max(
            0f,
            ApplyPercentage(value, staggerDurationMultiplierBonus));

        private static float ApplyPercentage(float value, float bonus)
        {
            // Prevent an accidental modifier below -100% from inverting a stat.
            return value * (1f + Mathf.Max(-1f, bonus));
        }
    }
}
