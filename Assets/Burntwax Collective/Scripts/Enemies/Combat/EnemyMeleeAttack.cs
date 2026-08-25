using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    public enum EnemyMeleeAttackState
    {
        Disabled = 0,
        Ready = 1,
        Windup = 2,
        Active = 3,
        Recovery = 4,
        Parried = 5
    }

    /// <summary>
    /// Coordinates one enemy melee attack without depending on AI or Animator.
    /// Prototype timers and future animation events enter the same phases, so
    /// changing the phase driver never replaces damage or defense resolution.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyMeleeAttack : MonoBehaviour,
        IParryReactionReceiver
    {
        private const int HitBufferSize = 32;

        [Header("Attack")]
        [SerializeField] private EnemyMeleeAttackConfig attackConfig;

        [Tooltip("Start of the prototype hit capsule and source recorded in DamageInfo. Defaults to this transform.")]
        [SerializeField] private Transform attackOrigin;

        [Tooltip("Forward direction of the prototype hit capsule. Defaults to Attack Origin.")]
        [SerializeField] private Transform facingReference;

        [Header("Phase Driver")]
        [Tooltip("Uses config timing for immediate testing. Disable this when animation events drive the public phase methods.")]
        [SerializeField] private bool useAutomaticTiming = true;

        [Header("Debug (read-only)")]
        [SerializeField] private EnemyMeleeAttackState currentState =
            EnemyMeleeAttackState.Disabled;
        [SerializeField] private float stateElapsedTime;
        [SerializeField] private int targetsResolvedThisAttack;
        [SerializeField] private float runtimeDamageMultiplier = 1f;
        [SerializeField] private float runtimeAttackSpeedMultiplier = 1f;
        [SerializeField] private float runtimeStaggerDurationMultiplier = 1f;

        public event Action<EnemyMeleeAttackState> StateChanged;
        public event Action<EnemyMeleeAttackConfig> AttackStarted;
        public event Action<DamageInfo, HitDefenseResult> HitResolved;
        public event Action<DamageInfo> AttackParried;

        public EnemyMeleeAttackConfig AttackConfig => attackConfig;
        public EnemyMeleeAttackState CurrentState => currentState;
        public bool CanStartAttack => isActiveAndEnabled
            && attackConfig != null
            && health != null
            && !health.IsDead
            && currentState == EnemyMeleeAttackState.Ready;

        private readonly Collider[] hitBuffer = new Collider[HitBufferSize];
        private readonly HashSet<Component> resolvedTargets = new();
        private EnemyHealth health;

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
            EnterState(
                health.IsDead
                    ? EnemyMeleeAttackState.Disabled
                    : EnemyMeleeAttackState.Ready);
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= OnEnemyDied;

            resolvedTargets.Clear();
            currentState = EnemyMeleeAttackState.Disabled;
            stateElapsedTime = 0f;
            targetsResolvedThisAttack = 0;
        }

        private void Update()
        {
            if (!useAutomaticTiming || attackConfig == null)
                return;

            stateElapsedTime += Time.deltaTime;

            switch (currentState)
            {
                case EnemyMeleeAttackState.Windup:
                    if (stateElapsedTime >= GetAttackPhaseDuration(
                        attackConfig.WindupDuration))
                        OpenDamageWindow();
                    break;

                case EnemyMeleeAttackState.Active:
                    if (stateElapsedTime >= GetAttackPhaseDuration(
                        attackConfig.ActiveDuration))
                        CloseDamageWindow();
                    break;

                case EnemyMeleeAttackState.Recovery:
                    if (stateElapsedTime >= GetAttackPhaseDuration(
                        attackConfig.RecoveryDuration))
                        FinishAttack();
                    break;

                case EnemyMeleeAttackState.Parried:
                    if (stateElapsedTime >=
                        attackConfig.ParriedStaggerDuration
                        * runtimeStaggerDurationMultiplier)
                    {
                        FinishParriedReaction();
                    }
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (currentState == EnemyMeleeAttackState.Active)
                ResolveActiveHits();
        }

        /// <summary>
        /// Called by AI, a test driver, or another gameplay decision maker.
        /// Animation remains a presentation/phase driver, never the requester.
        /// </summary>
        public bool TryBeginAttack()
        {
            if (!CanStartAttack)
                return false;

            resolvedTargets.Clear();
            targetsResolvedThisAttack = 0;
            EnterState(EnemyMeleeAttackState.Windup);
            AttackStarted?.Invoke(attackConfig);
            return true;
        }

        /// <summary>
        /// Animation-event hook. Opens the damaging portion of the attack.
        /// The automatic prototype timeline calls this same method.
        /// </summary>
        public void OpenDamageWindow()
        {
            if (currentState != EnemyMeleeAttackState.Windup)
                return;

            EnterState(EnemyMeleeAttackState.Active);

            // Resolve once immediately so a very short animation window cannot
            // fall entirely between two physics ticks.
            ResolveActiveHits();
        }

        /// <summary>
        /// Animation-event hook. Ends damage and begins recovery.
        /// </summary>
        public void CloseDamageWindow()
        {
            if (currentState != EnemyMeleeAttackState.Active)
                return;

            EnterState(EnemyMeleeAttackState.Recovery);
        }

        /// <summary>
        /// Animation-event hook. Marks normal recovery as complete.
        /// </summary>
        public void FinishAttack()
        {
            if (currentState != EnemyMeleeAttackState.Recovery)
                return;

            resolvedTargets.Clear();
            EnterState(EnemyMeleeAttackState.Ready);
        }

        /// <summary>
        /// Animation-event hook. Marks the attacker's parried reaction as
        /// complete. The prototype timer calls this automatically.
        /// </summary>
        public void FinishParriedReaction()
        {
            if (currentState != EnemyMeleeAttackState.Parried)
                return;

            resolvedTargets.Clear();
            EnterState(EnemyMeleeAttackState.Ready);
        }

        public void CancelAttack()
        {
            if (currentState == EnemyMeleeAttackState.Disabled)
                return;

            resolvedTargets.Clear();
            EnterState(EnemyMeleeAttackState.Ready);
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
            if (currentState != EnemyMeleeAttackState.Active
                || !BelongsToThisAttack(parriedHit))
            {
                return;
            }

            resolvedTargets.Clear();
            EnterState(EnemyMeleeAttackState.Parried);
            AttackParried?.Invoke(parriedHit);
        }

        private void ResolveActiveHits()
        {
            if (attackConfig == null
                || currentState != EnemyMeleeAttackState.Active
                || targetsResolvedThisAttack >=
                    attackConfig.MaximumTargets)
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
            Vector3 end = start + forward * attackConfig.Reach;

            int hitCount = Physics.OverlapCapsuleNonAlloc(
                start,
                end,
                attackConfig.Radius,
                hitBuffer,
                attackConfig.TargetLayers,
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

                if (currentState != EnemyMeleeAttackState.Active
                    || targetsResolvedThisAttack >=
                        attackConfig.MaximumTargets)
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
                attackConfig.BaseDamage * runtimeDamageMultiplier,
                attackConfig.DamageType,
                point,
                -direction,
                direction,
                origin.gameObject,
                gameObject,
                direction * attackConfig.ImpactForce,
                attackConfig.PresentationType);

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
            EnterState(EnemyMeleeAttackState.Disabled);
        }

        private void EnterState(EnemyMeleeAttackState nextState)
        {
            if (currentState == nextState)
            {
                stateElapsedTime = 0f;
                return;
            }

            currentState = nextState;
            stateElapsedTime = 0f;
            StateChanged?.Invoke(currentState);
        }

        private float GetAttackPhaseDuration(float authoredDuration)
        {
            return authoredDuration / Mathf.Max(
                0.01f,
                runtimeAttackSpeedMultiplier);
        }

        private void OnDrawGizmosSelected()
        {
            if (attackConfig == null)
                return;

            Transform origin = attackOrigin != null
                ? attackOrigin
                : transform;
            Transform facing = facingReference != null
                ? facingReference
                : origin;
            Vector3 start = origin.position;
            Vector3 end = start
                + facing.forward.normalized * attackConfig.Reach;

            Color previousColor = Gizmos.color;
            Gizmos.color = currentState == EnemyMeleeAttackState.Active
                ? Color.red
                : new Color(1f, 0.55f, 0f, 0.8f);
            Gizmos.DrawWireSphere(start, attackConfig.Radius);
            Gizmos.DrawWireSphere(end, attackConfig.Radius);
            Gizmos.DrawLine(
                start + facing.up * attackConfig.Radius,
                end + facing.up * attackConfig.Radius);
            Gizmos.DrawLine(
                start - facing.up * attackConfig.Radius,
                end - facing.up * attackConfig.Radius);
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
