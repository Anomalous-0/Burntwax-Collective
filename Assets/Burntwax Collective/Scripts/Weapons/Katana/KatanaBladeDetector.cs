using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Detects targets crossed by the curved blade during its active damage window
    /// and applies one slash-damage event to each target per window.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KatanaBladeDetector : MonoBehaviour
    {
        private const int MaximumResultsPerCheck = 64;

        [Header("References")]
        [SerializeField] private KatanaStateMachine stateMachine;
        [Tooltip("Ordered from the base of the cutting edge to its tip.")]
        [SerializeField] private Transform[] bladePoints;

        [Header("Detection")]
        [Min(0.001f)]
        [SerializeField] private float bladeRadius = 0.04f;
        [SerializeField] private LayerMask hittableLayers = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction =
            QueryTriggerInteraction.Collide;

        [Header("Debug")]
        [SerializeField] private bool logDetections = true;

        private readonly Collider[] overlapResults =
            new Collider[MaximumResultsPerCheck];
        private readonly RaycastHit[] sweepResults =
            new RaycastHit[MaximumResultsPerCheck];
        private readonly HashSet<GameObject> detectedTargets =
            new HashSet<GameObject>();

        private Vector3[] previousPointPositions;
        private bool wasDamageWindowActive;
        private Vector3 currentBladeCenter;
        private Vector3 currentSwingDirection;

        private void Awake()
        {
            if (stateMachine == null)
                stateMachine = GetComponentInParent<KatanaStateMachine>();

            EnsurePositionBuffer();
        }

        private void LateUpdate()
        {
            if (stateMachine == null || !HasEnoughBladePoints())
                return;

            EnsurePositionBuffer();

            if (!stateMachine.IsDamageWindowActive)
            {
                wasDamageWindowActive = false;
                return;
            }

            if (!wasDamageWindowActive)
            {
                detectedTargets.Clear();
                CaptureCurrentPositions();
                UpdateHitContext();
                CheckCurrentBladeSegments();
                wasDamageWindowActive = true;
                return;
            }

            UpdateHitContext();
            CheckCurrentBladeSegments();
            SweepBladePoints();
            SweepSegmentMidpoints();
            CaptureCurrentPositions();
        }

        private void CheckCurrentBladeSegments()
        {
            for (int i = 0; i < bladePoints.Length - 1; i++)
            {
                Transform pointA = bladePoints[i];
                Transform pointB = bladePoints[i + 1];
                if (pointA == null || pointB == null)
                    continue;

                int count = Physics.OverlapCapsuleNonAlloc(
                    pointA.position,
                    pointB.position,
                    bladeRadius,
                    overlapResults,
                    hittableLayers,
                    triggerInteraction);

                for (int resultIndex = 0; resultIndex < count; resultIndex++)
                    RegisterDetection(overlapResults[resultIndex]);
            }
        }

        private void SweepBladePoints()
        {
            for (int i = 0; i < bladePoints.Length; i++)
            {
                if (bladePoints[i] == null)
                    continue;

                SweepSphere(previousPointPositions[i], bladePoints[i].position);
            }
        }

        private void SweepSegmentMidpoints()
        {
            for (int i = 0; i < bladePoints.Length - 1; i++)
            {
                if (bladePoints[i] == null || bladePoints[i + 1] == null)
                    continue;

                Vector3 previousMidpoint =
                    (previousPointPositions[i] + previousPointPositions[i + 1]) * 0.5f;
                Vector3 currentMidpoint =
                    (bladePoints[i].position + bladePoints[i + 1].position) * 0.5f;
                SweepSphere(previousMidpoint, currentMidpoint);
            }
        }

        private void SweepSphere(Vector3 start, Vector3 end)
        {
            Vector3 movement = end - start;
            float distance = movement.magnitude;
            if (distance <= 0.0001f)
                return;

            int count = Physics.SphereCastNonAlloc(
                start,
                bladeRadius,
                movement / distance,
                sweepResults,
                distance,
                hittableLayers,
                triggerInteraction);

            for (int i = 0; i < count; i++)
                RegisterDetection(sweepResults[i].collider);
        }

        private void RegisterDetection(Collider detectedCollider)
        {
            if (detectedCollider == null)
                return;

            Transform ownerRoot = stateMachine.player != null
                ? stateMachine.player.transform.root
                : stateMachine.transform.root;
            if (detectedCollider.transform.root == ownerRoot)
                return;

            IDamageable damageable =
                detectedCollider.GetComponentInParent<IDamageable>();
            Component damageReceiver = damageable as Component;
            GameObject target = damageReceiver != null
                ? damageReceiver.gameObject
                : detectedCollider.attachedRigidbody != null
                    ? detectedCollider.attachedRigidbody.gameObject
                    : detectedCollider.transform.root.gameObject;

            if (!detectedTargets.Add(target))
                return;

            bool damageApplied = false;
            KatanaScriptableObject activeKatana = stateMachine.ActiveKatanaScriptable;
            KatanaDamageConfig damageConfig = activeKatana != null
                ? activeKatana.DamageConfig
                : null;

            if (damageConfig != null)
            {
                Vector3 hitPoint = detectedCollider.ClosestPoint(currentBladeCenter);
                Vector3 hitNormal = hitPoint - currentBladeCenter;
                if (hitNormal.sqrMagnitude <= Mathf.Epsilon)
                    hitNormal = -currentSwingDirection;

                GameObject instigator = stateMachine.player != null
                    ? stateMachine.player
                    : stateMachine.gameObject;
                Vector3 impulse = activeKatana.ImpactConfig != null
                    ? activeKatana.ImpactConfig.GetImpulse(
                        stateMachine.CurrentAttackStrength,
                        stateMachine.CurrentSlashDirection,
                        instigator.transform,
                        hitPoint)
                    : Vector3.zero;

                DamageInfo damage = new DamageInfo(
                    damageConfig.GetDamage(stateMachine.CurrentAttackStrength),
                    DamageType.Slash,
                    hitPoint,
                    hitNormal,
                    currentSwingDirection,
                    gameObject,
                    instigator,
                    impulse);

                damageApplied = HitResolver.ResolveHit(
                    detectedCollider,
                    damage);
            }

            if (logDetections)
            {
                Debug.Log(
                    $"[KatanaBladeDetector] Detected {target.name} "
                        + $"(damage applied: {damageApplied})",
                    target);
            }
        }

        private void UpdateHitContext()
        {
            Vector3 bladeCenter = Vector3.zero;
            Vector3 totalMovement = Vector3.zero;
            int validPointCount = 0;

            for (int i = 0; i < bladePoints.Length; i++)
            {
                if (bladePoints[i] == null)
                    continue;

                Vector3 currentPosition = bladePoints[i].position;
                bladeCenter += currentPosition;
                totalMovement += currentPosition - previousPointPositions[i];
                validPointCount++;
            }

            currentBladeCenter = validPointCount > 0
                ? bladeCenter / validPointCount
                : transform.position;

            if (totalMovement.sqrMagnitude > Mathf.Epsilon)
            {
                currentSwingDirection = totalMovement.normalized;
                return;
            }

            currentSwingDirection = stateMachine.player != null
                ? stateMachine.player.transform.forward
                : stateMachine.transform.forward;
        }

        private bool HasEnoughBladePoints()
        {
            return bladePoints != null && bladePoints.Length >= 2;
        }

        private void EnsurePositionBuffer()
        {
            int pointCount = bladePoints != null ? bladePoints.Length : 0;
            if (previousPointPositions == null
                || previousPointPositions.Length != pointCount)
            {
                previousPointPositions = new Vector3[pointCount];
            }
        }

        private void CaptureCurrentPositions()
        {
            for (int i = 0; i < bladePoints.Length; i++)
            {
                if (bladePoints[i] != null)
                    previousPointPositions[i] = bladePoints[i].position;
            }
        }

        private void OnDisable()
        {
            wasDamageWindowActive = false;
            detectedTargets.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (!HasEnoughBladePoints())
                return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < bladePoints.Length; i++)
            {
                if (bladePoints[i] == null)
                    continue;

                Gizmos.DrawWireSphere(bladePoints[i].position, bladeRadius);
                if (i < bladePoints.Length - 1 && bladePoints[i + 1] != null)
                    Gizmos.DrawLine(bladePoints[i].position, bladePoints[i + 1].position);
            }
        }
    }
}
