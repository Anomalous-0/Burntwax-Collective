using System;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Player-facing adapter between the universal hit-defense pipeline and the
    /// active katana's parry state/configuration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KatanaParryDefense : MonoBehaviour, IHitDefense
    {
        [Header("References")]
        [Tooltip("Katana state machine that owns the active katana and parry window.")]
        [SerializeField] private KatanaStateMachine stateMachine;

        [Tooltip("Transform whose forward direction defines the defense arc. Defaults to the player transform.")]
        [SerializeField] private Transform facingReference;

        [Header("Debug (read-only)")]
        [SerializeField] private HitDefenseOutcome lastOutcome =
            HitDefenseOutcome.Passed;
        [SerializeField] private DamageType lastIncomingTypes =
            DamageType.None;
        [SerializeField] private float lastIncomingAngle;
        [SerializeField] private int consecutiveBlocks;
        [SerializeField] private bool guardExhausted;

        public event Action<DamageInfo, HitDefenseOutcome> Defended;

        public int ConsecutiveBlocks => consecutiveBlocks;
        public bool GuardExhausted => guardExhausted;

        private float guardRecoveryStartTime = -1f;
        private float activeGuardRecoveryDelay = 1.5f;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            TickGuardRecovery();
        }

        public HitDefenseResult EvaluateHit(
            Collider hitCollider,
            DamageInfo incomingHit)
        {
            lastOutcome = HitDefenseOutcome.Passed;
            lastIncomingTypes = incomingHit.Type;

            if (!TryGetActiveDefense(out KatanaParryConfig config))
                return HitDefenseResult.Pass(incomingHit);

            if (!config.CanDefend(incomingHit.Type)
                || !IsInsideDefenseArc(incomingHit, config.DefenseArc))
            {
                return HitDefenseResult.Pass(incomingHit);
            }

            if (stateMachine.isParryWindowActive)
            {
                if (config.PerfectParryRestoresGuard)
                    RestoreGuardCapacity();

                lastOutcome = HitDefenseOutcome.Parried;
                Defended?.Invoke(incomingHit, lastOutcome);
                return HitDefenseResult.Parry(incomingHit);
            }

            activeGuardRecoveryDelay = config.GuardRecoveryDelay;

            if (consecutiveBlocks >= config.MaximumConsecutiveBlocks)
            {
                guardExhausted = true;
                lastOutcome = HitDefenseOutcome.GuardBroken;
                Defended?.Invoke(incomingHit, lastOutcome);
                return HitDefenseResult.GuardBreak(incomingHit);
            }

            DamageInfo reducedHit = incomingHit.ScaleDamageAndImpulse(
                config.GuardDamageMultiplier,
                config.GuardImpulseMultiplier);

            consecutiveBlocks++;
            guardExhausted =
                consecutiveBlocks >= config.MaximumConsecutiveBlocks;
            lastOutcome = HitDefenseOutcome.Blocked;
            Defended?.Invoke(incomingHit, lastOutcome);
            return HitDefenseResult.Block(reducedHit);
        }

        private void TickGuardRecovery()
        {
            if (consecutiveBlocks <= 0)
            {
                guardRecoveryStartTime = -1f;
                return;
            }

            if (IsGuardStanceActive())
            {
                guardRecoveryStartTime = -1f;
                return;
            }

            if (guardRecoveryStartTime < 0f)
                guardRecoveryStartTime = Time.time;

            if (Time.time - guardRecoveryStartTime
                >= activeGuardRecoveryDelay)
            {
                RestoreGuardCapacity();
            }
        }

        private void RestoreGuardCapacity()
        {
            consecutiveBlocks = 0;
            guardExhausted = false;
            guardRecoveryStartTime = -1f;
        }

        private bool TryGetActiveDefense(out KatanaParryConfig config)
        {
            config = null;

            if (stateMachine == null)
                ResolveReferences();

            if (!IsGuardStanceActive())
                return false;

            KatanaScriptableObject activeKatana =
                stateMachine.ActiveKatanaScriptable;
            config = activeKatana != null
                ? activeKatana.ParryConfig
                : null;
            return config != null;
        }

        private bool IsGuardStanceActive()
        {
            return stateMachine != null
                && stateMachine.IsKatanaEquipped
                && stateMachine.states != null
                && stateMachine.CurrentState == stateMachine.states.Parry();
        }

        private bool IsInsideDefenseArc(
            DamageInfo incomingHit,
            float defenseArc)
        {
            Transform facing = facingReference != null
                ? facingReference
                : stateMachine != null && stateMachine.player != null
                    ? stateMachine.player.transform
                    : transform;

            Vector3 directionToAttack = GetDirectionToAttack(
                incomingHit,
                facing.position);
            if (directionToAttack.sqrMagnitude <= Mathf.Epsilon)
            {
                lastIncomingAngle = 0f;
                return true;
            }

            lastIncomingAngle = Vector3.Angle(
                facing.forward,
                directionToAttack);
            return lastIncomingAngle <= defenseArc * 0.5f;
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
            if (stateMachine == null)
                stateMachine = GetComponentInChildren<KatanaStateMachine>(true);

            if (facingReference == null
                && stateMachine != null
                && stateMachine.player != null)
            {
                facingReference = stateMachine.player.transform;
            }
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (stateMachine == null)
            {
                Debug.LogWarning(
                    $"[{nameof(KatanaParryDefense)}] '{name}' has no katana state machine assigned.",
                    this);
            }
        }

        private void OnDisable()
        {
            RestoreGuardCapacity();
        }
    }
}
