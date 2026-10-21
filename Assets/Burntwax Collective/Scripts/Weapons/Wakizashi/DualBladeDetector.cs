using UnityEngine;

namespace Burntwax
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaBladeHand")]
    public enum DualBladeHand { Right, Left }

    /// <summary>
    /// Detects targets crossed by the curved blade during its active damage window
    /// Both blades share their owner's target list: one damage event per attack.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaBladeDetector")]
    public sealed class DualBladeDetector : MonoBehaviour, IParryReactionReceiver
    {
        private const int MaximumResultsPerCheck = 64;

        [Header("References")]
        [SerializeField] private DualBladeStateMachine stateMachine;
        [SerializeField] private DualBladeHand hand;
        public DualBladeStateMachine Owner => stateMachine;
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

        private GloveStateMachine gloveStateMachine;

        private readonly Collider[] overlapResults =
            new Collider[MaximumResultsPerCheck];
        private readonly RaycastHit[] sweepResults =
            new RaycastHit[MaximumResultsPerCheck];

        private Vector3[] previousPointPositions;
        private bool wasDamageWindowActive;
        private uint observedAttackSequence;
        private Vector3 currentBladeCenter;
        private Vector3 currentSwingDirection;

        public void Configure(DualBladeStateMachine owner, DualBladeHand bladeHand)
        {
            stateMachine = owner;
            hand = bladeHand;
            gloveStateMachine = owner.GetComponent<GloveStateMachine>();
            wasDamageWindowActive = false;
        }

        // A blade may live under a hand socket instead of the state-machine root.
        // Route its parry reaction to the shared attack regardless of hierarchy.
        public void OnAttackParried(DamageInfo parriedHit, Collider defenderCollider)
        {
            stateMachine?.OnAttackParried(parriedHit, defenderCollider);
        }

        private void Awake()
        {
            if (stateMachine == null)
                stateMachine = GetComponentInParent<DualBladeStateMachine>();

            if (stateMachine != null)
                gloveStateMachine = stateMachine.GetComponent<GloveStateMachine>();

            EnsurePositionBuffer();
        }

        private void LateUpdate()
        {
            if (stateMachine == null || !HasEnoughBladePoints())
                return;

            EnsurePositionBuffer();

            if (!stateMachine.CanBladeDealDamage(hand))
            {
                wasDamageWindowActive = false;
                return;
            }

            if (!wasDamageWindowActive || observedAttackSequence != stateMachine.AttackSequence)
            {
                observedAttackSequence = stateMachine.AttackSequence;
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

            // A parry can close the window during this detector pass. Stop
            // processing immediately so the canceled swing cannot hit another
            // collider later in the same frame.
            if (!stateMachine.CanBladeDealDamage(hand))
                return;

            Transform ownerRoot = stateMachine.player != null
                ? stateMachine.player.transform.root
                : stateMachine.transform.root;
            if (detectedCollider.transform.root == ownerRoot)
                return;

            // The glove owns the motion of its primary attracted/held object.
            // Ignore it completely until the glove releases or launches it so
            // an ordinary slash cannot damage the player's prepared projectile.
            if (gloveStateMachine == null && stateMachine != null)
                gloveStateMachine = stateMachine.GetComponent<GloveStateMachine>();

            if (gloveStateMachine != null
                && gloveStateMachine.OwnsControlledObject(detectedCollider))
            {
                return;
            }

            IDamageable damageable =
                detectedCollider.GetComponentInParent<IDamageable>();
            Component damageReceiver = damageable as Component;
            GameObject target = damageReceiver != null
                ? damageReceiver.gameObject
                : detectedCollider.attachedRigidbody != null
                    ? detectedCollider.attachedRigidbody.gameObject
                    : detectedCollider.transform.root.gameObject;

            if (!stateMachine.TryRegisterBladeHit(target))
                return;

            bool damageApplied = false;
            DualBladeScriptableObject activeDualBlade = stateMachine.ActiveDualBladeScriptable;
            DualBladeDamageConfig damageConfig = activeDualBlade != null
                ? activeDualBlade.DamageConfig
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
                Vector3 impulse = activeDualBlade.ImpactConfig != null
                    ? activeDualBlade.ImpactConfig.GetImpulse(
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
                    $"[DualBladeDetector] Detected {target.name} "
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
