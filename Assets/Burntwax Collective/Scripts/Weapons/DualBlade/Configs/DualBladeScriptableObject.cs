using UnityEngine;
using UnityEngine.Serialization;

namespace Burntwax
{
    /// <summary>
    /// Shared data for one blade pair. Scene models and hand availability
    /// belong to DualBladeStateMachine. Class and IDs preserve existing saves.
    /// </summary>
    [CreateAssetMenu(fileName = "Wakizashi", menuName = "DualBlade/Wakizashi", order = 0)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaScriptableObject")]
    public class DualBladeScriptableObject : ScriptableObject
    {
        [Tooltip("Stable save/load ID for this blade pair. Preserve existing IDs.")]
        public int ID;

        public const string DisplayName = "Wakizashi";

        [Header("Blade Pair")]
        [Tooltip("Use the assigned left model when the glove is not occupying that hand.")]
        public bool UseLeftBlade = true;

        [Header("Dash")]
        [Min(1)] public int MaxDashCharges = 3;
        [Min(0f)] public float DashSpeed = 20f;
        [Min(0.01f)] public float DashDuration = 0.2f;
        [Min(0.01f)] public float DashRechargeDuration = 2f;

        [Header("Shared Combat Configs")]
        [Tooltip("Damage per target per attack, shared by both blades, not damage per blade.")]
        public DualBladeDamageConfig DamageConfig;
        public DualBladeImpactConfig ImpactConfig;
        public DualBladeParryConfig ParryConfig;
        public DualBladeGestureConfig GestureConfig;

        [Header("Animation")]
        [Tooltip("Default profile for the pair. Existing clips can remain here during migration.")]
        public DualBladeAnimationConfig AnimationConfig;
        [Tooltip("Optional profile for right-only actions during glove use. Falls back to Animation Config.")]
        public DualBladeAnimationConfig RightBladeOnlyAnimationConfig;

        [Header("Feedback")]
        [FormerlySerializedAs("AudioConfig")]
        public DualBladeCombatFeedbackConfig FeedbackConfig;
        public DualBladeCrosshairConfig CrosshairConfig;

        public DualBladeAnimationConfig GetAnimationConfig(bool useBothBlades)
        {
            return !useBothBlades && RightBladeOnlyAnimationConfig != null
                ? RightBladeOnlyAnimationConfig : AnimationConfig;
        }
    }
}
