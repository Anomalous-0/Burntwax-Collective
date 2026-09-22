using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Authored rules for selecting and presenting one enemy defense action.
    /// EnemyDefenseConfig owns hit-response rules; this asset owns action
    /// selection, animation, and optional prototype timing.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Enemy Defense Action Config",
        menuName = "Enemies/Combat/Defense Action Config",
        order = 2)]
    public sealed class EnemyDefenseActionConfig :
        EnemyCombatActionConfig
    {
        public override EnemyCombatActionCategory Category =>
            EnemyCombatActionCategory.Defense;

        protected override EnemyCapability ImplicitRequiredCapabilities =>
            EnemyCapability.Blocking;

        [Header("Defense Rules")]
        [Tooltip("Block and perfect-parry behavior used while this action is active.")]
        [SerializeField] private EnemyDefenseConfig defenseConfig;

        [Tooltip("Keep turning toward the current target throughout the defense action.")]
        [SerializeField] private bool faceTargetWhileDefending = true;

        [Header("Fallback Timing")]
        [Tooltip("Prototype mode. Enable until animation events drive the parry window and finish the action.")]
        [SerializeField] private bool useFallbackTiming = true;

        [Tooltip("Total fallback duration of the defense action.")]
        [SerializeField, Min(0.01f)] private float defenseDuration = 0.7f;

        [Tooltip("Seconds after entering defense before its perfect-parry window opens.")]
        [SerializeField, Min(0f)] private float parryWindowStart = 0.12f;

        [Tooltip("Fallback perfect-parry duration. Set to 0 for a block-only defense.")]
        [SerializeField, Min(0f)] private float parryWindowDuration = 0.14f;

        public EnemyDefenseConfig DefenseConfig => defenseConfig;
        public bool FaceTargetWhileDefending => faceTargetWhileDefending;
        public bool UseFallbackTiming => useFallbackTiming;
        public float DefenseDuration => defenseDuration;
        public float ParryWindowStart => parryWindowStart;
        public float ParryWindowDuration => parryWindowDuration;

        protected override void OnValidate()
        {
            base.OnValidate();
            defenseDuration = Mathf.Max(0.01f, defenseDuration);
            parryWindowStart = Mathf.Clamp(
                parryWindowStart,
                0f,
                defenseDuration);
            parryWindowDuration = Mathf.Clamp(
                parryWindowDuration,
                0f,
                defenseDuration - parryWindowStart);
        }
    }
}
