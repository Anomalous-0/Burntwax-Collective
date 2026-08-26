using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Passive executor for enemy-thrown Rigidbody projectiles. It never makes
    /// combat decisions or emits on its own; EnemyThrowState supplies the
    /// selected loadout config and current target for each launch.
    /// </summary>
    public sealed class StraightLinePickupEmitter : MonoBehaviour
    {
        [Header("Launch References")]
        [Tooltip("Optional launch origin. Uses this transform when unassigned.")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Optional actor credited with each throw. When empty, the state-machine actor or nearest parent CombatAimTarget is used.")]
        [SerializeField] private GameObject instigator;

        private bool missingRigidbodyReported;

        public bool TryEmit(
            EnemyProjectileThrowConfig config,
            Transform target,
            GameObject requestedInstigator = null)
        {
            if (config == null || config.ProjectilePrefab == null)
                return false;

            Transform origin = spawnPoint != null
                ? spawnPoint
                : transform;
            Vector3 launchDirection = GetLaunchDirection(origin, target);
            Quaternion launchRotation = launchDirection.sqrMagnitude
                > Mathf.Epsilon
                    ? Quaternion.LookRotation(launchDirection, origin.up)
                    : origin.rotation;

            GameObject spawnedProjectile = Instantiate(
                config.ProjectilePrefab,
                origin.position,
                launchRotation);

            if (spawnedProjectile.TryGetComponent(out Rigidbody body))
            {
                LaunchBody(body, target, launchDirection, config);
                RigidbodySpinUtility.ApplyMassWeightedRandomSpin(
                    body,
                    config.MinimumLaunchSpin,
                    config.MaximumLaunchSpin);

                VelocityImpactDamageSource damageSource =
                    spawnedProjectile.GetComponent<
                        VelocityImpactDamageSource>();
                GameObject resolvedInstigator = ResolveInstigator(
                    requestedInstigator);
                if (damageSource != null && resolvedInstigator != null)
                    damageSource.Arm(resolvedInstigator);
            }
            else if (!missingRigidbodyReported)
            {
                missingRigidbodyReported = true;
                Debug.LogWarning(
                    $"[{nameof(StraightLinePickupEmitter)}] Projectile "
                    + $"prefab '{config.ProjectilePrefab.name}' has no "
                    + "Rigidbody on its root.",
                    this);
            }

            Destroy(spawnedProjectile, config.Lifetime);
            return true;
        }

        private static void LaunchBody(
            Rigidbody body,
            Transform target,
            Vector3 launchDirection,
            EnemyProjectileThrowConfig config)
        {
            if (target != null && config.UseCurvedFlight)
            {
                Vector3 capturedTargetPosition = target.position;
                float targetDistance = Vector3.Distance(
                    body.worldCenterOfMass,
                    capturedTargetPosition);
                float arcHeight = Mathf.Clamp(
                    targetDistance * config.ArcHeightPerMeter,
                    config.MinimumArcHeight,
                    Mathf.Max(
                        config.MinimumArcHeight,
                        config.MaximumArcHeight));

                if (CurvedRigidbodyFlight.Launch(
                    body,
                    capturedTargetPosition,
                    config.LaunchSpeed,
                    arcHeight))
                {
                    return;
                }
            }

            body.linearVelocity = launchDirection * config.LaunchSpeed;
        }

        private static Vector3 GetLaunchDirection(
            Transform origin,
            Transform target)
        {
            if (target != null)
            {
                Vector3 toTarget = target.position - origin.position;
                if (toTarget.sqrMagnitude > Mathf.Epsilon)
                    return toTarget.normalized;
            }

            return origin.forward.sqrMagnitude > Mathf.Epsilon
                ? origin.forward.normalized
                : Vector3.forward;
        }

        private GameObject ResolveInstigator(GameObject requestedInstigator)
        {
            if (instigator != null)
                return instigator;

            if (requestedInstigator != null)
                return requestedInstigator;

            CombatAimTarget aimTarget =
                GetComponentInParent<CombatAimTarget>();
            return aimTarget != null
                ? aimTarget.gameObject
                : gameObject;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = spawnPoint != null
                ? spawnPoint
                : transform;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(origin.position, origin.forward * 2f);
        }
    }
}
