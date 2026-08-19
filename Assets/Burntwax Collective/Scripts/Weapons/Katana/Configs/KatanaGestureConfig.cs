using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Shared tuning for translating frame-rate-normalized look velocity into
    /// light and heavy katana attack intents.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Katana Gesture Config",
        menuName = "Katana/Gesture Config",
        order = 3)]
    public sealed class KatanaGestureConfig : ScriptableObject
    {
        [Header("Velocity Sampling")]
        [Tooltip("Seconds of recent mouse movement used to calculate velocity. A short window filters frame spikes without allowing slow movement to accumulate indefinitely.")]
        [SerializeField, Range(0.02f, 0.15f)]
        private float velocitySampleWindow = 0.05f;

        [Header("Initial Flick")]
        [Tooltip("Minimum processed look speed per unscaled second required to qualify an attack. Releasing after qualification performs a light slash.")]
        [SerializeField, Min(0f)] private float minimumInitialSlashSpeed = 60f;

        [Header("Heavy Reversal")]
        [Tooltip("Minimum processed look speed per unscaled second required for the held reversal to become a heavy slash.")]
        [SerializeField, Min(0f)] private float minimumHeavyReversalSpeed = 120f;
        [Tooltip("How directly the reversal must move away from the initial flick: 0 allows perpendicular; 1 requires exactly opposite.")]
        [SerializeField, Range(0f, 1f)] private float minimumReversalAlignment = 0.1f;

        public float VelocitySampleWindow => velocitySampleWindow;
        public float MinimumInitialSlashSpeed => minimumInitialSlashSpeed;
        public float MinimumHeavyReversalSpeed => minimumHeavyReversalSpeed;
        public float MinimumReversalAlignment => minimumReversalAlignment;

        private void OnValidate()
        {
            velocitySampleWindow = Mathf.Clamp(
                velocitySampleWindow,
                0.02f,
                0.15f);
            minimumInitialSlashSpeed = Mathf.Max(
                0f,
                minimumInitialSlashSpeed);
            minimumHeavyReversalSpeed = Mathf.Max(
                0f,
                minimumHeavyReversalSpeed);
            minimumReversalAlignment = Mathf.Clamp01(
                minimumReversalAlignment);
        }
    }
}
