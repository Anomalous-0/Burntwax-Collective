using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Burntwax
{
    /// <summary>
    /// Runtime phases within one melee action and its animation. These are not
    /// hierarchical AI states and never select animations by themselves.
    /// </summary>
    public enum EnemyMeleeExecutionPhase
    {
        Disabled = 0,
        Ready = 1,
        Startup = 2,
        DamageWindow = 3,
        Recovery = 4,
        Parried = 5
    }

    /// <summary>
    /// Coordinates one enemy melee attack without depending on AI or Animator.
    /// Fallback timers and animation events enter the same phases, so
    /// changing the phase driver never replaces damage or defense resolution.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    [MovedFrom(
        true,
        "Burntwax",
        "Burntwax.Core",
        "EnemyMeleeAttack")]
    public sealed class EnemyMeleeExecutor : MonoBehaviour,
        IParryReactionReceiver
    {
        private const int HitBufferSize = 32;

        [Header("Attack")]
        [Tooltip("Backward-compatible default used until an AI profile supplies its own attack selection.")]
        [FormerlySerializedAs("attackConfig")]
        [SerializeField] private EnemyMeleeAttackConfig fallbackAttackConfig;

        [Tooltip("Start of the prototype hit capsule and source recorded in DamageInfo. Defaults to this transform.")]
        [SerializeField] private Transform attackOrigin;

        [Tooltip("Forward direction of the prototype hit capsule. Defaults to Attack Origin.")]
        [SerializeField] private Transform facingReference;

        [Header("Damage Window Timing")]
        [Tooltip("Uses config timing as a fallback. Disable this when animation events drive OpenDamageWindow, CloseDamageWindow, and FinishAttack.")]
        [SerializeField] private bool useAutomaticTiming = true;

        [Header("Debug (read-only)")]
        [FormerlySerializedAs("currentState")]
        [SerializeField] private EnemyMeleeExecutionPhase currentPhase =
            EnemyMeleeExecutionPhase.Disabled;
        [FormerlySerializedAs("stateElapsedTime")]
        [SerializeField] private float phaseElapsedTime;
        [SerializeField] private int targetsResolvedThisAttack;
        [SerializeField] private float runtimeDamageMultiplier = 1f;
        [SerializeField] private float runtimeAttackSpeedMultiplier = 1f;
        [SerializeField] private float runtimeStaggerDurationMultiplier = 1f;

        public event Action<EnemyMeleeExecutionPhase> PhaseChanged;
        public event Action<EnemyMeleeAttackConfig> AttackStarted;
        public event Action<DamageInfo, HitDefenseResult> HitResolved;
        public event Action<DamageInfo> AttackParried;

        public EnemyMeleeAttackConfig FallbackAttackConfig =>
            fallbackAttackConfig;
        public EnemyMeleeAttackConfig ActiveAttackConfig =>
            activeAttackConfig;
        public EnemyMeleeExecutionPhase CurrentPhase => currentPhase;
        public bool CanStartAttack => isActiveAndEnabled
            && health != null
            && !health.IsDead
            && currentPhase == EnemyMeleeExecutionPhase.Ready;

        private readonly Collider[] hitBuffer = new Collider[HitBufferSize];
        private readonly HashSet<Component> resolvedTargets = new();
        private EnemyHealth health;
        private EnemyMeleeAttackConfig activeAttackConfig;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();

            if (attackOrigin == null)
                attackOrigin = transform;

            if (facingReference == null)
                facingReference = attackOrigin;
        }

        private void OnEnable()
        {
            health.Died += OnEnemyDied;
            EnterPhase(
                health.IsDead
                    ? EnemyMeleeExecutionPhase.Disabled
                    : EnemyMeleeExecutionPhase.Ready);
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= OnEnemyDied;

            resolvedTargets.Clear();
            activeAttackConfig = null;
            currentPhase = EnemyMeleeExecutionPhase.Disabled;
            phaseElapsedTime = 0f;
            targetsResolvedThisAttack = 0;
        }

        private void Update()
        {
            if (activeAttackConfig == null)
                return;

            phaseElapsedTime += Time.deltaTime;

            // A perfect parry interrupts the authored attack timeline. Its
            // gameplay stagger must therefore recover even when normal attack
            // phases are driven by animation events rather than fallback
            // timers.
            if (currentPhase == EnemyMeleeExecutionPhase.Parried)
            {
                if (phaseElapsedTime >=
                    activeAttackConfig.ParriedStaggerDuration
                    * runtimeStaggerDurationMultiplier)
                {
                    FinishParriedReaction();
                }

                return;
            }

            if (!useAutomaticTiming)
                return;

            switch (currentPhase)
            {
                case EnemyMeleeExecutionPhase.Startup:
                    if (phaseElapsedTime >= GetAttackPhaseDuration(
                        activeAttackConfig.StartupDuration))
                        OpenDamageWindow();
                    break;

                case EnemyMeleeExecutionPhase.DamageWindow:
                    if (phaseElapsedTime >= GetAttackPhaseDuration(
                        activeAttackConfig.DamageWindowDuration))
                        CloseDamageWindow();
                    break;

                case EnemyMeleeExecutionPhase.Recovery:
                    if (phaseElapsedTime >= GetAttackPhaseDuration(
                        activeAttackConfig.RecoveryDuration))
                        FinishAttack();
                    break;

            }
        }

        private void FixedUpdate()
        {
            if (currentPhase == EnemyMeleeExecutionPhase.DamageWindow)
                ResolveDamageWindowHits();
        }

        /// <summary>
        /// Called by an AI action state or another gameplay decision maker.
        /// Animation remains a presentation/phase driver, never the requester.
        /// </summary>
        public bool TryBeginAttack(EnemyMeleeAttackConfig selectedAttack)
        {
            if (!CanStartAttack || selectedAttack == null)
                return false;

            activeAttackConfig = selectedAttack;
            resolvedTargets.Clear();
            targetsResolvedThisAttack = 0;
            EnterPhase(EnemyMeleeExecutionPhase.Startup);
            AttackStarted?.Invoke(activeAttackConfig);
            return true;
        }

        /// <summary>
        /// Animation-event hook. Opens the damaging portion of the attack.
        /// The automatic fallback timeline calls this same method.
        /// </summary>
        public void OpenDamageWindow()
        {
            if (currentPhase != EnemyMeleeExecutionPhase.Startup)
                return;

            EnterPhase(EnemyMeleeExecutionPhase.DamageWindow);

            // Resolve once immediately so a very short animation window cannot
            // fall entirely between two physics ticks.
            ResolveDamageWindowHits();
        }

        /// <summary>
        /// Animation-event hook. Ends damage and begins recovery.
        /// </summary>
        public void CloseDamageWindow()
        {
            if (currentPhase != EnemyMeleeExecutionPhase.DamageWindow)
                return;

            EnterPhase(EnemyMeleeExecutionPhase.Recovery);
        }

        /// <summary>
        /// Animation-event hook. Marks normal recovery as complete.
        /// </summary>
        public void FinishAttack()
        {
            if (currentPhase != EnemyMeleeExecutionPhase.Recovery)
                return;

            resolvedTargets.Clear();
            EnterPhase(EnemyMeleeExecutionPhase.Ready);
            activeAttackConfig = null;
        }

        /// <summary>
        /// Animation-event hook. Marks the attacker's parried reaction as
        /// complete. The fallback timer calls this automatically.
        /// </summary>
        public void FinishParriedReaction()
        {
            if (currentPhase != EnemyMeleeExecutionPhase.Parried)
                return;

            resolvedTargets.Clear();
            EnterPhase(EnemyMeleeExecutionPhase.Ready);
            activeAttackConfig = null;
        }

        public void CancelAttack()
        {
            if (currentPhase == EnemyMeleeExecutionPhase.Disabled)
                return;

            resolvedTargets.Clear();
            EnterPhase(EnemyMeleeExecutionPhase.Ready);
            activeAttackConfig = null;
        }

        /// <summary>
        /// Receives the fully composed archetype/evolution values. The attack
        /// controller never reads traits or player knowledge directly.
        /// </summary>
        public void ApplyRuntimeStats(EnemyRuntimeStats stats)
        {
            runtimeDamageMultiplier = Mathf.Max(
                0f,
                stats.MeleeDamageMultiplier);
            runtimeAttackSpeedMultiplier = Mathf.Max(
                0.01f,
                stats.AttackSpeedMultiplier);
            runtimeStaggerDurationMultiplier = Mathf.Max(
                0f,
                stats.StaggerDurationMultiplier);
        }

        public void OnAttackParried(
            DamageInfo parriedHit,
            Collider defenderCollider)
        {
            if (currentPhase != EnemyMeleeExecutionPhase.DamageWindow
                || !BelongsToThisAttack(parriedHit))
            {
                return;
            }

            resolvedTargets.Clear();
            EnterPhase(EnemyMeleeExecutionPhase.Parried);
            AttackParried?.Invoke(parriedHit);
        }

        private void ResolveDamageWindowHits()
        {
            if (activeAttackConfig == null
                || currentPhase != EnemyMeleeExecutionPhase.DamageWindow
                || targetsResolvedThisAttack >=
                    activeAttackConfig.MaximumTargets)
            {
                return;
            }

            Transform origin = attackOrigin != null
                ? attackOrigin
                : transform;
            Transform facing = facingReference != null
                ? facingReference
                : origin;
            Vector3 forward = facing.forward.sqrMagnitude > Mathf.Epsilon
                ? facing.forward.normalized
                : transform.forward;
            Vector3 start = origin.position;
            Vector3 end = start + forward * activeAttackConfig.Reach;

            int hitCount = Physics.OverlapCapsuleNonAlloc(
                start,
                end,
                activeAttackConfig.Radius,
                hitBuffer,
                activeAttackConfig.TargetLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = hitBuffer[i];
                if (!TryGetTargetIdentity(
                        hitCollider,
                        out Component targetIdentity)
                    || resolvedTargets.Contains(targetIdentity))
                {
                    continue;
                }

                resolvedTargets.Add(targetIdentity);
                targetsResolvedThisAttack++;
                ResolveHit(hitCollider, origin, forward);

                if (currentPhase != EnemyMeleeExecutionPhase.DamageWindow
                    || targetsResolvedThisAttack >=
                        activeAttackConfig.MaximumTargets)
                {
                    break;
                }
            }
        }

        private void ResolveHit(
            Collider hitCollider,
            Transform origin,
            Vector3 fallbackDirection)
        {
            Vector3 point = hitCollider.ClosestPoint(origin.position);
            Vector3 direction = point - origin.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                direction = fallbackDirection;
            else
                direction.Normalize();

            DamageInfo damage = new DamageInfo(
                activeAttackConfig.BaseDamage * runtimeDamageMultiplier,
                activeAttackConfig.DamageType,
                point,
                -direction,
                direction,
                origin.gameObject,
                gameObject,
                direction * activeAttackConfig.ImpactForce,
                activeAttackConfig.PresentationType);

            HitResolver.ResolveHit(
                hitCollider,
                damage,
                out HitDefenseResult defenseResult);
            HitResolved?.Invoke(damage, defenseResult);
        }

        private bool TryGetTargetIdentity(
            Collider hitCollider,
            out Component targetIdentity)
        {
            targetIdentity = null;

            if (hitCollider == null
                || hitCollider.transform == transform
                || hitCollider.transform.IsChildOf(transform))
            {
                return false;
            }

            IDamageable damageable =
                hitCollider.GetComponentInParent<IDamageable>();
            if (damageable is Component damageableComponent)
            {
                targetIdentity = damageableComponent;
                return true;
            }

            IHitDefense defense =
                hitCollider.GetComponentInParent<IHitDefense>();
            if (defense is Component defenseComponent)
            {
                targetIdentity = defenseComponent;
                return true;
            }

            return false;
        }

        private bool BelongsToThisAttack(DamageInfo damage)
        {
            if (damage.Instigator == gameObject)
                return true;

            if (damage.Source == null)
                return false;

            Transform sourceTransform = damage.Source.transform;
            return sourceTransform == transform
                || sourceTransform.IsChildOf(transform);
        }

        private void OnEnemyDied(DamageInfo killingHit)
        {
            resolvedTargets.Clear();
            activeAttackConfig = null;
            EnterPhase(EnemyMeleeExecutionPhase.Disabled);
        }

        private void EnterPhase(EnemyMeleeExecutionPhase nextPhase)
        {
            if (currentPhase == nextPhase)
            {
                phaseElapsedTime = 0f;
                return;
            }

            currentPhase = nextPhase;
            phaseElapsedTime = 0f;
            PhaseChanged?.Invoke(currentPhase);
        }

        private float GetAttackPhaseDuration(float authoredDuration)
        {
            return authoredDuration / Mathf.Max(
                0.01f,
                runtimeAttackSpeedMultiplier);
        }

        private void OnDrawGizmosSelected()
        {
            EnemyMeleeAttackConfig gizmoConfig = activeAttackConfig != null
                ? activeAttackConfig
                : fallbackAttackConfig;
            if (gizmoConfig == null)
                return;

            Transform origin = attackOrigin != null
                ? attackOrigin
                : transform;
            Transform facing = facingReference != null
                ? facingReference
                : origin;
            Vector3 start = origin.position;
            Vector3 end = start
                + facing.forward.normalized * gizmoConfig.Reach;

            Color previousColor = Gizmos.color;
            Gizmos.color = currentPhase ==
                EnemyMeleeExecutionPhase.DamageWindow
                ? Color.red
                : new Color(1f, 0.55f, 0f, 0.8f);
            Gizmos.DrawWireSphere(start, gizmoConfig.Radius);
            Gizmos.DrawWireSphere(end, gizmoConfig.Radius);
            Gizmos.DrawLine(
                start + facing.up * gizmoConfig.Radius,
                end + facing.up * gizmoConfig.Radius);
            Gizmos.DrawLine(
                start - facing.up * gizmoConfig.Radius,
                end - facing.up * gizmoConfig.Radius);
            Gizmos.color = previousColor;
        }

        private void OnValidate()
        {
            if (attackOrigin == null)
                attackOrigin = transform;

            if (facingReference == null)
                facingReference = attackOrigin;
        }
    }
}
