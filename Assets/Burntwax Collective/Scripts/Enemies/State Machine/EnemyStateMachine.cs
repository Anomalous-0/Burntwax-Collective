using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

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
    /// melee hit resolution itself; EnemyMeleeExecutor remains the executor.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(EnemyHealth),
        typeof(EnemyRuntimeProfile),
        typeof(EnemyMeleeExecutor))]
    [MovedFrom(
        true,
        "Burntwax",
        "Burntwax.Core",
        "EnemyMeleePrototypeDriver")]
    public sealed class EnemyStateMachine : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyRuntimeProfile runtimeProfile;
        [FormerlySerializedAs("meleeAttack")]
        [SerializeField] private EnemyMeleeExecutor meleeExecutor;

        [Tooltip("Passive executor used by configured projectile-throw actions. Optional for enemies without a throw action.")]
        [SerializeField] private StraightLinePickupEmitter projectileThrowExecutor;

        [Tooltip("Animator controlled by the enemy states. Defaults to the first Animator found on this enemy.")]
        [SerializeField] private Animator animator;

        [Tooltip("Transform turned by the movement region. Defaults to the enemy root.")]
        [SerializeField] private Transform rotationRoot;

        [Header("Temporary Player Acquisition")]
        [Tooltip("Temporary replacement for EnemyPerception. Disable it when a real perception component supplies SetTarget.")]
        [SerializeField] private bool automaticallyFindPlayer = true;
        [SerializeField] private Transform target;
        [SerializeField, Min(0.1f)] private float targetSearchInterval = 1f;

        [Header("Basic Melee Decision")]
        [Tooltip("Prototype/per-enemy override. When empty, actions come from the resolved archetype and evolution profile.")]
        [SerializeField] private EnemyCombatLoadout combatLoadoutOverride;

        [Tooltip("Attack selected by the current basic melee state. When empty, the executor's migrated fallback is used.")]
        [SerializeField] private EnemyMeleeAttackConfig defaultMeleeAttack;

        [FormerlySerializedAs("faceTargetDuringWindup")]
        [FormerlySerializedAs("faceTargetDuringMeleeWindup")]
        [SerializeField] private bool faceTargetDuringMeleeStartup = true;

        [Header("Animation")]
        [Tooltip("State used when no attack animation is playing. A short name is expanded to the selected layer.")]
        [SerializeField] private string defaultAnimationState = "default";

        [SerializeField, Min(0)] private int defaultAnimationLayer;
        [SerializeField, Min(0f)] private float defaultCrossFadeDuration = 0.1f;

        [Header("Debug (read-only)")]
        [SerializeField] private string lifeStatePath = "Inactive";
        [SerializeField] private string movementStatePath = "Inactive";
        [SerializeField] private string actionStatePath = "Inactive";

        public EnemyHealth Health => health;
        public EnemyRuntimeProfile RuntimeProfile => runtimeProfile;
        public EnemyMeleeExecutor MeleeExecutor => meleeExecutor;
        public StraightLinePickupEmitter ProjectileThrowExecutor =>
            projectileThrowExecutor;
        public Animator Animator => animator;
        public Transform Target => target;
        public bool HasValidTarget => target != null
            && target.gameObject.activeInHierarchy;
        public bool FaceTargetDuringMeleeStartup =>
            faceTargetDuringMeleeStartup;

        private EnemyStateFactory states;
        private readonly EnemyCombatActionSelector combatActionSelector = new();
        private readonly EnemyCombatActionConfig[] fallbackCombatActions =
            new EnemyCombatActionConfig[1];
        private float nextTargetSearchTime;

        // Action states write transient intents; movement states consume them.
        // This prevents an attack state from directly rotating or moving the
        // body and keeps future NavMesh/flying motors replaceable.
        private bool hasFacingRequest;
        private Vector3 requestedFacingPoint;
        private readonly HashSet<int> warnedAnimationStates = new();

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
            if (runtimeProfile != null)
                runtimeProfile.ProfileRebuilt += ApplyRuntimeProfile;
            ApplyRuntimeProfile();

            nextTargetSearchTime = 0f;
            TryFindPlayerTarget();
            StartRootState(
                EnemyStateRegion.Life,
                health.IsDead
                    ? states.Dead()
                    : states.Alive());

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
            combatActionSelector.ClearCooldowns();
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

        /// <summary>
        /// Central selection point used by the combat action states. Loadouts,
        /// capabilities, cooldowns, range, angle, priority, and weight all stay
        /// data-driven as more melee and projectile actions are added.
        /// </summary>
        internal bool TrySelectCombatAction(
            out EnemyCombatActionConfig selectedAction)
        {
            selectedAction = null;
            if (!HasValidTarget)
                return false;

            IReadOnlyList<EnemyCombatActionConfig> availableActions =
                GetAvailableCombatActions();
            Transform root = rotationRoot != null
                ? rotationRoot
                : transform;
            Vector3 toTarget = target.position - root.position;
            Vector3 flatDirection = Vector3.ProjectOnPlane(
                toTarget,
                Vector3.up);
            float targetAngle = flatDirection.sqrMagnitude > Mathf.Epsilon
                ? Vector3.Angle(root.forward, flatDirection)
                : 0f;
            EnemyCapability capabilities = runtimeProfile != null
                ? runtimeProfile.Capabilities
                : EnemyCapability.GroundMovement
                    | EnemyCapability.MeleeAttack;
            float targetDistance = toTarget.magnitude;
            float meleeBoundary = GetMeleeBoundary(availableActions);
            EnemyCombatActionCategory supportedCategories =
                EnemyCombatActionCategory.None;

            if (targetDistance <= meleeBoundary)
            {
                if (meleeExecutor != null
                    && meleeExecutor.CanStartAttack)
                {
                    supportedCategories |=
                        EnemyCombatActionCategory.Melee;
                }
            }
            else if (projectileThrowExecutor != null)
            {
                supportedCategories |= EnemyCombatActionCategory.Throw;
            }

            if (supportedCategories == EnemyCombatActionCategory.None)
                return false;

            EnemyCombatSelectionContext selectionContext =
                new EnemyCombatSelectionContext(
                    targetDistance,
                    targetAngle,
                    Time.time,
                    capabilities,
                    supportedCategories);

            return combatActionSelector.TrySelect(
                availableActions,
                selectionContext,
                out selectedAction);
        }

        private static float GetMeleeBoundary(
            IReadOnlyList<EnemyCombatActionConfig> actions)
        {
            float boundary = 0f;
            if (actions == null)
                return boundary;

            for (int i = 0; i < actions.Count; i++)
            {
                EnemyCombatActionConfig action = actions[i];
                if (action != null
                    && (action.Category
                        & EnemyCombatActionCategory.Melee) != 0)
                {
                    boundary = Mathf.Max(
                        boundary,
                        action.MaximumRange);
                }
            }

            return boundary;
        }

        internal void MarkCombatActionStarted(
            EnemyCombatActionConfig action)
        {
            combatActionSelector.MarkStarted(action, Time.time);
        }

        private IReadOnlyList<EnemyCombatActionConfig>
            GetAvailableCombatActions()
        {
            if (combatLoadoutOverride != null)
                return combatLoadoutOverride.Actions;

            if (runtimeProfile != null
                && runtimeProfile.CombatActions.Count > 0)
            {
                return runtimeProfile.CombatActions;
            }

            fallbackCombatActions[0] = defaultMeleeAttack != null
                ? defaultMeleeAttack
                : meleeExecutor?.FallbackAttackConfig;
            return fallbackCombatActions;
        }

        internal void PlayMeleeAnimation(EnemyMeleeAttackConfig config)
        {
            PlayCombatActionAnimation(config);
        }

        internal void PlayCombatActionAnimation(
            EnemyCombatActionConfig config)
        {
            if (config == null
                || string.IsNullOrWhiteSpace(config.AnimationStateName))
            {
                return;
            }

            TryCrossFadeAnimation(
                config.AnimationStateName,
                config.AnimationLayer,
                config.CrossFadeDuration);
        }

        internal void PlayDefaultAnimation()
        {
            TryCrossFadeAnimation(
                defaultAnimationState,
                defaultAnimationLayer,
                defaultCrossFadeDuration);
        }

        private void OnEnemyDied(DamageInfo killingHit)
        {
            ChangeRootState(EnemyStateRegion.Life, states.Dead());
        }

        private void ApplyRuntimeProfile()
        {
            if (runtimeProfile != null && meleeExecutor != null)
                meleeExecutor.ApplyRuntimeStats(runtimeProfile.Stats);
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

            if (meleeExecutor == null)
                meleeExecutor = GetComponent<EnemyMeleeExecutor>();

            if (projectileThrowExecutor == null)
            {
                projectileThrowExecutor = GetComponentInChildren<
                    StraightLinePickupEmitter>(true);
            }

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (rotationRoot == null)
                rotationRoot = transform;
        }

        private bool TryCrossFadeAnimation(
            string stateName,
            int layer,
            float duration)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return false;

            if (layer < 0 || layer >= animator.layerCount)
            {
                WarnMissingAnimation(
                    Animator.StringToHash(stateName),
                    $"Animator layer {layer} does not exist.");
                return false;
            }

            // Animator.HasState expects a full path hash. Configs can still use
            // readable short names; the layer name is supplied automatically.
            string statePath = stateName.Contains(".")
                ? stateName
                : animator.GetLayerName(layer) + "." + stateName;
            int stateHash = Animator.StringToHash(statePath);
            if (!animator.HasState(layer, stateHash))
            {
                WarnMissingAnimation(
                    stateHash,
                    $"Animator state '{statePath}' was not found.");
                return false;
            }

            animator.CrossFadeInFixedTime(
                stateHash,
                Mathf.Max(0f, duration),
                layer,
                0f);
            return true;
        }

        private void WarnMissingAnimation(int key, string message)
        {
            if (!warnedAnimationStates.Add(key))
                return;

            Debug.LogWarning($"[EnemyStateMachine] {message}", this);
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
            defaultAnimationLayer = Mathf.Max(0, defaultAnimationLayer);
            defaultCrossFadeDuration = Mathf.Max(
                0f,
                defaultCrossFadeDuration);
        }
    }
}
