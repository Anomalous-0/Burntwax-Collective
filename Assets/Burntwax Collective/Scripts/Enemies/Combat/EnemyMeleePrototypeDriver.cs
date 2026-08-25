using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Temporary stationary decision driver for testing melee combat before NPC
    /// AI exists. It only selects a target, faces it, and requests attacks; the
    /// EnemyMeleeAttack controller owns every combat rule and phase.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMeleeAttack))]
    public sealed class EnemyMeleePrototypeDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyMeleeAttack meleeAttack;

        [Tooltip("Optional explicit target. When empty, the active PlayerHealth is found automatically.")]
        [SerializeField] private Transform target;

        [Tooltip("Transform rotated toward the target. Defaults to this enemy root.")]
        [SerializeField] private Transform rotationRoot;

        [Header("Attack Request")]
        [SerializeField, Min(0f)] private float attackRequestRange = 2f;

        [Tooltip("The driver waits until it is this closely aligned before requesting an attack.")]
        [SerializeField, Range(0f, 180f)] private float maximumStartAngle =
            15f;

        [Header("Facing")]
        [SerializeField, Min(0f)] private float turnSpeed = 360f;

        [Tooltip("Turning stops once the damage window opens, allowing the player to dodge the committed attack.")]
        [SerializeField] private bool faceTargetDuringWindup = true;

        [Header("Target Search")]
        [SerializeField, Min(0.1f)] private float targetSearchInterval = 1f;

        private float nextTargetSearchTime;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            nextTargetSearchTime = 0f;
            TryFindPlayerTarget();
        }

        private void Update()
        {
            if (target == null)
            {
                TryFindPlayerTarget();
                return;
            }

            if (ShouldFaceTarget())
                FaceTarget();

            TryRequestAttack();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void TryRequestAttack()
        {
            if (meleeAttack == null || !meleeAttack.CanStartAttack)
                return;

            Vector3 toTarget = target.position - GetRotationRoot().position;
            if (toTarget.sqrMagnitude >
                attackRequestRange * attackRequestRange)
            {
                return;
            }

            Vector3 flatDirection = Vector3.ProjectOnPlane(
                toTarget,
                Vector3.up);
            if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            float angle = Vector3.Angle(
                GetRotationRoot().forward,
                flatDirection);
            if (angle > maximumStartAngle)
                return;

            meleeAttack.TryBeginAttack();
        }

        private bool ShouldFaceTarget()
        {
            if (meleeAttack == null)
                return true;

            return meleeAttack.CurrentState == EnemyMeleeAttackState.Ready
                || faceTargetDuringWindup
                && meleeAttack.CurrentState ==
                    EnemyMeleeAttackState.Windup;
        }

        private void FaceTarget()
        {
            Transform root = GetRotationRoot();
            Vector3 flatDirection = Vector3.ProjectOnPlane(
                target.position - root.position,
                Vector3.up);
            if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(
                flatDirection.normalized,
                Vector3.up);
            root.rotation = Quaternion.RotateTowards(
                root.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }

        private void TryFindPlayerTarget()
        {
            if (target != null || Time.time < nextTargetSearchTime)
                return;

            nextTargetSearchTime = Time.time + targetSearchInterval;
            PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
                target = playerHealth.transform;
        }

        private Transform GetRotationRoot()
        {
            return rotationRoot != null
                ? rotationRoot
                : transform;
        }

        private void ResolveReferences()
        {
            if (meleeAttack == null)
                meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (rotationRoot == null)
                rotationRoot = transform;
        }

        private void OnValidate()
        {
            ResolveReferences();
            attackRequestRange = Mathf.Max(0f, attackRequestRange);
            maximumStartAngle = Mathf.Clamp(
                maximumStartAngle,
                0f,
                180f);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            targetSearchInterval = Mathf.Max(0.1f, targetSearchInterval);
        }

        private void OnDrawGizmosSelected()
        {
            Color previousColor = Gizmos.color;
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.8f);
            Gizmos.DrawWireSphere(
                GetRotationRoot().position,
                attackRequestRange);
            Gizmos.color = previousColor;
        }
    }
}
