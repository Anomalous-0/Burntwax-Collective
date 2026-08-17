using UnityEngine;

namespace Burntwax
{
    public enum GloveHoldPose
    {
        Small,
        Large
    }

    /// <summary>
    /// Per-prefab authoring data for how an attractable object should be held.
    /// Objects without this component retain the glove's default hold behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GloveHoldProfile : MonoBehaviour
    {
        [Header("Pose")]
        [SerializeField] private GloveHoldPose holdPose = GloveHoldPose.Small;

        [Header("Placement Relative To Hold Point")]
        [Tooltip("Local-space position adjustment used while this object is held.")]
        [SerializeField] private Vector3 localPositionOffset;

        [Tooltip("When enabled, the authored local rotation replaces the orientation captured when the object is grabbed.")]
        [SerializeField] private bool overrideRotation;

        [Tooltip("Euler rotation relative to the glove hold point when Override Rotation is enabled.")]
        [SerializeField] private Vector3 localHeldRotation;

        [Header("Suspended Motion")]
        [Tooltip("Multiplies the glove's normal hover spin. Set to 0 to keep the authored rotation locked.")]
        [SerializeField, Min(0f)] private float hoverSpinMultiplier = 1f;

        public GloveHoldPose HoldPose => holdPose;
        public Vector3 LocalPositionOffset => localPositionOffset;
        public bool OverrideRotation => overrideRotation;
        public Quaternion LocalHeldRotation => Quaternion.Euler(localHeldRotation);
        public float HoverSpinMultiplier => hoverSpinMultiplier;

        private void OnValidate()
        {
            hoverSpinMultiplier = Mathf.Max(0f, hoverSpinMultiplier);
        }
    }
}
