using System;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Facts measured from one Rigidbody collision. Consumers decide whether those facts
    /// should remain presentation-only or become gameplay damage.
    /// </summary>
    public sealed class RigidbodyImpact
    {
        public Collider HitCollider { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public Vector3 Direction { get; }
        public Vector3 SourceVelocity { get; }
        public float SourceClosingSpeed { get; }
        public float ClosingSpeed { get; }
        public bool PresentationHandled { get; private set; }

        public RigidbodyImpact(
            Collider hitCollider,
            Vector3 point,
            Vector3 normal,
            Vector3 direction,
            Vector3 sourceVelocity,
            float sourceClosingSpeed,
            float closingSpeed)
        {
            HitCollider = hitCollider;
            Point = point;
            Normal = normal;
            Direction = direction;
            SourceVelocity = sourceVelocity;
            SourceClosingSpeed = Mathf.Max(0f, sourceClosingSpeed);
            ClosingSpeed = Mathf.Max(0f, closingSpeed);
        }

        public void MarkPresentationHandled()
        {
            PresentationHandled = true;
        }
    }

    /// <summary>
    /// Reports solid collisions from an ordinary gameplay Rigidbody. Every qualifying contact
    /// can make an Impact surface sound, while optional consumers such as
    /// <see cref="VelocityImpactDamageSource"/> can independently turn the same event into damage.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodyImpactReporter : MonoBehaviour
    {
        [Header("Passive impact audio")]
        [Tooltip("Closing speed below which the collision stays silent.")]
        [SerializeField, Min(0f)] private float minimumAudioSpeed = .5f;

        [Tooltip("Closing speed at which audio switches from Minor Impact to the regular combat Impact category.")]
        [SerializeField, Min(0f)] private float majorImpactAudioSpeed = 7f;

        [Tooltip("Minimum time between sounds from this Rigidbody. Prevents rapid bounces and compound colliders from spamming audio.")]
        [SerializeField, Min(0f)] private float audioCooldown = 0.1f;

        public event Action<RigidbodyImpact> ImpactReported;

        private Rigidbody body;
        private Vector3 velocityBeforePhysics;
        private float nextAudioTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (body != null)
                velocityBeforePhysics = body.linearVelocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.contactCount == 0)
                return;

            Collider hitCollider = collision.collider;
            if (hitCollider == null)
                return;

            ContactPoint contact = collision.GetContact(0);

            // Only movement into the surface counts, so a fast sideways scrape does not sound
            // like a hard collision or qualify as velocity damage.
            float closingSpeed = Mathf.Abs(Vector3.Dot(
                collision.relativeVelocity,
                contact.normal));

            // Gameplay damage belongs to this Rigidbody only when this body
            // was itself moving into the contact. Relative speed remains the
            // correct measurement for passive collision presentation.
            float sourceClosingSpeed = Mathf.Max(
                0f,
                -Vector3.Dot(velocityBeforePhysics, contact.normal));

            Vector3 incomingDirection = velocityBeforePhysics.sqrMagnitude > Mathf.Epsilon
                ? velocityBeforePhysics.normalized
                : -contact.normal;

            RigidbodyImpact impact = new RigidbodyImpact(
                hitCollider,
                contact.point,
                contact.normal,
                incomingDirection,
                velocityBeforePhysics,
                sourceClosingSpeed,
                closingSpeed);

            // Give a gameplay or specialized source first ownership of the collision. It can
            // resolve damage and select presentation without the reporter duplicating effects.
            ImpactReported?.Invoke(impact);

            if (impact.PresentationHandled)
            {
                nextAudioTime = Time.time + audioCooldown;
                return;
            }

            if (closingSpeed >= minimumAudioSpeed && Time.time >= nextAudioTime)
            {
                if (closingSpeed >= majorImpactAudioSpeed)
                {
                    DamageInfo presentation = new DamageInfo(
                        0f,
                        DamageType.Impact,
                        contact.point,
                        contact.normal,
                        incomingDirection,
                        gameObject,
                        null);

                    SurfaceImpactSystem.Instance?.PlayImpact(
                        hitCollider,
                        presentation);
                }
                else
                {
                    DamageInfo presentation = new DamageInfo(
                        0f,
                        DamageType.MinorImpact,
                        contact.point,
                        contact.normal,
                        incomingDirection,
                        gameObject,
                        null);

                    // Minor impacts are presentation-only, but use the same
                    // surface route so their profiles may contain optional VFX.
                    SurfaceImpactSystem.Instance?.PlayImpact(
                        hitCollider,
                        presentation);
                }

                nextAudioTime = Time.time + audioCooldown;
            }
        }

        private void OnValidate()
        {
            minimumAudioSpeed = Mathf.Max(0f, minimumAudioSpeed);
            majorImpactAudioSpeed = Mathf.Max(
                minimumAudioSpeed,
                majorImpactAudioSpeed);
            audioCooldown = Mathf.Max(0f, audioCooldown);
        }
    }
}
