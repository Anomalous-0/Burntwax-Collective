using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Burntwax
{
    /// <summary>
    /// Optional physical response to a resolved Sharp velocity impact.
    /// Damage, defense and surface effects remain owned by the existing hit
    /// pipeline. This component only attaches and releases the Rigidbody.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VelocityImpactDamageSource))]
    public sealed class SharpObjectEmbedding : MonoBehaviour
    {
        [Header("Eligibility")]
        [Tooltip("Layer of the struck collider. Nothing is eligible until layers are selected.")]
        [SerializeField] private LayerMask embeddableLayers;
        [Tooltip("Additional embedding gate. The velocity damage config's speed gate must also pass.")]
        [SerializeField, Min(0f)] private float minimumEmbedSpeed = 6f;

        [Header("Embedded Pose")]
        [Tooltip("Optional child at the cutting tip/edge. Its forward (+Z) is the penetration axis. Without this, use the actual collision contact and preserve orientation.")]
        [SerializeField] private Transform embeddingPoint;
        [Tooltip("Align the point's +Z axis to the incoming flight direction and keep its configured Up upright. Leave off to preserve incoming rotation.")]
        [FormerlySerializedAs("alignPointToSurface")]
        [SerializeField] private bool alignPointToThrow = true;
        [Tooltip("Up axis in the embedding point's local space. Usually (0,1,0). Controls roll around its unchanged +Z cutting axis. Must not be parallel to +Z.")]
        [SerializeField] private Vector3 localUpDirection = Vector3.up;
        [SerializeField, Min(0f)] private float penetrationDepth = 0.03f;

        [Header("Release")]
        [SerializeField, Min(0f)] private float releaseClearance = 0.02f;
        [Tooltip("Briefly prevent re-embedding after being pulled free.")]
        [SerializeField, Min(0f)] private float reembedDelay = 0.15f;

        public bool IsEmbedded { get; private set; }

        private Rigidbody body;
        private VelocityImpactDamageSource damageSource;
        private Breakable ownBreakable;
        private Collider targetCollider;
        private Transform target;
        private Breakable targetBreakable;
        private Vector3 localPosition;
        private Quaternion localRotation;
        private Vector3 localOutwardNormal;
        private Vector3 lastOutwardNormal;
        private float embeddedDepth;
        private bool previousUseGravity;
        private bool previousIsKinematic;
        private RigidbodyInterpolation previousInterpolation;
        private CollisionDetectionMode previousCollisionMode;
        private float canEmbedAfter;
        private GloveStateMachine controllingGlove;
        private readonly List<(Collider source, Collider target)> ignoredPairs = new();

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            damageSource = GetComponent<VelocityImpactDamageSource>();
            ownBreakable = GetComponent<Breakable>();
        }

        private void OnEnable()
        {
            damageSource.ImpactResolved += OnImpactResolved;
        }

        private void OnDisable()
        {
            if (damageSource != null)
                damageSource.ImpactResolved -= OnImpactResolved;
            Detach();
            controllingGlove = null;
        }

        private void OnImpactResolved(RigidbodyImpact impact, HitDefenseResult defense)
        {
            if (!isActiveAndEnabled || IsEmbedded || body == null || body.isKinematic
                || damageSource.Behavior != VelocityImpactBehavior.Sharp
                || Time.time < canEmbedAfter
                || (ownBreakable != null && ownBreakable.IsBroken)
                || (controllingGlove != null && controllingGlove.heldObject == gameObject))
                return;

            // A blocked or returned object belongs to the defense response.
            // GuardBroken may continue as a normal penetrating impact.
            if (defense.Outcome == HitDefenseOutcome.Blocked
                || defense.Outcome == HitDefenseOutcome.Parried
                || defense.SourceMotionHandled)
                return;

            Collider hit = impact.HitCollider;
            if (hit == null || !hit.enabled || !hit.gameObject.activeInHierarchy
                || hit.isTrigger || hit.attachedRigidbody == body
                || (embeddableLayers.value & (1 << hit.gameObject.layer)) == 0
                || impact.SourceClosingSpeed < minimumEmbedSpeed
                || impact.Normal.sqrMagnitude < 0.0001f)
                return;

            // A breakable may have scheduled Destroy(), which is deferred to
            // the end of the frame. Check its explicit state as well as null.
            Breakable breakable = hit.GetComponentInParent<Breakable>();
            if (breakable != null && breakable.IsBroken)
                return;

            Embed(impact, breakable);
        }

        private void Embed(RigidbodyImpact impact, Breakable breakable)
        {
            // Guidance must restore its own settings BEFORE we capture ours.
            GetComponent<CurvedRigidbodyFlight>()?.CancelFlight();
            previousUseGravity = body.useGravity;
            previousIsKinematic = body.isKinematic;
            previousInterpolation = body.interpolation;
            previousCollisionMode = body.collisionDetectionMode;

            Vector3 outward = impact.Normal.normalized;
            Quaternion rotation = transform.rotation;
            Vector3 contactOffset = impact.Point - transform.position;
            if (embeddingPoint != null && embeddingPoint.IsChildOf(transform))
            {
                contactOffset = embeddingPoint.position - transform.position;
                if (alignPointToThrow)
                {
                    // Use velocity captured BEFORE the collision, not the
                    // post-collision rebound or the wall's normal. Pitch/yaw
                    // therefore match the actual approach, including arcs.
                    Vector3 incoming = impact.SourceVelocity.sqrMagnitude > 0.0001f
                        ? impact.SourceVelocity.normalized : -outward;
                    Vector3 desiredUp = Vector3.ProjectOnPlane(Vector3.up, incoming);
                    // Straight up/down has no unique upright roll. Use a
                    // consistent world axis instead of the object's random spin.
                    if (desiredUp.sqrMagnitude < 0.0001f)
                        desiredUp = Vector3.ProjectOnPlane(Vector3.forward, incoming);

                    Vector3 pointUp = Vector3.ProjectOnPlane(localUpDirection, Vector3.forward);
                    if (pointUp.sqrMagnitude < 0.0001f)
                        pointUp = Vector3.up;

                    Quaternion sourceFrame = Quaternion.LookRotation(
                        embeddingPoint.forward, embeddingPoint.TransformDirection(pointUp));
                    Quaternion destinationFrame = Quaternion.LookRotation(incoming, desiredUp);
                    Quaternion correction = destinationFrame * Quaternion.Inverse(sourceFrame);
                    rotation = correction * rotation;
                    contactOffset = correction * contactOffset;
                }
            }

            embeddedDepth = Mathf.Max(0f, penetrationDepth);
            Vector3 position = impact.Point - outward * embeddedDepth - contactOffset;
            targetCollider = impact.HitCollider;
            target = targetCollider.transform;
            targetBreakable = breakable;
            localPosition = target.InverseTransformPoint(position);
            localRotation = Quaternion.Inverse(target.rotation) * rotation;
            localOutwardNormal = target.InverseTransformDirection(outward);
            lastOutwardNormal = outward;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            IsEmbedded = true;
            damageSource.Disarm();
            IgnoreTargetCollisions();
            FollowTarget();
        }

        private void IgnoreTargetCollisions()
        {
            Collider[] sourceColliders = GetComponentsInChildren<Collider>();
            Rigidbody targetBody = targetCollider.attachedRigidbody;
            Collider[] targetColliders = targetBody != null
                ? targetBody.GetComponentsInChildren<Collider>()
                : new[] { targetCollider };

            foreach (Collider source in sourceColliders)
            {
                if (source.attachedRigidbody != body)
                    continue;
                foreach (Collider other in targetColliders)
                {
                    if (other == source || other.attachedRigidbody != targetBody
                        || Physics.GetIgnoreCollision(source, other))
                        continue;
                    Physics.IgnoreCollision(source, other, true);
                    ignoredPairs.Add((source, other));
                }
            }
        }

        private void FixedUpdate()
        {
            UpdateAttachment();
        }

        private void LateUpdate()
        {
            // Also follow animated collider/bone transforms after animation.
            UpdateAttachment();
        }

        private void UpdateAttachment()
        {
            if (!IsEmbedded)
                return;
            if (target == null || targetCollider == null || !targetCollider.enabled
                || !target.gameObject.activeInHierarchy
                || (targetBreakable != null && targetBreakable.IsBroken)
                || (ownBreakable != null && ownBreakable.IsBroken))
            {
                Detach();
                return;
            }
            FollowTarget();
        }

        private void FollowTarget()
        {
            // Keep the original hierarchy: parent-based damage lookup and
            // glove hold-parent restoration must still find this object.
            body.position = target.TransformPoint(localPosition);
            body.rotation = target.rotation * localRotation;
            lastOutwardNormal = target.TransformDirection(localOutwardNormal).normalized;
        }

        /// <summary>
        /// Release before the glove records gravity/interpolation settings.
        /// Suppress embedding while this glove still owns attraction/hold.
        /// </summary>
        public void PrepareForAttraction(GloveStateMachine glove)
        {
            Detach();
            controllingGlove = glove;
        }

        public void Detach()
        {
            if (!IsEmbedded)
                return;

            if (body != null)
            {
                body.position += lastOutwardNormal * (embeddedDepth + releaseClearance);
                body.isKinematic = previousIsKinematic;
                body.useGravity = previousUseGravity;
                body.interpolation = previousInterpolation;
                body.collisionDetectionMode = previousCollisionMode;
                // Never resurrect the pre-impact throw velocity on release.
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.WakeUp();
                }
            }

            foreach (var pair in ignoredPairs)
            {
                if (pair.source != null && pair.target != null)
                    Physics.IgnoreCollision(pair.source, pair.target, false);
            }
            ignoredPairs.Clear();
            IsEmbedded = false;
            target = null;
            targetCollider = null;
            targetBreakable = null;
            canEmbedAfter = Time.time + Mathf.Max(0f, reembedDelay);
        }
    }
}
