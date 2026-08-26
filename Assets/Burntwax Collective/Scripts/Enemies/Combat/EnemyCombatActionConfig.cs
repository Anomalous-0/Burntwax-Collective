using System;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Broad execution paths understood by the combat action state machine.
    /// Flags let the selector receive the set of action categories that have a
    /// working state/executor on this enemy.
    /// </summary>
    [Flags]
    public enum EnemyCombatActionCategory
    {
        None = 0,
        Melee = 1 << 0,
        Ranged = 1 << 1,
        Throw = 1 << 2,
        Special = 1 << 3
    }

    /// <summary>
    /// Common authored information needed to decide and present any combat
    /// action. Derived configs add execution data such as a melee hit volume or
    /// a projectile definition.
    /// </summary>
    public abstract class EnemyCombatActionConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable identifier used by save, evolution, and debugging systems.")]
        [SerializeField] private string stableId;

        [Header("Availability")]
        [Tooltip("Capabilities required before this action can be selected.")]
        [SerializeField] private EnemyCapability requiredCapabilities =
            EnemyCapability.MeleeAttack;

        [Header("Selection")]
        [Tooltip("Closest target distance at which this action may be selected.")]
        [SerializeField, Min(0f)] private float minimumRange;

        [Tooltip("Farthest target distance at which this action may be selected.")]
        [SerializeField, Min(0f)] private float maximumRange = 2f;

        [Tooltip("Maximum angle from the enemy's forward direction to the target.")]
        [SerializeField, Range(0f, 180f)] private float maximumStartAngle = 40f;

        [Tooltip("Minimum time after this action begins before it may be selected again.")]
        [SerializeField, Min(0f)] private float cooldown = 0.6f;

        [Tooltip("Only actions at the highest currently eligible priority participate in weighted selection.")]
        [SerializeField] private int selectionPriority;

        [Tooltip("Relative chance among eligible actions sharing the highest priority.")]
        [SerializeField, Min(0f)] private float selectionWeight = 1f;

        [Header("Animation")]
        [Tooltip("Animator state played when this action starts. Short names are expanded to the selected Animator layer.")]
        [SerializeField] private string animationStateName;

        [Tooltip("Animator layer containing the action state. Layer 0 is normally the Base Layer.")]
        [SerializeField, Min(0)] private int animationLayer;

        [Tooltip("Fixed time, in seconds, used to blend into this action.")]
        [SerializeField, Min(0f)] private float crossFadeDuration = 0.1f;

        public abstract EnemyCombatActionCategory Category { get; }
        public string StableId => string.IsNullOrWhiteSpace(stableId)
            ? name
            : stableId;
        public EnemyCapability RequiredCapabilities => requiredCapabilities;
        public float MinimumRange => minimumRange;
        public float MaximumRange => maximumRange;
        public float MaximumStartAngle => maximumStartAngle;
        public float Cooldown => cooldown;
        public int SelectionPriority => selectionPriority;
        public float SelectionWeight => selectionWeight;
        public string AnimationStateName => animationStateName;
        public int AnimationLayer => animationLayer;
        public float CrossFadeDuration => crossFadeDuration;

        protected virtual void OnValidate()
        {
            minimumRange = Mathf.Max(0f, minimumRange);
            maximumRange = Mathf.Max(minimumRange, maximumRange);
            maximumStartAngle = Mathf.Clamp(
                maximumStartAngle,
                0f,
                180f);
            cooldown = Mathf.Max(0f, cooldown);
            selectionWeight = Mathf.Max(0f, selectionWeight);
            animationLayer = Mathf.Max(0, animationLayer);
            crossFadeDuration = Mathf.Max(0f, crossFadeDuration);
        }
    }
}
