using UnityEngine;

namespace Burntwax
{
    [CreateAssetMenu(
        fileName = "Mantle Config",
        menuName = "Player/Movement/Mantle Config")]
    public class PlayerMantleConfig : ScriptableObject
    {
        [Header("Detection Layers")]
        public LayerMask WallLayers;
        public LayerMask TopSurfaceLayers;
        public LayerMask ClearanceBlockingLayers;

        [Header("Detection")]
        [Min(0.01f)] public float ForwardCheckDistance = 0.8f;
        [Range(0f, 1f)] public float ChestHeightNormalized = 0.65f;
        [Range(0.1f, 1f)] public float WallProbeRadiusScale = 0.65f;
        [Min(0f)] public float MinimumMantleHeight = 0.4f;
        [Min(0.01f)] public float MaximumMantleHeight = 1.5f;
        [Min(0.01f)] public float LandingDepth = 0.45f;
        [Range(0f, 1f)] public float MinimumTopUpDot = 0.7f;
        [Min(0f)] public float ClearanceSkin = 0.03f;

        [Header("Movement")]
        [Min(0.01f)] public float MantleDuration = 0.3f;
        [Min(0f)] public float MantleStartCurveHeight = 0.5f;
        [Min(0f)] public float MantleEndCurveHeight = 0.25f;

        [Header("Debug")]
        public bool DrawDebugChecks = true;
    }

    public readonly struct PlayerMantleTarget
    {
        public readonly Vector3 RigidbodyPosition;
        public readonly RaycastHit WallHit;
        public readonly RaycastHit TopHit;

        public PlayerMantleTarget(
            Vector3 rigidbodyPosition,
            RaycastHit wallHit,
            RaycastHit topHit)
        {
            RigidbodyPosition = rigidbodyPosition;
            WallHit = wallHit;
            TopHit = topHit;
        }
    }
}
