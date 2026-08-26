using System;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Player-facing adapter between the universal hit-defense pipeline and the
    /// active katana's parry state/configuration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KatanaParryDefense : MonoBehaviour,
        IHitDefense,
        IPlayerResourceSource
    {
        [Header("Resource")]
        [Tooltip("HUD identity for the katana's remaining safe blocks.")]
        [SerializeField] private PlayerResourceDefinition blockResourceDefinition;

        [Header("References")]
        [Tooltip("Katana state machine that owns the active katana and parry window.")]
        [SerializeField] private KatanaStateMachine stateMachine;

        [Tooltip("Transform whose forward direction defines the defense arc. Defaults to the player transform.")]
        [SerializeField] private Transform facingReference;

        [Tooltip("Audio source used for local defense sounds. Defaults to the player's AudioSource.")]
        [SerializeField] private AudioSource feedbackAudioSource;

        [Header("Debug (read-only)")]
        [SerializeField] private HitDefenseOutcome lastOutcome =
            HitDefenseOutcome.Passed;
        [SerializeField] private DamageType lastIncomingTypes =
            DamageType.None;
        [SerializeField] private float lastIncomingAngle;
        [SerializeField] private int consecutiveBlocks;
        [SerializeField] private bool guardExhausted;

        public event Action<DamageInfo, HitDefenseOutcome> Defended;
        public event Action<IPlayerResourceSource> ResourceChanged;

        public int ConsecutiveBlocks => consecutiveBlocks;
        public bool GuardExhausted => guardExhausted;
        public PlayerResourceDefinition ResourceDefinition =>
            blockResourceDefinition;
        public float CurrentValue => Mathf.Max(
            0f,
            MaximumValue - consecutiveBlocks);
        public float MaximumValue
        {
            get
            {
                KatanaScriptableObject katana = stateMachine != null
                    ? stateMachine.ActiveKatanaScriptable != null
                        ? stateMachine.ActiveKatanaScriptable
                        : stateMachine.KatanaScriptable
                    : null;
                KatanaParryConfig config = katana != null
                    ? katana.ParryConfig
                    : null;
                return config != null
                    ? config.MaximumConsecutiveBlocks
                    : 0f;
            }
        }
        public bool IsResourceAvailable => stateMachine != null
            && stateMachine.IsKatanaEquipped;

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
                bool perfectParryReturnHandled =
                    TryLaunchPerfectParryReturn(incomingHit, config);
                PlayDefenseFeedback(config, incomingHit, lastOutcome);
                Defended?.Invoke(incomingHit, lastOutcome);
                return HitDefenseResult.Parry(
                    incomingHit,
                    0f,
                    config.PerfectParryReboundSpeedMultiplier,
                    perfectParryReturnHandled);
            }

            activeGuardRecoveryDelay = config.GuardRecoveryDelay;

            if (consecutiveBlocks >= config.MaximumConsecutiveBlocks)
            {
                guardExhausted = true;
                lastOutcome = HitDefenseOutcome.GuardBroken;
                PlayDefenseFeedback(config, incomingHit, lastOutcome);
                Defended?.Invoke(incomingHit, lastOutcome);
                return HitDefenseResult.GuardBreak(incomingHit);
            }

            DamageInfo reducedHit = incomingHit.ScaleDamageAndImpulse(
                config.GuardDamageMultiplier,
                config.GuardImpulseMultiplier);

            consecutiveBlocks++;
            guardExhausted =
                consecutiveBlocks >= config.MaximumConsecutiveBlocks;
            NotifyGuardResourceChanged();
            lastOutcome = HitDefenseOutcome.Blocked;
            PlayDefenseFeedback(config, incomingHit, lastOutcome);
            Defended?.Invoke(incomingHit, lastOutcome);
            return HitDefenseResult.Block(
                reducedHit,
                config.GuardCollisionImpulseMultiplier,
                config.GuardReboundSpeedMultiplier);
        }

        private void PlayDefenseFeedback(
            KatanaParryConfig config,
            DamageInfo incomingHit,
            HitDefenseOutcome outcome)
        {
            config.DefenseFeedback?.Play(
                outcome,
                incomingHit,
                feedbackAudioSource);
        }

        private bool TryLaunchPerfectParryReturn(
            DamageInfo incomingHit,
            KatanaParryConfig config)
        {
            if (incomingHit.Source == null)
                return false;

            VelocityImpactDamageSource impactSource =
                incomingHit.Source.GetComponentInParent<
                    VelocityImpactDamageSource>();
            if (impactSource == null)
                return false;

            Rigidbody sourceBody = impactSource.GetComponentInParent<Rigidbody>();
            if (sourceBody == null)
                return false;

            float incomingSpeed = impactSource.LastImpactSpeed > 0f
                ? impactSource.LastImpactSpeed
                : impactSource.LastImpactClosingSpeed;
            float returnSpeed = incomingSpeed
                * config.PerfectParryReturnSpeedMultiplier;
            if (returnSpeed <= Mathf.Epsilon)
                return false;

            sourceBody.isKinematic = false;
            bool launchApplied;
            if (TryGetRecordedReturnPoint(
                impactSource,
                out Vector3 returnPoint))
            {
                float targetDistance = Vector3.Distance(
                    sourceBody.worldCenterOfMass,
                    returnPoint);
                float arcHeight = Mathf.Clamp(
                    targetDistance * config.PerfectParryArcHeightPerMeter,
                    config.PerfectParryMinimumArcHeight,
                    Mathf.Max(
                        config.PerfectParryMinimumArcHeight,
                        config.PerfectParryMaximumArcHeight));

                launchApplied = CurvedRigidbodyFlight.Launch(
                    sourceBody,
                    returnPoint,
                    returnSpeed,
                    arcHeight,
                    true);

                if (!launchApplied)
                {
                    Vector3 returnDirection =
                        returnPoint - sourceBody.worldCenterOfMass;
                    launchApplied = TryApplyStraightVelocity(
                        sourceBody,
                        returnDirection,
                        returnSpeed);
                }
            }
            else
            {
                CurvedRigidbodyFlight existingFlight =
                    sourceBody.GetComponent<CurvedRigidbodyFlight>();
                if (existingFlight != null)
                    existingFlight.CancelFlight();

                Camera playerCamera = Camera.main;
                Vector3 returnDirection = playerCamera != null
                    ? playerCamera.transform.forward
                    : facingReference != null
                        ? facingReference.forward
                        : transform.forward;
                launchApplied = TryApplyStraightVelocity(
                    sourceBody,
                    returnDirection,
                    returnSpeed);
            }

            if (!launchApplied)
                return false;

            RigidbodySpinUtility.ApplyMassWeightedRandomSpin(
                sourceBody,
                config.PerfectParryMinimumSpin,
                config.PerfectParryMaximumSpin);

            GameObject defender = stateMachine != null
                ? stateMachine.player
                : gameObject;
            impactSource.ArmAfterCurrentImpact(defender);
            return true;
        }

        private bool TryGetRecordedReturnPoint(
            VelocityImpactDamageSource impactSource,
            out Vector3 returnPoint)
        {
            returnPoint = default;
            GameObject recordedThrower = impactSource.LastArmedInstigator;
            if (recordedThrower == null
                || !recordedThrower.activeInHierarchy)
            {
                return false;
            }

            GameObject defender = stateMachine != null
                ? stateMachine.player
                : gameObject;
            if (recordedThrower == defender
                || defender != null
                && recordedThrower.transform.IsChildOf(defender.transform))
            {
                return false;
            }

            CombatAimTarget aimTarget =
                recordedThrower.GetComponentInChildren<CombatAimTarget>();
            return aimTarget != null
                && aimTarget.TryGetPosition(out returnPoint);
        }

        private static bool TryApplyStraightVelocity(
            Rigidbody body,
            Vector3 direction,
            float speed)
        {
            if (body == null
                || speed <= Mathf.Epsilon
                || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            body.linearVelocity = direction.normalized * speed;
            return true;
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
            bool changed = consecutiveBlocks != 0 || guardExhausted;
            consecutiveBlocks = 0;
            guardExhausted = false;
            guardRecoveryStartTime = -1f;

            if (changed)
                NotifyGuardResourceChanged();
        }

        private void NotifyGuardResourceChanged()
        {
            ResourceChanged?.Invoke(this);
        }

        private void OnKatanaResourceChanged(IPlayerResourceSource source)
        {
            // Dash and guard have separate values, but this notification also
            // tells the guard HUD when the katana is equipped or removed.
            NotifyGuardResourceChanged();
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

            if (feedbackAudioSource == null)
            {
                if (stateMachine != null && stateMachine.player != null)
                {
                    feedbackAudioSource =
                        stateMachine.player.GetComponent<AudioSource>();
                }

                if (feedbackAudioSource == null)
                    feedbackAudioSource = GetComponentInParent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (stateMachine != null)
                stateMachine.ResourceChanged += OnKatanaResourceChanged;

            if (stateMachine == null)
            {
                Debug.LogWarning(
                    $"[{nameof(KatanaParryDefense)}] '{name}' has no katana state machine assigned.",
                    this);
            }
        }

        private void OnDisable()
        {
            if (stateMachine != null)
                stateMachine.ResourceChanged -= OnKatanaResourceChanged;

            RestoreGuardCapacity();
        }
    }
}
