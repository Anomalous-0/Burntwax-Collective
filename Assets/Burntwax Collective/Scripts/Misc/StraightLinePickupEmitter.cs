using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Continuously launches temporary pickup prefabs in a straight line.
    /// Intended for testing interactions such as katana parries.
    /// </summary>
    public sealed class StraightLinePickupEmitter : MonoBehaviour
    {
        [Header("Pickup")]
        [Tooltip("Pickup prefab to launch. Its root must have a Rigidbody.")]
        [SerializeField] private GameObject pickupPrefab;

        [Tooltip("Optional launch origin and direction. Uses this transform when unassigned.")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Optional actor credited with throwing each pickup. When empty, the nearest parent CombatAimTarget is used.")]
        [SerializeField] private GameObject instigator;

        [Header("Optional Curved Throw")]
        [Tooltip("When assigned, pickups follow a curved flight toward this Transform's position captured at spawn time. Leave empty for a straight launch.")]
        [SerializeField] private Transform curvedTarget;

        [Tooltip("Arc height added per metre between the emitter and the captured target position.")]
        [SerializeField, Min(0f)]
        private float arcHeightPerMeter = 0.12f;

        [SerializeField, Min(0f)] private float minimumArcHeight = 0.4f;
        [SerializeField, Min(0f)] private float maximumArcHeight = 2.5f;

        [Header("Emission")]
        [Tooltip("Seconds between spawned pickups.")]
        [SerializeField, Min(0.02f)] private float spawnInterval = 0.5f;

        [Tooltip("Initial speed along the spawn point's forward direction.")]
        [SerializeField, Min(0f)] private float launchSpeed = 12f;

        [Header("Launch Spin")]
        [Tooltip("Minimum random angular speed in radians per second. Heavier objects receive proportionally less spin.")]
        [SerializeField, Min(0f)] private float minimumLaunchSpin = 1f;

        [Tooltip("Maximum random angular speed in radians per second. Heavier objects receive proportionally less spin.")]
        [SerializeField, Min(0f)] private float maximumLaunchSpin = 4f;

        [Tooltip("Seconds before each spawned pickup is destroyed.")]
        [SerializeField, Min(0.1f)] private float lifetime = 5f;

        [Tooltip("Emit once immediately when this component becomes active.")]
        [SerializeField] private bool spawnImmediately = true;

        private float nextSpawnTime;
        private bool missingRigidbodyReported;

        private void OnEnable()
        {
            nextSpawnTime = spawnImmediately
                ? Time.time
                : Time.time + spawnInterval;
        }

        private void Update()
        {
            if (pickupPrefab == null || Time.time < nextSpawnTime)
                return;

            EmitPickup();
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void EmitPickup()
        {
            Transform origin = spawnPoint != null
                ? spawnPoint
                : transform;

            GameObject spawnedPickup = Instantiate(
                pickupPrefab,
                origin.position,
                origin.rotation);

            if (spawnedPickup.TryGetComponent(out Rigidbody body))
            {
                if (curvedTarget != null)
                {
                    Vector3 capturedTargetPosition = curvedTarget.position;
                    float targetDistance = Vector3.Distance(
                        body.worldCenterOfMass,
                        capturedTargetPosition);
                    float arcHeight = Mathf.Clamp(
                        targetDistance * arcHeightPerMeter,
                        minimumArcHeight,
                        Mathf.Max(minimumArcHeight, maximumArcHeight));

                    CurvedRigidbodyFlight.Launch(
                        body,
                        capturedTargetPosition,
                        launchSpeed,
                        arcHeight);
                }
                else
                {
                    body.linearVelocity = origin.forward * launchSpeed;
                }

                RigidbodySpinUtility.ApplyMassWeightedRandomSpin(
                    body,
                    minimumLaunchSpin,
                    maximumLaunchSpin);

                VelocityImpactDamageSource damageSource =
                    spawnedPickup.GetComponent<VelocityImpactDamageSource>();
                GameObject resolvedInstigator = ResolveInstigator();
                if (damageSource != null && resolvedInstigator != null)
                    damageSource.Arm(resolvedInstigator);
            }
            else if (!missingRigidbodyReported)
            {
                missingRigidbodyReported = true;
                Debug.LogWarning(
                    $"[{nameof(StraightLinePickupEmitter)}] Pickup prefab "
                    + $"'{pickupPrefab.name}' has no Rigidbody on its root.",
                    this);
            }

            Destroy(spawnedPickup, lifetime);
        }

        private GameObject ResolveInstigator()
        {
            if (instigator != null)
                return instigator;

            CombatAimTarget aimTarget =
                GetComponentInParent<CombatAimTarget>();
            return aimTarget != null
                ? aimTarget.gameObject
                : null;
        }

        private void OnValidate()
        {
            spawnInterval = Mathf.Max(0.02f, spawnInterval);
            launchSpeed = Mathf.Max(0f, launchSpeed);
            minimumLaunchSpin = Mathf.Max(0f, minimumLaunchSpin);
            maximumLaunchSpin = Mathf.Max(
                minimumLaunchSpin,
                maximumLaunchSpin);
            lifetime = Mathf.Max(0.1f, lifetime);
            arcHeightPerMeter = Mathf.Max(0f, arcHeightPerMeter);
            minimumArcHeight = Mathf.Max(0f, minimumArcHeight);
            maximumArcHeight = Mathf.Max(
                minimumArcHeight,
                maximumArcHeight);
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = spawnPoint != null
                ? spawnPoint
                : transform;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(origin.position, origin.forward * 2f);

            if (curvedTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin.position, curvedTarget.position);
                Gizmos.DrawWireSphere(curvedTarget.position, 0.12f);
            }
        }
    }
}
