using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Identifies the point targeted by reflected objects and other precision
    /// combat actions. Non-humanoid actors can assign any appropriate point.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatAimTarget : MonoBehaviour
    {
        [Tooltip("Preferred target point, normally the actor's head or weak point.")]
        [SerializeField] private Transform aimPoint;

        [Tooltip("When Aim Point is empty, use the Head bone from a humanoid Animator if available.")]
        [SerializeField] private bool useHumanoidHeadFallback = true;

        [SerializeField] private Animator humanoidAnimator;

        public Transform AimPoint => ResolveAimPoint();

        public bool TryGetPosition(out Vector3 position)
        {
            Transform resolvedPoint = ResolveAimPoint();
            if (resolvedPoint == null)
            {
                position = default;
                return false;
            }

            position = resolvedPoint.position;
            return true;
        }

        private Transform ResolveAimPoint()
        {
            if (aimPoint != null)
                return aimPoint;

            if (!useHumanoidHeadFallback)
                return null;

            if (humanoidAnimator == null)
                humanoidAnimator = GetComponentInChildren<Animator>();

            if (humanoidAnimator == null || !humanoidAnimator.isHuman)
                return null;

            return humanoidAnimator.GetBoneTransform(HumanBodyBones.Head);
        }

        private void OnDrawGizmosSelected()
        {
            Transform resolvedPoint = ResolveAimPoint();
            if (resolvedPoint == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(resolvedPoint.position, 0.12f);
        }
    }
}
