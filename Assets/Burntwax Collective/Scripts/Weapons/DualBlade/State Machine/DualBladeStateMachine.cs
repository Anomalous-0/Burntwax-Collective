using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Animations.Rigging;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;

namespace Burntwax
{
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaStateMachine")]
    public class DualBladeStateMachine : MonoBehaviour,
        IParryReactionReceiver,
        IPlayerResourceSource
    {

        [Header("Debug (read-only)")]
        public string currentStateText;


        [HideInInspector] public Arsenal arsenal;
        [UnityEngine.Serialization.FormerlySerializedAs("katanaDriver")]
        [HideInInspector] public DualBladeDriver dualBladeDriver;

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
        [UnityEngine.Serialization.FormerlySerializedAs("KatanaScriptable")]
        public DualBladeScriptableObject DualBladeScriptable;

        [Header("Save/Load Catalog")]
        [Tooltip("Every dualBlade asset that can be restored from a save file. IDs must be unique.")]
        [UnityEngine.Serialization.FormerlySerializedAs("katanaCatalog")]
        [SerializeField] private List<DualBladeScriptableObject> dualBladeCatalog = new();

        [Header("Animation")]
        [FormerlySerializedAs("Katana")]
        [SerializeField] private GameObject rightBladeModel;
        [SerializeField] private GameObject leftBladeModel;
        public Animator anim;
        [UnityEngine.Serialization.FormerlySerializedAs("ActiveKatanaScriptable")]
        public DualBladeScriptableObject ActiveDualBladeScriptable;

        public GameObject RightBladeModel => rightBladeModel;
        public GameObject LeftBladeModel => leftBladeModel;
        private GloveStateMachine gloveStateMachine;
        private readonly HashSet<GameObject> attackTargets = new();
        private bool attackUsesLeftBlade;
        public bool AttackUsesLeftBlade => attackUsesLeftBlade;
        public uint AttackSequence { get; private set; }

        private bool BladeModeActive => enabled && gameObject.activeInHierarchy
            && arsenal != null && arsenal.currentMode == Arsenal.WeaponMode.DualBlade
            && !arsenal.GameplaySuppressed && ActiveDualBladeScriptable != null;
        public bool LeftHandAvailable => leftBladeModel != null
            && ActiveDualBladeScriptable != null && ActiveDualBladeScriptable.UseLeftBlade
            && (gloveStateMachine == null || !gloveStateMachine.OccupiesLeftHand);

        [Header("Slash Feedback")]
        [Tooltip("Optional origin for swing VFX. Defaults to the active dualBlade model transform.")]
        [SerializeField] private Transform slashFeedbackOrigin;
        [Tooltip("Optional second swing-VFX origin on the left blade. No extra audio; unused during glove actions. Leave empty for one effect for the pair.")]
        [SerializeField] private Transform leftSlashFeedbackOrigin;

        [Tooltip("Audio source used for local dualBlade swing and attacker-parried cues. Falls back to an AudioSource on the arms or player.")]
        [UnityEngine.Serialization.FormerlySerializedAs("katanaFeedbackAudioSource")]
        [SerializeField] private AudioSource dualBladeFeedbackAudioSource;

        private DualBladeBaseState currentState;
        public DualBladeStateFactory states;

        public DualBladeBaseState CurrentState
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
        public float MaximumValue => ActiveDualBladeScriptable != null
            ? ActiveDualBladeScriptable.MaxDashCharges
            : DualBladeScriptable != null
                ? DualBladeScriptable.MaxDashCharges
                : 0f;
        public bool IsResourceAvailable => IsDashEquipped;
        public bool HasDashCharge => currentDashCharges > 0;
        public bool IsDualBladeEquipped => BladeModeActive
            && rightBladeModel != null && rightBladeModel.activeInHierarchy;
        public bool IsDashEquipped => IsDualBladeEquipped;

        public event Action<IPlayerResourceSource> ResourceChanged;

        public bool CanDash
        {
            get
            {
                if (!IsDashEquipped || !HasDashCharge)
                    return false;

                return IsPlayerGrounded()
                    || airborneDashesUsed < ActiveDualBladeScriptable.MaxDashCharges;
            }
        }

        [Header("Current Attack")]
        [SerializeField]
        private DualBladeAttackStrength currentAttackStrength =
            DualBladeAttackStrength.Light;
        [SerializeField]
        private DualBladeSlashDirection currentSlashDirection =
            DualBladeSlashDirection.None;
        [SerializeField] private bool isDamageWindowActive;

        [Header("Gesture Debug (read-only)")]
        [SerializeField] private string lastRecognizedGesture = "None";
        [SerializeField] private Vector2 currentLookVelocity;
        [SerializeField] private float currentLookSpeed;
        [SerializeField] private float initialQualificationSpeed;
        [SerializeField] private float lastHeavyReversalSpeed;
        [SerializeField] private float lastReversalAlignment;
        [SerializeField] private bool isInitialSlashQualified;

        public DualBladeAttackStrength CurrentAttackStrength => currentAttackStrength;
        public DualBladeSlashDirection CurrentSlashDirection => currentSlashDirection;
        public bool IsDamageWindowActive => isDamageWindowActive;
        public bool IsInitialSlashQualified =>
            gestureRecognizer.IsInitialSlashQualified;
        public Vector2 InitialSlashDirection =>
            gestureRecognizer.InitialSlashDirection;
        public float ParriedAttackRecoveryDuration
        {
            get
            {
                DualBladeParryConfig config = ActiveDualBladeScriptable != null
                    ? ActiveDualBladeScriptable.ParryConfig
                    : null;
                return config != null
                    ? config.ParriedAttackRecoveryDuration
                    : 0.35f;
            }
        }

        [HideInInspector] public bool selectedByArsenal;

        private readonly Dictionary<int, DualBladeScriptableObject> dualBladeCatalogLookup = new();
        private readonly HashSet<int> warnedMissingAnimationStates = new();
        private DualBladeAnimationType? activeAnimationType;
        private int activeAnimationHash;
        private int activeAnimationLayer;
        private readonly DualBladeGestureRecognizer gestureRecognizer = new();
        private DualBladeAttackIntent pendingAttackIntent;
        private bool hasPendingAttackIntent;
        private bool pendingAttackIsChained;

        private void Awake()
        {

            stateMachine = player.GetComponent<PlayerStateMachine>();
            camStateMachine = player.GetComponent<CamStateMachine>();
            crosshair = player.GetComponentInChildren<PlayerCrosshair>();

            arsenal = GetComponent<Arsenal>();
            gloveStateMachine = GetComponent<GloveStateMachine>();
            dualBladeDriver = GetComponent<DualBladeDriver>();
            ResolveFeedbackAudioSource();
            RebuildDualBladeCatalogLookup();
            states = new DualBladeStateFactory(this);
            currentState = states.Idle();
            currentState.EnterState();
        }

        private void RebuildDualBladeCatalogLookup()
        {
            dualBladeCatalogLookup.Clear();

            if (dualBladeCatalog == null)
                return;

            foreach (DualBladeScriptableObject dualBlade in dualBladeCatalog)
            {
                if (dualBlade == null)
                    continue;

                if (dualBladeCatalogLookup.ContainsKey(dualBlade.ID))
                    Debug.LogWarning($"DualBlade save catalog contains duplicate ID {dualBlade.ID} ('{dualBlade.name}').", this);

                dualBladeCatalogLookup[dualBlade.ID] = dualBlade;
            }
        }

        private void Update()
        {
            RefreshBladeModels();
            if (arsenal != null && arsenal.GameplaySuppressed)
                return;
            if (stateMachine != null && stateMachine.GameplayActionsLocked)
                return;

            if (ActiveDualBladeScriptable != null)
            {
                TickDashRecharge();

                if (IsPlayerGrounded())
                    airborneDashesUsed = 0;

                currentState.UpdateStates();
            }

            currentStateText = CurrentState.GetType().Name;
        }

        private void LateUpdate() => RefreshBladeModels();

        // Arsenal selects the weapon; the glove only reserves the left hand.
        // Once reserved during a swing, that blade stays excluded until the next attack.
        public void RefreshBladeModels()
        {
            if (isSlashing && !LeftHandAvailable)
                attackUsesLeftBlade = false;

            SetBladeVisible(rightBladeModel, BladeModeActive);
            SetBladeVisible(leftBladeModel, BladeModeActive && LeftHandAvailable
                && (!isSlashing || attackUsesLeftBlade));
        }

        private static void SetBladeVisible(GameObject model, bool visible)
        {
            if (model != null && model.activeSelf != visible)
                model.SetActive(visible);
        }

        private void BindBladeDetectors(GameObject model, DualBladeHand hand)
        {
            if (model == null)
                return;
            foreach (DualBladeDetector detector in model.GetComponentsInChildren<DualBladeDetector>(true))
                detector.Configure(this, hand);
        }

        public void BeginBladeAttack(DualBladeAttackStrength strength, DualBladeSlashDirection direction)
        {
            CloseDamageWindow();
            SetAttackStrength(strength);
            SetSlashDirection(direction);
            attackTargets.Clear();
            unchecked { AttackSequence++; }
            attackUsesLeftBlade = LeftHandAvailable;
        }

        public bool CanBladeDealDamage(DualBladeHand hand)
        {
            if (!IsDualBladeEquipped || !isSlashing || !IsDamageWindowActive)
                return false;
            return hand == DualBladeHand.Right
                || (attackUsesLeftBlade && LeftHandAvailable && leftBladeModel.activeInHierarchy);
        }

        // One hit per target per attack, shared by both independently swept blades.
        // Closing/reopening an animation window must not clear this list.
        public bool TryRegisterBladeHit(GameObject target) =>
            target != null && IsDamageWindowActive && attackTargets.Add(target);

        private bool OwnsAttackSource(GameObject source)
        {
            if (source == null)
                return false;
            DualBladeDetector detector = source.GetComponent<DualBladeDetector>();
            return (detector != null && detector.Owner == this)
                || source.transform.IsChildOf(transform);
        }

        public bool TryRecognizeAttack(out DualBladeAttackIntent intent)
        {
            intent = default;
            DualBladeGestureConfig gestureConfig = GetGestureConfig();

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

        public void QueueAttackIntent(DualBladeAttackIntent intent, bool isChained = false)
        {
            pendingAttackIntent = intent;
            hasPendingAttackIntent = intent.IsValid;
            pendingAttackIsChained = isChained;
        }

        public bool TryConsumeAttackIntent(out DualBladeAttackIntent intent, out bool isChained)
        {
            intent = pendingAttackIntent;
            bool hadIntent = hasPendingAttackIntent;
            isChained = hadIntent && pendingAttackIsChained;
            pendingAttackIsChained = false;
            pendingAttackIntent = default;
            hasPendingAttackIntent = false;
            return hadIntent;
        }

        private DualBladeGestureConfig GetGestureConfig()
        {
            return ActiveDualBladeScriptable != null
                ? ActiveDualBladeScriptable.GestureConfig
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

        public void Despawn(DualBladeScriptableObject activeDualBlade)
        {
            ResetCombatPresentation();
            fullyEquipped = false;
            currentDashCharges = 0;
            airborneDashesUsed = 0;
            dashRechargeCompletionTimes.Clear();

            ActiveDualBladeScriptable = null;
            RefreshBladeModels();
            NotifyDashResourceChanged();
        }

        private void Spawn(DualBladeScriptableObject activeDualBlade)
        {
            if (activeDualBlade == null)
                return;
            ActiveDualBladeScriptable = activeDualBlade;
            BindBladeDetectors(rightBladeModel, DualBladeHand.Right);
            BindBladeDetectors(leftBladeModel, DualBladeHand.Left);
            RefreshBladeModels();

            if (activeDualBlade.GestureConfig == null)
            {
                Debug.LogWarning(
                    $"DualBlade '{activeDualBlade.name}' has no Gesture Config. " +
                    "It cannot recognize light or heavy slashes.",
                    this);
            }

            if (activeDualBlade.ParryConfig == null)
            {
                Debug.LogWarning(
                    $"DualBlade '{activeDualBlade.name}' has no Parry Config. "
                        + "It cannot block or parry incoming hits.",
                    this);
            }
        }

        public void OnAttackParried(
            DamageInfo parriedHit,
            Collider defenderCollider)
        {
            if (!IsDualBladeEquipped
                || (parriedHit.Type & DamageType.Slash) == 0
                || !IsCurrentStateSlash()
                || parriedHit.Source == null
                || !OwnsAttackSource(parriedHit.Source))
            {
                return;
            }

            PlayAttackParriedFeedback(parriedHit);
            currentState.ExitState();
            currentState = states.ParriedRecoil();
            currentState.EnterState();
        }

        public void PlaySlashFeedback(DualBladeAttackStrength strength)
        {
            DualBladeCombatFeedbackConfig feedbackConfig = ActiveDualBladeScriptable != null
                ? ActiveDualBladeScriptable.FeedbackConfig
                : null;
            if (feedbackConfig == null)
                return;

            ResolveFeedbackAudioSource();
            Transform cueOrigin = slashFeedbackOrigin != null
                ? slashFeedbackOrigin
                : rightBladeModel != null
                    ? rightBladeModel.transform
                    : transform;
            feedbackConfig.PlaySwing(
                strength,
                cueOrigin,
                dualBladeFeedbackAudioSource);
            if (attackUsesLeftBlade && LeftHandAvailable && leftSlashFeedbackOrigin != null)
                feedbackConfig.PlaySwing(strength, leftSlashFeedbackOrigin,
                    dualBladeFeedbackAudioSource, playAudio: false);
        }

        private void PlayAttackParriedFeedback(DamageInfo parriedHit)
        {
            DualBladeCombatFeedbackConfig feedbackConfig = ActiveDualBladeScriptable != null
                ? ActiveDualBladeScriptable.FeedbackConfig
                : null;
            if (feedbackConfig == null)
                return;

            ResolveFeedbackAudioSource();
            feedbackConfig.PlayAttackParried(
                parriedHit,
                dualBladeFeedbackAudioSource);
        }

        private void ResolveFeedbackAudioSource()
        {
            if (dualBladeFeedbackAudioSource != null)
                return;

            dualBladeFeedbackAudioSource = GetComponent<AudioSource>();
            if (dualBladeFeedbackAudioSource == null && player != null)
            {
                dualBladeFeedbackAudioSource =
                    player.GetComponent<AudioSource>();
            }
        }

        private bool IsCurrentStateSlash()
        {
            return currentState == states.LightSlash()
                || currentState == states.HeavySlash();
        }

        public void Pickup(DualBladeScriptableObject dualBlade)
        {
            GunStateMachine gun = GetComponent<GunStateMachine>();
            Arsenal arsenal = GetComponent<Arsenal>();
            if (arsenal.currentMode == Arsenal.WeaponMode.Guns)
                gun.Despawn(gun.ActiveGunScriptable);

            arsenal.lastID = arsenal.currentID;
            arsenal.currentID = -1;

            DualBladeScriptable = dualBlade;
            arsenal.currentMode = Arsenal.WeaponMode.DualBlade;
            SetupDualBlade();
            NotifyDashResourceChanged();
        }

        public void SetupDualBlade()
        {
            ResetCombatPresentation();
            fullyEquipped = false;
            isSlashing = false;
            isParrying = false;
            ableToSlashInMiddleOfAnimation = false;

            Spawn(DualBladeScriptable);
            RefillDashCharges();
            crosshair?.SetDualBladeCrosshair(ActiveDualBladeScriptable);

            currentState = states.Idle();
            currentState.EnterState();

            if (handIK != null)
                handIK.weight = 1;

            PlayDualBladeAnimation(DualBladeAnimationType.Equip);

            if (rightBladeModel != null)
                camShake = rightBladeModel.GetComponent<CinemachineImpulseSource>();

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
                ActiveDualBladeScriptable.DashRechargeDuration);
            dashRechargeCompletionTimes.Add(Time.time + rechargeDuration);
            NotifyDashResourceChanged();
            return true;
        }

        public void RefillDashCharges()
        {
            dashRechargeCompletionTimes.Clear();
            airborneDashesUsed = 0;
            currentDashCharges = ActiveDualBladeScriptable != null
                ? Mathf.Max(0, ActiveDualBladeScriptable.MaxDashCharges)
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
                    ActiveDualBladeScriptable.MaxDashCharges);
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
            attackTargets.Clear();
            attackUsesLeftBlade = false;
            CloseDamageWindow();
            EndDefenseAnimation();
            isSlashing = false;
            ableToSlashInMiddleOfAnimation = false;
            activeAnimationType = null;
            ResetGestureRecognition();
            dualBladeDriver?.EndSwing();
        }

        /// <summary>
        /// Directly blends to the animation requested by the current gameplay
        /// state. The Animator stores clips/blend trees; dualBlade transitions
        /// are owned entirely by the gameplay state machine.
        /// </summary>
        public bool PlayDualBladeAnimation(DualBladeAnimationType animationType, bool restart = true,
            bool isChained = false)
        {
            if (anim == null)
                return false;

            DualBladeAnimationConfig animationConfig =
                ActiveDualBladeScriptable != null
                    ? ActiveDualBladeScriptable.GetAnimationConfig(
                        animationType == DualBladeAnimationType.Slash
                            ? attackUsesLeftBlade : LeftHandAvailable)
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
            DualBladeAnimationConfig.State configuredState =
                animationConfig != null
                    ? animationConfig.GetState(animationType)
                    : null;

            if (animationType == DualBladeAnimationType.Slash)
            {
                DualBladeAnimationConfig.SlashState slashSettings =
                    animationConfig != null ? animationConfig.GetSlashState(currentSlashDirection) : null;
                stateName = slashSettings != null
                    && !string.IsNullOrWhiteSpace(slashSettings.AnimatorStateName)
                    ? slashSettings.AnimatorStateName
                    : DualBladeAnimationConfig.GetDefaultSlashName(currentSlashDirection);
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
            DualBladeAnimationType animationType,
            out string stateName,
            out float crossFadeDuration,
            out float startTime)
        {
            startTime = 0f;
            switch (animationType)
            {
                case DualBladeAnimationType.Idle:
                case DualBladeAnimationType.ParriedRecoil:
                    stateName = "equip_DualBlade";
                    crossFadeDuration = animationType == DualBladeAnimationType.Idle ? 0.15f : 0.05f;
                    startTime = 0.275f;
                    return;
                case DualBladeAnimationType.Equip:
                    stateName = "equip_DualBlade";
                    crossFadeDuration = 0.05f;
                    return;
                case DualBladeAnimationType.Ready:
                case DualBladeAnimationType.HeavyCharge:
                    stateName = "ready_DualBlade";
                    crossFadeDuration = 0.05f;
                    return;
                case DualBladeAnimationType.Parry:
                    stateName = "parry_DualBlade";
                    crossFadeDuration = 0.04f;
                    return;
                case DualBladeAnimationType.Stow:
                    stateName = "stow_DualBlade";
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

            Debug.LogWarning($"[DualBladeStateMachine] {message}", this);
        }

        // ── Animation Events ─────────────────────────────────────

        public void EndEquip(AnimationEvent animationEvent = null)
        {
            if (!AcceptAnimationEvent(animationEvent, DualBladeAnimationType.Equip))
                return;
            fullyEquipped = true;
            if (currentState == states.Idle())
                PlayDualBladeAnimation(DualBladeAnimationType.Idle);
        }

        public void SetAttackStrength(DualBladeAttackStrength attackStrength)
        {
            currentAttackStrength = attackStrength;
        }

        public void SetSlashDirection(DualBladeSlashDirection slashDirection)
        {
            currentSlashDirection = slashDirection;
        }

        public void OpenDamageWindow(AnimationEvent animationEvent = null)
        {
            if (!IsCurrentStateSlash()
                || !AcceptAnimationEvent(animationEvent, DualBladeAnimationType.Slash))
                return;
            isDamageWindowActive = true;
        }

        public void CloseDamageWindow(AnimationEvent animationEvent = null)
        {
            // Direct gameplay cleanup must always work. Only clip events
            // are filtered, so an outgoing clip cannot close a new swing.
            if (animationEvent != null
                && !AcceptAnimationEvent(animationEvent, DualBladeAnimationType.Slash))
                return;
            isDamageWindowActive = false;
            //camStateMachine?.SlowCam(false);
        }

        public void EndSlash(AnimationEvent animationEvent = null)
        {
            if (!IsCurrentStateSlash()
                || !AcceptAnimationEvent(animationEvent, DualBladeAnimationType.Slash))
                return;
            CloseDamageWindow();
            ableToSlashInMiddleOfAnimation = false;
            isSlashing = false;


        }

        public void SlashChain(AnimationEvent animationEvent = null)
        {
            if (!IsCurrentStateSlash()
                || !AcceptAnimationEvent(animationEvent, DualBladeAnimationType.Slash))
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
                || !AcceptAnimationEvent(animationEvent, DualBladeAnimationType.Parry))
                return;
            if (parryWindowCoroutine != null)
                StopCoroutine(parryWindowCoroutine);

            parryWindowCoroutine = StartCoroutine(ParryWindow());
        }

        public void EndParry(AnimationEvent animationEvent = null)
        {
            if (currentState != states.Parry()
                || !AcceptAnimationEvent(animationEvent, DualBladeAnimationType.Parry))
                return;
            isParrying = false;

        }

        private void OnEnable()
        {
            RegisterDashResource();
            RefreshBladeModels();
        }

        public void EndDefenseAnimation()
        {
            if (parryWindowCoroutine != null)
                StopCoroutine(parryWindowCoroutine);
            parryWindowCoroutine = null;
            isParryWindowActive = false;
            isParrying = false;
        }

        private bool AcceptAnimationEvent(AnimationEvent animationEvent, DualBladeAnimationType expected)
        {
            if (!IsDualBladeEquipped || activeAnimationType != expected)
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
            RefreshBladeModels();
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

        public void SaveData(ref DualBladeSaveData data)
        {
            data.ownsDualBlade = DualBladeScriptable != null;
            data.dualBladeID = DualBladeScriptable != null ? DualBladeScriptable.ID : -1;

            if (DualBladeScriptable != null && !dualBladeCatalogLookup.ContainsKey(DualBladeScriptable.ID))
                Debug.LogWarning($"Owned dualBlade '{DualBladeScriptable.name}' (ID {DualBladeScriptable.ID}) is not in the save/load catalog and cannot be restored after restarting the game.", this);
        }

        public void LoadData(DualBladeSaveData data)
        {
            // Restore the dualBlade reference (whether or not it's the active weapon)
            if (!data.ownsDualBlade)
            {
                DualBladeScriptable = null;
                return;
            }

            if (dualBladeCatalogLookup.Count == 0)
                RebuildDualBladeCatalogLookup();

            if (!dualBladeCatalogLookup.TryGetValue(data.dualBladeID, out DualBladeScriptableObject savedDualBlade))
            {
                DualBladeScriptable = null;
                Debug.LogWarning($"Saved dualBlade ID {data.dualBladeID} is not present in the DualBladeStateMachine save/load catalog.", this);
                return;
            }

            DualBladeScriptable = savedDualBlade;
        }

        /// <summary>
        /// Called by Arsenal.LoadData when dualBlade is the active weapon mode.
        /// Spawns the dualBlade and enters a ready state without playing the equip animation.
        /// </summary>
        public void SetupAfterLoad()
        {
            if (DualBladeScriptable == null)
            {
                anim.Play("empty_rest");
                return;
            }

            ResetCombatPresentation();

            Spawn(DualBladeScriptable);
            RefillDashCharges();
            crosshair?.SetDualBladeCrosshair(ActiveDualBladeScriptable);

            currentState = states.Idle();
            fullyEquipped = true;
            currentState.EnterState();

            if (handIK != null)
                handIK.weight = 1;

            if (rightBladeModel != null)
                camShake = rightBladeModel.GetComponent<CinemachineImpulseSource>();

        }
    }

    [System.Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaSaveData")]
    public struct DualBladeSaveData
    {
        [UnityEngine.Serialization.FormerlySerializedAs("ownsKatana")]
        public bool ownsDualBlade;
        [UnityEngine.Serialization.FormerlySerializedAs("katanaID")]
        public int dualBladeID;
    }
}
