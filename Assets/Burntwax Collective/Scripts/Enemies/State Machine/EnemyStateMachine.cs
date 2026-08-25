using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Top-level enemy coordinator.
    ///
    /// Life is hierarchical (Alive or Dead). While Alive, two independent
    /// hierarchical root branches are updated:
    ///   Movement: physical mode and locomotion execution.
    ///   Action: awareness, combat decisions, attacks, and reactions.
    ///
    /// This component owns shared context and update order. It does not perform
    /// melee hit resolution itself; EnemyMeleeAttack remains the executor.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(EnemyHealth),
        typeof(EnemyRuntimeProfile),
        typeof(EnemyMeleeAttack))]
    public sealed class EnemyStateMachine : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyRuntimeProfile runtimeProfile;
        [SerializeField] private EnemyMeleeAttack meleeAttack;

        [Tooltip("Transform turned by the movement region. Defaults to the enemy root.")]
        [SerializeField] private Transform rotationRoot;

        [Header("Temporary Player Acquisition")]
        [Tooltip("Temporary replacement for EnemyPerception. Disable it when a real perception component supplies SetTarget.")]
        [SerializeField] private bool automaticallyFindPlayer = true;
        [SerializeField] private Transform target;
        [SerializeField, Min(0.1f)] private float targetSearchInterval = 1f;

        [Header("Basic Melee Decision")]
        [Tooltip("Decision distance only. The actual damaging shape remains in EnemyMeleeAttackConfig.")]
        [SerializeField, Min(0f)] private float meleeRequestRange = 2f;
        [SerializeField, Range(0f, 180f)] private float meleeStartAngle =
            15f;
        [SerializeField] private bool faceTargetDuringMeleeWindup = true;

        [Header("Debug (read-only)")]
        [SerializeField] private string lifeStatePath = "Inactive";
        [SerializeField] private string movementStatePath = "Inactive";
        [SerializeField] private string actionStatePath = "Inactive";

        public EnemyHealth Health => health;
        public EnemyRuntimeProfile RuntimeProfile => runtimeProfile;
        public EnemyMeleeAttack MeleeAttack => meleeAttack;
        public Transform Target => target;
        public bool HasValidTarget => target != null
            && target.gameObject.activeInHierarchy;
        public bool FaceTargetDuringMeleeWindup =>
            faceTargetDuringMeleeWindup;

        private EnemyStateFactory states;
        private float nextTargetSearchTime;

        // Action states write transient intents; movement states consume them.
        // This prevents an attack state from directly rotating or moving the
        // body and keeps future NavMesh/flying motors replaceable.
        private bool hasFacingRequest;
        private Vector3 requestedFacingPoint;

        public EnemyBaseState CurrentLifeState { get; private set; }
        public EnemyBaseState CurrentMovementState { get; private set; }
        public EnemyBaseState CurrentActionState { get; private set; }
        public EnemyStateFactory States => states;

        private void Awake()
        {
            ResolveReferences();
            states = new EnemyStateFactory(this);
            ApplyRuntimeProfile();
        }

        private void OnEnable()
        {
            ResolveReferences();
            health.Died += OnEnemyDied;
            runtimeProfile.ProfileRebuilt += ApplyRuntimeProfile;
            ApplyRuntimeProfile();

            nextTargetSearchTime = 0f;
            TryFindPlayerTarget();
            StartRootState(
                EnemyStateRegion.Life,
                health.IsDead
                    ? states.Dead()
                    : states.Alive());

            EnemyMeleePrototypeDriver prototypeDriver =
                GetComponent<EnemyMeleePrototypeDriver>();
            if (prototypeDriver != null && prototypeDriver.enabled)
            {
                Debug.LogWarning(
                    "[EnemyStateMachine] EnemyMeleePrototypeDriver is still "
                    + "enabled. Disable or remove it because the Action region "
                    + "now decides when melee attacks begin.",
                    this);
            }
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= OnEnemyDied;

            if (runtimeProfile != null)
                runtimeProfile.ProfileRebuilt -= ApplyRuntimeProfile;

            StopRootState(EnemyStateRegion.Action);
            StopRootState(EnemyStateRegion.Movement);
            StopRootState(EnemyStateRegion.Life);
            ClearMovementIntents();
            RefreshDebugPaths();
        }

        private void Update()
        {
            TryFindPlayerTarget();
            ClearMovementIntents();

            // Life gets first refusal. If it transitions to Dead, Alive.OnExit
            // shuts down both concurrent child regions before they can tick.
            CurrentLifeState?.UpdateStates();
            if (IsAlive)
            {
                // Action runs first so it can submit movement/facing intent for
                // Movement to consume deterministically in the same frame.
                CurrentActionState?.UpdateStates();
                CurrentMovementState?.UpdateStates();
            }

            RefreshDebugPaths();
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
                return;

            CurrentActionState?.FixedUpdateStates();
            CurrentMovementState?.FixedUpdateStates();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        internal bool IsAlive => ReferenceEquals(
            CurrentLifeState,
            states?.Alive());

        internal void StartAliveRegions()
        {
            StartRootState(EnemyStateRegion.Movement, states.Grounded());
            StartRootState(EnemyStateRegion.Action, states.Unaware());
        }

        internal void StopAliveRegions()
        {
            StopRootState(EnemyStateRegion.Action);
            StopRootState(EnemyStateRegion.Movement);
            ClearMovementIntents();
        }

        /// <summary>
        /// Called by EnemyBaseState.SwitchState when the active state is a root.
        /// Keeping branch ownership here is the only extension over the player's
        /// single CurrentState property.
        /// </summary>
        internal void ChangeRootState(
            EnemyStateRegion region,
            EnemyBaseState newState)
        {
            StartRootState(region, newState);
        }

        internal void RequestFacing(Vector3 worldPoint)
        {
            hasFacingRequest = true;
            requestedFacingPoint = worldPoint;
        }

        internal void RequestFacingTarget()
        {
            if (HasValidTarget)
                RequestFacing(target.position);
        }

        internal void ApplyFacingRequest()
        {
            if (!hasFacingRequest)
                return;

            Transform root = rotationRoot != null
                ? rotationRoot
                : transform;
            Vector3 flatDirection = Vector3.ProjectOnPlane(
                requestedFacingPoint - root.position,
                Vector3.up);
            if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            float turnSpeed = runtimeProfile != null
                ? runtimeProfile.Stats.TurnSpeed
                : EnemyRuntimeStats.Default.TurnSpeed;
            Quaternion desiredRotation = Quaternion.LookRotation(
                flatDirection.normalized,
                Vector3.up);
            root.rotation = Quaternion.RotateTowards(
                root.rotation,
                desiredRotation,
                turnSpeed * Time.deltaTime);
        }

        internal bool CanBeginMeleeAttack()
        {
            if (!HasValidTarget
                || meleeAttack == null
                || !meleeAttack.CanStartAttack
                || runtimeProfile == null
                || !runtimeProfile.HasCapability(
                    EnemyCapability.MeleeAttack))
            {
                return false;
            }

            Transform root = rotationRoot != null
                ? rotationRoot
                : transform;
            Vector3 toTarget = target.position - root.position;
            if (toTarget.sqrMagnitude > meleeRequestRange
                * meleeRequestRange)
            {
                return false;
            }

            Vector3 flatDirection = Vector3.ProjectOnPlane(
                toTarget,
                Vector3.up);
            if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
                return false;

            return Vector3.Angle(root.forward, flatDirection)
                <= meleeStartAngle;
        }

        private void OnEnemyDied(DamageInfo killingHit)
        {
            ChangeRootState(EnemyStateRegion.Life, states.Dead());
        }

        private void ApplyRuntimeProfile()
        {
            if (runtimeProfile != null && meleeAttack != null)
                meleeAttack.ApplyRuntimeStats(runtimeProfile.Stats);
        }

        private void TryFindPlayerTarget()
        {
            if (!automaticallyFindPlayer
                || HasValidTarget
                || Time.time < nextTargetSearchTime)
            {
                return;
            }

            nextTargetSearchTime = Time.time + targetSearchInterval;
            PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
                target = playerHealth.transform;
        }

        private void ClearMovementIntents()
        {
            hasFacingRequest = false;
            requestedFacingPoint = default;
        }

        private void StartRootState(
            EnemyStateRegion region,
            EnemyBaseState newState)
        {
            EnemyBaseState current = GetRootState(region);
            if (ReferenceEquals(current, newState))
                return;

            current?.ExitStateTree();
            SetRootState(region, newState);
            newState?.EnterStateTree();
        }

        private void StopRootState(EnemyStateRegion region)
        {
            EnemyBaseState current = GetRootState(region);
            current?.ExitStateTree();
            SetRootState(region, null);
        }

        private EnemyBaseState GetRootState(EnemyStateRegion region)
        {
            return region switch
            {
                EnemyStateRegion.Life => CurrentLifeState,
                EnemyStateRegion.Movement => CurrentMovementState,
                EnemyStateRegion.Action => CurrentActionState,
                _ => null
            };
        }

        private void SetRootState(
            EnemyStateRegion region,
            EnemyBaseState state)
        {
            switch (region)
            {
                case EnemyStateRegion.Life:
                    CurrentLifeState = state;
                    break;
                case EnemyStateRegion.Movement:
                    CurrentMovementState = state;
                    break;
                case EnemyStateRegion.Action:
                    CurrentActionState = state;
                    break;
            }
        }

        private void ResolveReferences()
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();

            if (runtimeProfile == null)
                runtimeProfile = GetComponent<EnemyRuntimeProfile>();

            if (meleeAttack == null)
                meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (rotationRoot == null)
                rotationRoot = transform;
        }

        private void RefreshDebugPaths()
        {
            lifeStatePath = BuildStatePath(CurrentLifeState);
            movementStatePath = BuildStatePath(CurrentMovementState);
            actionStatePath = BuildStatePath(CurrentActionState);
        }

        private static string BuildStatePath(EnemyBaseState root)
        {
            if (root == null)
                return "Inactive";

            string path = root.DebugName;
            EnemyBaseState substate = root.CurrentSubstate();
            while (substate != null)
            {
                path += " / " + substate.DebugName;
                substate = substate.CurrentSubstate();
            }

            return path;
        }

        private void OnValidate()
        {
            ResolveReferences();
            targetSearchInterval = Mathf.Max(0.1f, targetSearchInterval);
            meleeRequestRange = Mathf.Max(0f, meleeRequestRange);
            meleeStartAngle = Mathf.Clamp(meleeStartAngle, 0f, 180f);
        }
    }
}
