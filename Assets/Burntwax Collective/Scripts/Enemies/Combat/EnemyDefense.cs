using System;
using UnityEngine;

namespace Burntwax
{
    public enum EnemyDefenseMode
    {
        Inactive = 0,
        Guarding = 1,
        PerfectParryWindow = 2
    }

    /// <summary>
    /// Gives one enemy the capacity to block and parry through the universal
    /// hit pipeline. It does not decide when to defend; enemy action states and
    /// animation events control its small runtime mode explicitly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyDefense : MonoBehaviour, IHitDefense
    {
        [Header("References")]
        [Tooltip("Fallback rules used when a future defense action does not supply an override config.")]
        [SerializeField] private EnemyDefenseConfig defaultConfig;

        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyRuntimeProfile runtimeProfile;

        [Tooltip("Forward direction used by the defense arc. Defaults to the enemy root.")]
        [SerializeField] private Transform facingReference;

        [Header("Debug (read-only)")]
        [SerializeField] private EnemyDefenseMode currentMode;
        [SerializeField] private HitDefenseOutcome lastOutcome =
            HitDefenseOutcome.Passed;
        [SerializeField] private DamageType lastIncomingTypes =
            DamageType.None;
        [SerializeField] private float lastIncomingAngle;

        private EnemyDefenseConfig activeConfig;

        public event Action<DamageInfo, HitDefenseOutcome> Defended;

        public EnemyDefenseConfig DefaultConfig => defaultConfig;
        public EnemyDefenseConfig ActiveConfig => activeConfig;
        public EnemyDefenseMode CurrentMode => currentMode;
        public bool IsDefending => currentMode != EnemyDefenseMode.Inactive;
        public bool IsPerfectParryWindowActive =>
            currentMode == EnemyDefenseMode.PerfectParryWindow;

        private void Awake()
        {
            ResolveReferences();
            EndDefense();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (health != null)
                health.Died += OnEnemyDied;
        }

        /// <summary>
        /// Starts a normal guard using the default config. This no-argument
        /// version can also be called directly by an animation event.
        /// </summary>
        public void BeginDefense()
        {
            BeginDefense(defaultConfig);
        }

        /// <summary>
        /// Starts guarding with action-specific rules. A future defense state
        /// will use this overload when human and alien actions need different
        /// behavior without changing this component.
        /// </summary>
        public bool BeginDefense(EnemyDefenseConfig config)
        {
            activeConfig = config != null ? config : defaultConfig;
            if (!CanEnterDefense())
            {
                EndDefense();
                return false;
            }

            currentMode = EnemyDefenseMode.Guarding;
            return true;
        }

        /// <summary>
        /// Promotes an existing guard into its short perfect-parry window.
        /// Intended to be called by an animation event at the exact visual beat.
        /// </summary>
        public void OpenParryWindow()
        {
            if (currentMode == EnemyDefenseMode.Guarding)
                currentMode = EnemyDefenseMode.PerfectParryWindow;
        }

        /// <summary>
        /// Ends only the perfect timing window. The enemy remains in its weaker
        /// normal guard until its defense action exits.
        /// </summary>
        public void CloseParryWindow()
        {
            if (currentMode == EnemyDefenseMode.PerfectParryWindow)
                currentMode = EnemyDefenseMode.Guarding;
        }

        public void EndDefense()
        {
            currentMode = EnemyDefenseMode.Inactive;
            activeConfig = null;
        }

        public HitDefenseResult EvaluateHit(
            Collider hitCollider,
            DamageInfo incomingHit)
        {
            lastOutcome = HitDefenseOutcome.Passed;
            lastIncomingTypes = incomingHit.Type;

            if (!CanEvaluateHit(incomingHit))
                return HitDefenseResult.Pass(incomingHit);

            if (currentMode == EnemyDefenseMode.PerfectParryWindow)
            {
                lastOutcome = HitDefenseOutcome.Parried;
                Defended?.Invoke(incomingHit, lastOutcome);
                return HitDefenseResult.Parry(
                    incomingHit,
                    activeConfig.ParryCollisionImpulseMultiplier,
                    activeConfig.ParrySourceReboundSpeedMultiplier);
            }

            DamageInfo reducedHit = incomingHit.ScaleDamageAndImpulse(
                activeConfig.BlockDamageMultiplier,
                activeConfig.BlockImpulseMultiplier);
            lastOutcome = HitDefenseOutcome.Blocked;
            Defended?.Invoke(incomingHit, lastOutcome);
            return HitDefenseResult.Block(
                reducedHit,
                activeConfig.BlockCollisionImpulseMultiplier,
                activeConfig.BlockSourceReboundSpeedMultiplier);
        }

        private bool CanEnterDefense()
        {
            return activeConfig != null
                && health != null
                && !health.IsDead
                && (runtimeProfile == null
                    || runtimeProfile.HasCapability(
                        EnemyCapability.Blocking));
        }

        private bool CanEvaluateHit(DamageInfo incomingHit)
        {
            if (currentMode == EnemyDefenseMode.Inactive
                || !CanEnterDefense()
                || !activeConfig.CanDefend(incomingHit.Type))
            {
                return false;
            }

            return IsInsideDefenseArc(incomingHit);
        }

        private bool IsInsideDefenseArc(DamageInfo incomingHit)
        {
            Transform facing = facingReference != null
                ? facingReference
                : transform;
            Vector3 directionToAttack = GetDirectionToAttack(
                incomingHit,
                facing.position);

            if (directionToAttack.sqrMagnitude <= Mathf.Epsilon)
            {
                lastIncomingAngle = 0f;
                return true;
            }

            Vector3 flatForward = Vector3.ProjectOnPlane(
                facing.forward,
                Vector3.up);
            Vector3 flatDirection = Vector3.ProjectOnPlane(
                directionToAttack,
                Vector3.up);
            if (flatForward.sqrMagnitude <= Mathf.Epsilon
                || flatDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                lastIncomingAngle = Vector3.Angle(
                    facing.forward,
                    directionToAttack);
            }
            else
            {
                lastIncomingAngle = Vector3.Angle(
                    flatForward,
                    flatDirection);
            }

            return activeConfig.DefenseArc >= 360f
                || lastIncomingAngle <= activeConfig.DefenseArc * 0.5f;
        }

        private static Vector3 GetDirectionToAttack(
            DamageInfo incomingHit,
            Vector3 defenderPosition)
        {
            GameObject attackOwner = incomingHit.Instigator != null
                ? incomingHit.Instigator
                : incomingHit.Source;
            if (attackOwner != null)
            {
                Vector3 direction =
                    attackOwner.transform.position - defenderPosition;
                if (direction.sqrMagnitude > Mathf.Epsilon)
                    return direction.normalized;
            }

            return incomingHit.Direction.sqrMagnitude > Mathf.Epsilon
                ? -incomingHit.Direction
                : Vector3.zero;
        }

        private void ResolveReferences()
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();

            if (runtimeProfile == null)
                runtimeProfile = GetComponent<EnemyRuntimeProfile>();

            if (facingReference == null)
                facingReference = transform;
        }

        private void OnEnemyDied(DamageInfo killingHit)
        {
            EndDefense();
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= OnEnemyDied;

            EndDefense();
        }
    }
}
