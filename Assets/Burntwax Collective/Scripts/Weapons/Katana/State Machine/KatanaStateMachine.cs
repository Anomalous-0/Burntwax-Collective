using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class KatanaStateMachine : MonoBehaviour,
        IParryReactionReceiver,
        IPlayerResourceSource
    {

        [Header("Debug (read-only)")]
        public string currentStateText;


        [HideInInspector] public Arsenal arsenal;
        [HideInInspector] public KatanaDriver katanaDriver;

        [Header("State Machines")]
        public GameObject player;
        [HideInInspector] public PlayerStateMachine stateMachine;
        [HideInInspector] public CamStateMachine camStateMachine;
        [HideInInspector] public PlayerCrosshair crosshair;


        [Space]
        [Header("Parry")]
        public float parryWindow = 0.2f;
        public float timeSlowRate = 0.1f;

        public bool isParryWindowActive = false;
        private Coroutine parryWindowCoroutine;

        [Space]
        [Header("Camera")]
        public float camSlowdown = 0.02f;


        [Space]
        [Header("Cinemachine Required")]
        public CinemachineImpulseSource camShake;

        [Header("Animation Rigging Required")]
        public Rig handIK;

        [Space]
        [Space]
        public KatanaScriptableObject KatanaScriptable;

        [Header("Save/Load Catalog")]
        [Tooltip("Every katana asset that can be restored from a save file. IDs must be unique.")]
        [SerializeField] private List<KatanaScriptableObject> katanaCatalog = new();

        [Header("Animation")]
        public GameObject Katana;
        public Animator anim;
        public KatanaScriptableObject ActiveKatanaScriptable;

        [Header("Slash Feedback")]
        [Tooltip("Optional origin for swing VFX. Defaults to the active katana model transform.")]
        [SerializeField] private Transform slashFeedbackOrigin;

        [Tooltip("Audio source used for local katana swing and attacker-parried cues. Falls back to an AudioSource on the arms or player.")]
        [SerializeField] private AudioSource katanaFeedbackAudioSource;

        private KatanaBaseState currentState;
        public KatanaStateFactory states;

        public KatanaBaseState CurrentState
        {
            get => currentState;
            set => currentState = value;
        }

        [Space]
        public bool isSlashing;
        public bool isParrying;
        public bool ableToSlashInMiddleOfAnimation;
        public bool fullyEquipped;

        [Header("Dash")]
        [SerializeField] private PlayerResourceDefinition dashResourceDefinition;

        [Header("Dash (read-only)")]
        [SerializeField] private int currentDashCharges;
        [SerializeField] private int airborneDashesUsed;

        private readonly List<float> dashRechargeCompletionTimes = new();
        private PlayerResourceRegistry playerResourceRegistry;

        public int CurrentDashCharges => currentDashCharges;
        public int AirborneDashesUsed => airborneDashesUsed;
        public PlayerResourceDefinition ResourceDefinition =>
            dashResourceDefinition;
        public float CurrentValue => currentDashCharges;
        public float MaximumValue => ActiveKatanaScriptable != null
            ? ActiveKatanaScriptable.MaxDashCharges
            : KatanaScriptable != null
                ? KatanaScriptable.MaxDashCharges
                : 0f;
        public bool IsResourceAvailable => IsDashEquipped;
        public bool HasDashCharge => currentDashCharges > 0;
        public bool IsKatanaEquipped => enabled
            && gameObject.activeInHierarchy
            && arsenal != null
            && arsenal.currentMode == Arsenal.WeaponMode.Katana
            && ActiveKatanaScriptable != null
            && Katana != null
            && Katana.activeInHierarchy;
        public bool IsDashEquipped => IsKatanaEquipped;

        public event Action<IPlayerResourceSource> ResourceChanged;

        public bool CanDash
        {
            get
            {
                if (!IsDashEquipped || !HasDashCharge)
                    return false;

                return IsPlayerGrounded()
                    || airborneDashesUsed < ActiveKatanaScriptable.MaxDashCharges;
            }
        }

        [Header("Current Attack")]
        [SerializeField]
        private KatanaAttackStrength currentAttackStrength =
            KatanaAttackStrength.Light;
        [SerializeField]
        private KatanaSlashDirection currentSlashDirection =
            KatanaSlashDirection.None;
        [SerializeField] private bool isDamageWindowActive;

        [Header("Gesture Debug (read-only)")]
        [SerializeField] private string lastRecognizedGesture = "None";
        [SerializeField] private Vector2 currentLookVelocity;
        [SerializeField] private float currentLookSpeed;
        [SerializeField] private float initialQualificationSpeed;
        [SerializeField] private float lastHeavyReversalSpeed;
        [SerializeField] private float lastReversalAlignment;
        [SerializeField] private bool isInitialSlashQualified;

        public KatanaAttackStrength CurrentAttackStrength => currentAttackStrength;
        public KatanaSlashDirection CurrentSlashDirection => currentSlashDirection;
        public bool IsDamageWindowActive => isDamageWindowActive;
        public bool IsInitialSlashQualified =>
            gestureRecognizer.IsInitialSlashQualified;
        public Vector2 InitialSlashDirection =>
            gestureRecognizer.InitialSlashDirection;
        public float ParriedAttackRecoveryDuration
        {
            get
            {
                KatanaParryConfig config = ActiveKatanaScriptable != null
                    ? ActiveKatanaScriptable.ParryConfig
                    : null;
                return config != null
                    ? config.ParriedAttackRecoveryDuration
                    : 0.35f;
            }
        }

        [HideInInspector] public bool selectedByArsenal;

        private readonly Dictionary<int, KatanaScriptableObject> katanaCatalogLookup = new();
        private readonly HashSet<int> warnedMissingAnimationStates = new();
        private KatanaAnimationType? activeAnimationType;
        private int activeAnimationHash;
        private int activeAnimationLayer;
        private readonly KatanaGestureRecognizer gestureRecognizer = new();
        private KatanaAttackIntent pendingAttackIntent;
        private bool hasPendingAttackIntent;
        private bool pendingAttackIsChained;

        private void Awake()
        {

            stateMachine = player.GetComponent<PlayerStateMachine>();
            camStateMachine = player.GetComponent<CamStateMachine>();
            crosshair = player.GetComponentInChildren<PlayerCrosshair>();

            arsenal = GetComponent<Arsenal>();
            katanaDriver = GetComponent<KatanaDriver>();
            ResolveFeedbackAudioSource();
            RebuildKatanaCatalogLookup();
            states = new KatanaStateFactory(this);
            currentState = states.Idle();
            currentState.EnterState();
        }

        private void RebuildKatanaCatalogLookup()
        {
            katanaCatalogLookup.Clear();

            if (katanaCatalog == null)
                return;

            foreach (KatanaScriptableObject katana in katanaCatalog)
            {
                if (katana == null)
                    continue;

                if (katanaCatalogLookup.ContainsKey(katana.ID))
                    Debug.LogWarning($"Katana save catalog contains duplicate ID {katana.ID} ('{katana.name}').", this);

                katanaCatalogLookup[katana.ID] = katana;
            }
        }

        private void Update()
        {
            if (stateMachine != null && stateMachine.GameplayActionsLocked)
                return;

            if (ActiveKatanaScriptable != null)
            {
                TickDashRecharge();

                if (IsPlayerGrounded())
                    airborneDashesUsed = 0;

                currentState.UpdateStates();
            }

            currentStateText = CurrentState.GetType().Name;
        }

        public bool TryRecognizeAttack(out KatanaAttackIntent intent)
        {
            intent = default;
            KatanaGestureConfig gestureConfig = GetGestureConfig();

            if (gestureConfig == null || InputManager.Instance == null)
            {
                ResetGestureRecognition();
                return false;
            }

            bool wasQualified =
                gestureRecognizer.IsInitialSlashQualified;
            bool recognized = gestureRecognizer.TryRecognize(
                InputManager.Instance.LookInput,
                InputManager.Instance.attackIsPressed,
                Time.unscaledDeltaTime,
                gestureConfig,
                out intent);

            if (recognized)
            {
                lastRecognizedGesture =
                    $"{intent.Strength} {intent.SlashDirection}";
            }
            else if (!wasQualified
                && gestureRecognizer.IsInitialSlashQualified)
            {
                lastRecognizedGesture = "Initial Flick Qualified";
            }

            SyncGestureDebug();
            return recognized;
        }

        public void ResetGestureRecognition()
        {
            gestureRecognizer.Reset();
            currentLookVelocity = Vector2.zero;
            currentLookSpeed = 0f;
            initialQualificationSpeed = 0f;
            lastHeavyReversalSpeed = 0f;
            lastReversalAlignment = 0f;
            isInitialSlashQualified = false;
            lastRecognizedGesture = "None";
            pendingAttackIntent = default;
            hasPendingAttackIntent = false;
            pendingAttackIsChained = false;
        }

        public void QueueAttackIntent(KatanaAttackIntent intent, bool isChained = false)
        {
            pendingAttackIntent = intent;
            hasPendingAttackIntent = intent.IsValid;
            pendingAttackIsChained = isChained;
        }

        public bool TryConsumeAttackIntent(out KatanaAttackIntent intent, out bool isChained)
        {
            intent = pendingAttackIntent;
            bool hadIntent = hasPendingAttackIntent;
            isChained = hadIntent && pendingAttackIsChained;
            pendingAttackIsChained = false;
            pendingAttackIntent = default;
            hasPendingAttackIntent = false;
            return hadIntent;
        }

        private KatanaGestureConfig GetGestureConfig()
        {
            return ActiveKatanaScriptable != null
                ? ActiveKatanaScriptable.GestureConfig
                : null;
        }

        private void SyncGestureDebug()
        {
            currentLookVelocity = gestureRecognizer.CurrentLookVelocity;
            currentLookSpeed = gestureRecognizer.CurrentLookSpeed;
            initialQualificationSpeed =
                gestureRecognizer.InitialQualificationSpeed;
            lastHeavyReversalSpeed =
                gestureRecognizer.LastHeavyReversalSpeed;
            lastReversalAlignment =
                gestureRecognizer.LastReversalAlignment;
            isInitialSlashQualified =
                gestureRecognizer.IsInitialSlashQualified;
        }

        public void Despawn(KatanaScriptableObject activeKatana)
        {
            ResetCombatPresentation();
            fullyEquipped = false;
            currentDashCharges = 0;
            airborneDashesUsed = 0;
            dashRechargeCompletionTimes.Clear();

            if (Katana != null)
                Katana.SetActive(false);

            activeKatana?.Despawn(this);
            ActiveKatanaScriptable = null;
            NotifyDashResourceChanged();
        }

        private void Spawn(KatanaScriptableObject activeKatana)
        {
            if (activeKatana == null)
                return;
            if (Katana != null)
                Katana.SetActive(true);

            activeKatana.Spawn(player, Katana, this);
            ActiveKatanaScriptable = activeKatana;

            if (activeKatana.GestureConfig == null)
            {
                Debug.LogWarning(
                    $"Katana '{activeKatana.name}' has no Gesture Config. " +
                    "It cannot recognize light or heavy slashes.",
                    this);
            }

            if (activeKatana.ParryConfig == null)
            {
                Debug.LogWarning(
                    $"Katana '{activeKatana.name}' has no Parry Config. "
                        + "It cannot block or parry incoming hits.",
                    this);
            }
        }

        public void OnAttackParried(
            DamageInfo parriedHit,
            Collider defenderCollider)
        {
            if (!IsKatanaEquipped
                || (parriedHit.Type & DamageType.Slash) == 0
                || !IsCurrentStateSlash()
                || parriedHit.Source == null
                || !parriedHit.Source.transform.IsChildOf(transform))
            {
                return;
            }

            PlayAttackParriedFeedback(parriedHit);
            currentState.ExitState();
            currentState = states.ParriedRecoil();
            currentState.EnterState();
        }

        public void PlaySlashFeedback(KatanaAttackStrength strength)
        {
            KatanaCombatFeedbackConfig feedbackConfig = ActiveKatanaScriptable != null
                ? ActiveKatanaScriptable.FeedbackConfig
                : null;
            if (feedbackConfig == null)
                return;

            ResolveFeedbackAudioSource();
            Transform cueOrigin = slashFeedbackOrigin != null
                ? slashFeedbackOrigin
                : Katana != null
                    ? Katana.transform
                    : transform;
            feedbackConfig.PlaySwing(
                strength,
                cueOrigin,
                katanaFeedbackAudioSource);
        }

        private void PlayAttackParriedFeedback(DamageInfo parriedHit)
        {
            KatanaCombatFeedbackConfig feedbackConfig = ActiveKatanaScriptable != null
                ? ActiveKatanaScriptable.FeedbackConfig
                : null;
            if (feedbackConfig == null)
                return;

            ResolveFeedbackAudioSource();
            feedbackConfig.PlayAttackParried(
                parriedHit,
                katanaFeedbackAudioSource);
        }

        private void ResolveFeedbackAudioSource()
        {
            if (katanaFeedbackAudioSource != null)
                return;

            katanaFeedbackAudioSource = GetComponent<AudioSource>();
            if (katanaFeedbackAudioSource == null && player != null)
            {
                katanaFeedbackAudioSource =
                    player.GetComponent<AudioSource>();
            }
        }

        private bool IsCurrentStateSlash()
        {
            return currentState == states.LightSlash()
                || currentState == states.HeavySlash();
        }

        public void Pickup(KatanaScriptableObject katana)
        {
            GunStateMachine gun = GetComponent<GunStateMachine>();
            Arsenal arsenal = GetComponent<Arsenal>();
            if (arsenal.currentMode == Arsenal.WeaponMode.Guns)
                gun.Despawn(gun.ActiveGunScriptable);

            arsenal.lastID = arsenal.currentID;
            arsenal.currentID = -1;

            KatanaScriptable = katana;
            SetupKatana();
            arsenal.currentMode = Arsenal.WeaponMode.Katana;
            NotifyDashResourceChanged();
        }

        public void SetupKatana()
        {
            ResetCombatPresentation();
            fullyEquipped = false;
            isSlashing = false;
            isParrying = false;
            ableToSlashInMiddleOfAnimation = false;

            Spawn(KatanaScriptable);
            RefillDashCharges();
            crosshair?.SetKatanaCrosshair(ActiveKatanaScriptable);

            currentState = states.Idle();
            currentState.EnterState();

            if (handIK != null)
                handIK.weight = 1;

            PlayKatanaAnimation(KatanaAnimationType.Equip);

            if (Katana != null)
                camShake = Katana.GetComponent<CinemachineImpulseSource>();

        }

        public bool TryConsumeDashCharge()
        {
            if (!CanDash)
                return false;

            currentDashCharges--;

            if (!IsPlayerGrounded())
                airborneDashesUsed++;

            float rechargeDuration = Mathf.Max(
                0.01f,
                ActiveKatanaScriptable.DashRechargeDuration);
            dashRechargeCompletionTimes.Add(Time.time + rechargeDuration);
            NotifyDashResourceChanged();
            return true;
        }

        public void RefillDashCharges()
        {
            dashRechargeCompletionTimes.Clear();
            airborneDashesUsed = 0;
            currentDashCharges = ActiveKatanaScriptable != null
                ? Mathf.Max(0, ActiveKatanaScriptable.MaxDashCharges)
                : 0;
            NotifyDashResourceChanged();
        }

        private void TickDashRecharge()
        {
            bool chargeChanged = false;
            for (int i = dashRechargeCompletionTimes.Count - 1; i >= 0; i--)
            {
                if (Time.time < dashRechargeCompletionTimes[i])
                    continue;

                currentDashCharges = Mathf.Min(
                    currentDashCharges + 1,
                    ActiveKatanaScriptable.MaxDashCharges);
                dashRechargeCompletionTimes.RemoveAt(i);
                chargeChanged = true;
            }

            if (chargeChanged)
                NotifyDashResourceChanged();
        }

        private bool IsPlayerGrounded()
        {
            return stateMachine != null
                && (stateMachine.CurrentState == stateMachine.states.Grounded()
                    || stateMachine.CurrentState == stateMachine.states.Slope());
        }

        private void ResetCombatPresentation()
        {
            CloseDamageWindow();
            EndDefenseAnimation();
            isSlashing = false;
            ableToSlashInMiddleOfAnimation = false;
            activeAnimationType = null;
            ResetGestureRecognition();
            katanaDriver?.EndSwing();
        }

        /// <summary>
        /// Directly blends to the animation requested by the current gameplay
        /// state. The Animator stores clips/blend trees; katana transitions
        /// are owned entirely by the gameplay state machine.
        /// </summary>
        public bool PlayKatanaAnimation(KatanaAnimationType animationType, bool restart = true,
            bool isChained = false)
        {
            if (anim == null)
                return false;

            KatanaAnimationConfig animationConfig =
                ActiveKatanaScriptable != null
                    ? ActiveKatanaScriptable.AnimationConfig
                    : null;

            string layerName = animationConfig != null
                ? animationConfig.AnimatorLayerName
                : "Default Layer";
            int layerIndex = anim.GetLayerIndex(layerName);
            if (layerIndex < 0)
            {
                WarnMissingAnimation(
                    Animator.StringToHash($"Layer:{layerName}"),
                    $"Animator layer '{layerName}' was not found.");
                return false;
            }

            string stateName;
            float crossFadeDuration;
            float startTime = 0f;
            KatanaAnimationConfig.State configuredState =
                animationConfig != null
                    ? animationConfig.GetState(animationType)
                    : null;

            if (animationType == KatanaAnimationType.Slash)
            {
                KatanaAnimationConfig.SlashState slashSettings =
                    animationConfig != null ? animationConfig.GetSlashState(currentSlashDirection) : null;
                stateName = slashSettings != null
                    && !string.IsNullOrWhiteSpace(slashSettings.AnimatorStateName)
                    ? slashSettings.AnimatorStateName
                    : KatanaAnimationConfig.GetDefaultSlashName(currentSlashDirection);
                crossFadeDuration = slashSettings == null ? 0.035f
                    : isChained ? slashSettings.ChainBlendDuration : slashSettings.BlendDuration;
                startTime = isChained && slashSettings != null
                    ? slashSettings.GetChainStartTime() : 0f;
            }
            else if (configuredState != null
                && !string.IsNullOrWhiteSpace(
                    configuredState.AnimatorStateName))
            {
                stateName = configuredState.AnimatorStateName;
                crossFadeDuration = configuredState.CrossFadeDuration;
                startTime = configuredState.StartTime;
            }
            else
            {
                GetFallbackAnimation(
                    animationType,
                    out stateName,
                    out crossFadeDuration,
                    out startTime);
            }

            string statePath = stateName.Contains(".")
                ? stateName
                : layerName + "." + stateName;
            int stateHash = Animator.StringToHash(statePath);
            if (!anim.HasState(layerIndex, stateHash))
            {
                WarnMissingAnimation(
                    stateHash,
                    $"Animator state '{statePath}' was not found.");
                return false;
            }

            // Ready and HeavyCharge can share a clip without replaying the
            // pullback. Chained slashes deliberately restart their clip.
            bool alreadyPlaying = anim.IsInTransition(layerIndex)
                ? anim.GetNextAnimatorStateInfo(layerIndex).fullPathHash == stateHash
                : anim.GetCurrentAnimatorStateInfo(layerIndex).fullPathHash == stateHash;
            activeAnimationType = animationType;
            activeAnimationHash = stateHash;
            activeAnimationLayer = layerIndex;
            if (!restart && alreadyPlaying)
                return true;

            anim.CrossFadeInFixedTime(
                stateHash,
                Mathf.Max(0f, crossFadeDuration),
                layerIndex,
                Mathf.Max(0f, startTime));
            return true;
        }

        private static void GetFallbackAnimation(
            KatanaAnimationType animationType,
            out string stateName,
            out float crossFadeDuration,
            out float startTime)
        {
            startTime = 0f;
            switch (animationType)
            {
                case KatanaAnimationType.Idle:
                case KatanaAnimationType.ParriedRecoil:
                    stateName = "equip_Katana";
                    crossFadeDuration = animationType == KatanaAnimationType.Idle ? 0.15f : 0.05f;
                    startTime = 0.275f;
                    return;
                case KatanaAnimationType.Equip:
                    stateName = "equip_Katana";
                    crossFadeDuration = 0.05f;
                    return;
                case KatanaAnimationType.Ready:
                case KatanaAnimationType.HeavyCharge:
                    stateName = "ready_Katana";
                    crossFadeDuration = 0.05f;
                    return;
                case KatanaAnimationType.Parry:
                    stateName = "parry_Katana";
                    crossFadeDuration = 0.04f;
                    return;
                case KatanaAnimationType.Stow:
                    stateName = "stow_Katana";
                    crossFadeDuration = 0.05f;
                    return;
                default:
                    stateName = string.Empty;
                    crossFadeDuration = 0f;
                    return;
            }
        }

        private void WarnMissingAnimation(int key, string message)
        {
            if (!warnedMissingAnimationStates.Add(key))
                return;

            Debug.LogWarning($"[KatanaStateMachine] {message}", this);
        }

        // ── Animation Events ─────────────────────────────────────

        public void EndEquip(AnimationEvent animationEvent = null)
        {
            if (!AcceptAnimationEvent(animationEvent, KatanaAnimationType.Equip))
                return;
            fullyEquipped = true;
            if (currentState == states.Idle())
                PlayKatanaAnimation(KatanaAnimationType.Idle);
        }

        public void SetAttackStrength(KatanaAttackStrength attackStrength)
        {
            currentAttackStrength = attackStrength;
        }

        public void SetSlashDirection(KatanaSlashDirection slashDirection)
        {
            currentSlashDirection = slashDirection;
        }

        public void OpenDamageWindow(AnimationEvent animationEvent = null)
        {
            if (!IsCurrentStateSlash()
                || !AcceptAnimationEvent(animationEvent, KatanaAnimationType.Slash))
                return;
            isDamageWindowActive = true;
        }

        public void CloseDamageWindow(AnimationEvent animationEvent = null)
        {
            // Direct gameplay cleanup must always work. Only clip events
            // are filtered, so an outgoing clip cannot close a new swing.
            if (animationEvent != null
                && !AcceptAnimationEvent(animationEvent, KatanaAnimationType.Slash))
                return;
            isDamageWindowActive = false;
            //camStateMachine?.SlowCam(false);
        }

        public void EndSlash(AnimationEvent animationEvent = null)
        {
            if (!IsCurrentStateSlash()
                || !AcceptAnimationEvent(animationEvent, KatanaAnimationType.Slash))
                return;
            CloseDamageWindow();
            ableToSlashInMiddleOfAnimation = false;
            isSlashing = false;


        }

        public void SlashChain(AnimationEvent animationEvent = null)
        {
            if (!IsCurrentStateSlash()
                || !AcceptAnimationEvent(animationEvent, KatanaAnimationType.Slash))
                return;
            ableToSlashInMiddleOfAnimation = true;
            // camStateMachine.SlowCam(false);

        }


        private IEnumerator ParryWindow()
        {
            // Debug.Log("Parry window started");
            isParryWindowActive = true;
            yield return new WaitForSeconds(parryWindow);
            isParryWindowActive = false;
            parryWindowCoroutine = null;
        }

        public void StartParry(AnimationEvent animationEvent = null)
        {
            if (currentState != states.Parry()
                || !AcceptAnimationEvent(animationEvent, KatanaAnimationType.Parry))
                return;
            if (parryWindowCoroutine != null)
                StopCoroutine(parryWindowCoroutine);

            parryWindowCoroutine = StartCoroutine(ParryWindow());
        }

        public void EndParry(AnimationEvent animationEvent = null)
        {
            if (currentState != states.Parry()
                || !AcceptAnimationEvent(animationEvent, KatanaAnimationType.Parry))
                return;
            isParrying = false;

        }

        private void OnEnable()
        {
            RegisterDashResource();
        }

        public void EndDefenseAnimation()
        {
            if (parryWindowCoroutine != null)
                StopCoroutine(parryWindowCoroutine);
            parryWindowCoroutine = null;
            isParryWindowActive = false;
            isParrying = false;
        }

        private bool AcceptAnimationEvent(AnimationEvent animationEvent, KatanaAnimationType expected)
        {
            if (!IsKatanaEquipped || activeAnimationType != expected)
                return false;
            if (animationEvent == null)
                return true;
            if (!animationEvent.isFiredByAnimator)
                return false;

            AnimatorStateInfo source = animationEvent.animatorStateInfo;
            AnimatorStateInfo destination = anim.IsInTransition(activeAnimationLayer)
                ? anim.GetNextAnimatorStateInfo(activeAnimationLayer)
                : anim.GetCurrentAnimatorStateInfo(activeAnimationLayer);
            // Crossfading can emit events from both clips. Compare playback
            // time as well as the state hash for slash-to-slash crossfades.
            return source.fullPathHash == activeAnimationHash
                && destination.fullPathHash == activeAnimationHash
                && Mathf.Abs(source.normalizedTime - destination.normalizedTime) < 0.001f;
        }

        private void OnDisable()
        {
            if (playerResourceRegistry != null)
            {
                playerResourceRegistry.UnregisterExternalSource(this);
                playerResourceRegistry = null;
            }

            ResetCombatPresentation();
        }

        private void RegisterDashResource()
        {
            if (player == null || dashResourceDefinition == null)
                return;

            playerResourceRegistry =
                player.GetComponent<PlayerResourceRegistry>();
            playerResourceRegistry?.RegisterExternalSource(this);
        }

        private void NotifyDashResourceChanged()
        {
            ResourceChanged?.Invoke(this);
        }


        // ── Save / Load ──────────────────────────────────────────

        public void SaveData(ref KatanaSaveData data)
        {
            data.ownsKatana = KatanaScriptable != null;
            data.katanaID = KatanaScriptable != null ? KatanaScriptable.ID : -1;

            if (KatanaScriptable != null && !katanaCatalogLookup.ContainsKey(KatanaScriptable.ID))
                Debug.LogWarning($"Owned katana '{KatanaScriptable.name}' (ID {KatanaScriptable.ID}) is not in the save/load catalog and cannot be restored after restarting the game.", this);
        }

        public void LoadData(KatanaSaveData data)
        {
            // Restore the katana reference (whether or not it's the active weapon)
            if (!data.ownsKatana)
            {
                KatanaScriptable = null;
                return;
            }

            if (katanaCatalogLookup.Count == 0)
                RebuildKatanaCatalogLookup();

            if (!katanaCatalogLookup.TryGetValue(data.katanaID, out KatanaScriptableObject savedKatana))
            {
                KatanaScriptable = null;
                Debug.LogWarning($"Saved katana ID {data.katanaID} is not present in the KatanaStateMachine save/load catalog.", this);
                return;
            }

            KatanaScriptable = savedKatana;
        }

        /// <summary>
        /// Called by Arsenal.LoadData when katana is the active weapon mode.
        /// Spawns the katana and enters a ready state without playing the equip animation.
        /// </summary>
        public void SetupAfterLoad()
        {
            if (KatanaScriptable == null)
            {
                anim.Play("empty_rest");
                return;
            }

            ResetCombatPresentation();

            Spawn(KatanaScriptable);
            RefillDashCharges();
            crosshair?.SetKatanaCrosshair(ActiveKatanaScriptable);

            currentState = states.Idle();
            fullyEquipped = true;
            currentState.EnterState();

            if (handIK != null)
                handIK.weight = 1;

            if (Katana != null)
                camShake = Katana.GetComponent<CinemachineImpulseSource>();

        }
    }

    [System.Serializable]
    public struct KatanaSaveData
    {
        public bool ownsKatana;
        public int katanaID;
    }
}
