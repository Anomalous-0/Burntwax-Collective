using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Burntwax
{
    /// <summary>
    /// The gravity glove. It lives ALONGSIDE the Arsenal (it is not a WeaponMode) and is equipped by
    /// tapping F once. Two mutually-exclusive ability branches hang off the Idle hub:
    ///   • Attract  (double-tap F): pull Consumable-layer objects toward the hand; the nearest latches
    ///     and orbits, then a tap / katana slash launches it along the camera forward.
    ///   • Charge   (hold F):       charge a plasma blast (slows movement), release to fire a projectile.
    ///
    /// This MonoBehaviour owns all the runtime work (detection, physics, tint, spawning); the HSM
    /// state classes stay thin and just call into it and pick transitions. Input arrives purely as the
    /// semantic flags on <see cref="InputManager"/> (gloveTap / gloveDoubleTap / gloveCharging / …).
    /// </summary>
    [DisallowMultipleComponent]
    public class GloveStateMachine : MonoBehaviour
    {
        [Header("Debug (read-only)")]
        public string currentStateText;
        public string currentSubstateText;

        [Header("References")]
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private CamStateMachine camStateMachine;
        [SerializeField] private Animator armsAnimator;
        [SerializeField] private TwoBoneIKConstraint leftHandIK;

        [Header("Arm Override")]
        [SerializeField] private string gloveLayerName = "Glove";
        [Tooltip("Default fixed-time blend, in seconds, between glove animation states.")]
        [SerializeField, Min(0f)] private float animationCrossFadeDuration =
            0.08f;
        [Tooltip("Raise/lower speed in normalized layer weight per second.")]
        [SerializeField] private float raiseSpeed = 6f;
        [Range(0f, 1f)]
        [SerializeField] private float raiseAmount;

        [Header("States")]
        [SerializeField] private float equipWaitTime = 1f;
        public bool BlastWindow { get; private set; }

        [SerializeField] private bool enterAttract = false;
        [SerializeField] private bool enterCharge = false;

        [Header("Detection")]
        public GameObject detectedObject;
        [SerializeField] private bool canAttract = false;
        [SerializeField] private float detectDistance = 9f;        // max reach of the glove
        [SerializeField] private float castRadius = 0.6f;          // spherecast radius = aim margin of error
        [SerializeField] private LayerMask attractableLayer;       // set to the Consumable layer
        [Tooltip("Colour the primary target is tinted toward while the glove is equipped.")]
        [SerializeField] private Color detectionTint = Color.cyan;
        [Range(0f, 1f)]
        [Tooltip("How far the primary target is tinted toward Detection Tint (0 = none).")]
        [SerializeField] private float detectionTintStrength = 0.35f;

        [Header("Holding")]
        public GameObject heldObject;
        public GameObject restPos;
        [Tooltip("Animator state used for objects with the Small hold pose or no GloveHoldProfile.")]
        [SerializeField] private string smallHoldAnimationState = "hold_Glove";
        [Tooltip("Animator state used for objects with the Large hold pose. Falls back to the small state when missing.")]
        [SerializeField] private string largeHoldAnimationState = "hold_large_Glove";
        [Header("Attraction")]
        [Tooltip("How strongly the object corrects toward the arc.")]
        [SerializeField, Min(0f)] private float attractionSteeringAcceleration = 35f;
        [Tooltip("Closest-range attraction speed. Prevents nearby objects from becoming permanently slow if the player moves away.")]
        [SerializeField, Min(0f)] private float minimumAttractionVelocity = 3f;
        [Tooltip("Instantaneous mass-independent launch speed applied when attraction begins.")]
        [SerializeField, Min(0f)] private float initialAttractionVelocity = 18f;
        [Tooltip("Objects at this starting distance or farther use full attraction velocity. Closer objects are proportionally slower.")]
        [SerializeField, Min(0.01f)] private float fullAttractionVelocityDistance = 8f;
        [Tooltip("Lower speed the object settles toward after the initial launch.")]
        [SerializeField, Min(0f)] private float attractionCruiseVelocity = 9f;
        [Tooltip("How quickly attraction speed adapts when the object's distance changes.")]
        [SerializeField, Min(0f)] private float attractionSlowdownRate = 10f;
        [Header("Attraction Arc")]
        [Tooltip("Where the arc reaches full height. Higher values keep the object rising for longer.")]
        [SerializeField, Range(0.05f, 0.95f)] private float arcPeakPosition = 0.6f;
        [SerializeField, Min(0f)] private float arcHeightPerMeter = 0.18f;
        [SerializeField, Min(0f)] private float minimumArcHeight = 0.35f;
        [SerializeField, Min(0f)] private float maximumArcHeight = 1.5f;
        [Header("Attraction Spin")]
        [Tooltip("Minimum random angular speed in radians per second while an object is being attracted.")]
        [SerializeField, Min(0f)] private float minimumAttractionSpin = 1f;
        [Tooltip("Maximum random angular speed in radians per second while an object is being attracted.")]
        [SerializeField, Min(0f)] private float maximumAttractionSpin = 4f;
        [SerializeField] private float restDistance = 0.8f;
        [SerializeField] private float holdLerpSpeed = 18f;
        [Tooltip("Spin speed (deg/sec) of the object while it orbits in the hand.")]
        [SerializeField] private float holdSpinSpeed = 90f;
        [Header("Hold Bob")]
        [Tooltip("Vertical bob height (metres) of the held object, so it reads as suspended in a field rather than pinned to a point. Per-object scaled by GloveHoldProfile.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float holdBobAmplitude = 0.04f;
        [Tooltip("Bob cycles per second of the held object.")]
        [Range(0f, 5f)]
        [SerializeField] private float holdBobFrequency = 0.8f;
        [Header("Secondary Attraction")]
        [Tooltip("Pull applied to non-primary consumables in reach. Scaled down by distance from the primary.")]
        [SerializeField, Min(0f)] private float secondaryAttractionForce = 12f;
        [Tooltip("Fraction of momentum kept by tugged non-primary objects once the pull ends. Lower = they settle faster instead of rushing the player.")]
        [SerializeField, Range(0f, 1f)] private float secondarySettleDamping = 0.25f;
        [Header("Dropping")]
        [SerializeField, Min(0f)] private float dropForwardOffset = 0.4f;
        [SerializeField, Min(0f)] private float dropForwardVelocity = 1.5f;
        [SerializeField, Range(0f, 1f)] private float playerVelocityInheritance = 1f;
        [Tooltip("How long a dropped object continues ignoring only the player's colliders.")]
        [SerializeField, Min(0f)] private float postDropCollisionDelay = 0.35f;
        [Header("Katana Launch")]
        [Tooltip("Speed the attracted object is hurled along the camera aim direction by the katana baseball interaction.")]
        [SerializeField, Min(0f)] private float katanaLaunchSpeed = 20f;
        [Tooltip("Random tumble applied to the launched object.")]
        [SerializeField, Min(0f)] private float katanaLaunchSpin = 6f;
        [Tooltip("Maximum distance from the glove rest point at which a perfectly timed parry can bat an attracting object.")]
        [SerializeField, Min(0f)] private float baseballBatDistance = 2f;
        [Tooltip("Brief grace period after an attracting object enters Hold during which a perfect parry can still bat it.")]
        [SerializeField, Min(0f)] private float baseballHoldForgivenessDuration = 0.25f;

        [Header("Physical Projectile Aiming")]
        [Tooltip("Maximum distance checked from the camera center when aiming a launched held object.")]
        [SerializeField, Min(0.1f)] private float physicalProjectileAimDistance = 100f;
        [Tooltip("Layers the camera-center aim ray can target.")]
        [SerializeField] private LayerMask physicalProjectileAimLayers = ~0;

        [Header("Charge Blast")]
        [Range(0f, 1f)][SerializeField] private float minChargeSpeed = 0.45f; // player speed scale at full charge
        [Tooltip("Optional. If empty, a runtime emissive sphere is spawned so the blast is testable without art.")]
        [SerializeField] private GloveBlastProjectile blastPrefab;
        [Tooltip("Launch speed of the plasma blast along the camera forward.")]
        [SerializeField] private float blastSpeed = 45f;
        [SerializeField] private float blastMinDamage = 8f;
        [SerializeField] private float blastMaxDamage = 40f;
        [SerializeField] private float blastMinForce = 6f;
        [SerializeField] private float blastMaxForce = 30f;
        [Tooltip("Camera kick on fire. Auto-fetched from this object if left empty.")]
        [SerializeField] private CinemachineImpulseSource blastShake;
        [SerializeField] private float blastMinShake = 0.4f;
        [SerializeField] private float blastMaxShake = 1.4f;

        [Header("Attract / Repulse Shake")]
        [Tooltip("Camera kick when an object latches onto the pull or is hurled off the glove. Auto-fetched from this object if left empty.")]
        [SerializeField] private CinemachineImpulseSource attractShake;
        [Tooltip("Shake for the lightest objects (Playground: ammo ~0.5 mass).")]
        [SerializeField] private float minShake = 0.35f;
        [Tooltip("Shake cap. Objects at or above Shake Mass For Max use this, so huge objects don't over-shake.")]
        [SerializeField] private float maxShake = 1.3f;
        [Tooltip("Object mass that reaches the maximum shake. Anything heavier is clamped to Max Shake (Playground: barrel = 6).")]
        [SerializeField, Min(0.01f)] private float shakeMassForMax = 6f;
        public bool CanAttractObject { get { return canAttract; } set { canAttract = value; } }
        public bool EnterAttract { get { return enterAttract; } set { enterAttract = value; } }
        public bool EnterCharge { get { return enterCharge; } set { enterCharge = value; } }
        // ── Exposed to states ──
        public float EquipWaitTime => equipWaitTime;
        public float AttractionForce => attractionSteeringAcceleration;
        public float MinimumAttractionVelocity => minimumAttractionVelocity;
        public float InitialAttractionVelocity => initialAttractionVelocity;
        public float FullAttractionVelocityDistance => fullAttractionVelocityDistance;
        public float AttractionCruiseVelocity => attractionCruiseVelocity;
        public float AttractionSlowdownRate => attractionSlowdownRate;
        public float ArcPeakPosition => arcPeakPosition;
        public float ArcHeightPerMeter => arcHeightPerMeter;
        public float MinimumArcHeight => minimumArcHeight;
        public float MaximumArcHeight => maximumArcHeight;
        public float MinimumAttractionSpin => minimumAttractionSpin;
        public float MaximumAttractionSpin => maximumAttractionSpin;
        public float RestDistance => restDistance;
        public float HoldLerpSpeed => holdLerpSpeed;
        public float HoldSpinSpeed => holdSpinSpeed;
        public float HoldBobAmplitude => holdBobAmplitude;
        public float HoldBobFrequency => holdBobFrequency;
        public bool HasUsableHeldObject =>
            heldObject != null && heldObject.activeInHierarchy;
        // Read-only gates let Attract coordinate with the equipped katana
        // without making the glove itself an Arsenal WeaponMode.
        public bool KatanaEquipped => arsenal != null && arsenal.currentMode == Arsenal.WeaponMode.Katana;
        public bool KatanaPerfectParryActive => KatanaEquipped
            && arsenal.katanaStateMachine != null
            && arsenal.katanaStateMachine.isParryWindowActive;
        public float BaseballBatDistance => baseballBatDistance;
        public float BaseballHoldForgivenessDuration =>
            baseballHoldForgivenessDuration;
        // Raise progress of the arm-override layer, read by the Equipping / Unequipping action states.
        public bool GloveRaised => raiseAmount >= 0.99f;
        public bool GloveLowered => raiseAmount <= 0.01f;

        public GloveBaseState CurrentState;
        public GloveStateFactory states;

        // ── Wired refs ──
        private Camera playerCamera;
        private int gloveLayerIndex = -1;
        private float baseLeftHandWeight = 1f;
        private bool gloveEquipped;
        private bool unequippedAnimationPending;
        private bool gameplaySuppressed;

        private struct CollisionIgnoreRecord
        {
            public Collider heldCollider;
            public Collider playerCollider;
            public bool wasIgnored;
        }

        private readonly List<CollisionIgnoreRecord> heldPlayerCollisionIgnores = new();
        private readonly RaycastHit[] detectionHits = new RaycastHit[16];
        private readonly Collider[] secondaryBuffer = new Collider[16];
        // Every non-primary body tugged during the current attract, so their momentum can be bled
        // off cleanly when the pull ends instead of leaving them rushing the player.
        private readonly HashSet<Rigidbody> pulledSecondaryBodies = new();
        private readonly HashSet<int> warnedMissingAnimationStates = new();
        private readonly RaycastHit[] physicalProjectileAimHits = new RaycastHit[16];
        private Arsenal arsenal;
        private MaterialPropertyBlock detectionMpb;
        private Renderer tintedRenderer;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private Coroutine restorePlayerCollisionsCoroutine;
        private bool waitingToRestorePlayerCollisions;
        private Transform gloveParentedObject;
        private Transform heldObjectOriginalParent;
        private int heldObjectOriginalSiblingIndex;

        [HideInInspector] public int emptyHash, idleHash, chargeHash, attractHash, blastHash, blastProjectileHash;
        private int holdHash;
        private int largeHoldHash;

        private void Awake()
        {
            playerCamera = Camera.main;
            arsenal = GetComponent<Arsenal>();
            if (camStateMachine == null && player != null)
                camStateMachine = player.GetComponent<CamStateMachine>();
            detectionMpb = new MaterialPropertyBlock();
            if (blastShake == null)
                blastShake = GetComponent<CinemachineImpulseSource>();
            if (attractShake == null)
                attractShake = GetComponent<CinemachineImpulseSource>();
            emptyHash = Animator.StringToHash("empty_Glove");
            idleHash = Animator.StringToHash("idle_Glove");
            chargeHash = Animator.StringToHash("charge_blast_Glove");
            blastHash = Animator.StringToHash("blast_Glove");
            holdHash = Animator.StringToHash(smallHoldAnimationState);
            largeHoldHash = Animator.StringToHash(largeHoldAnimationState);
            attractHash = Animator.StringToHash("attract_Glove");
            blastProjectileHash = Animator.StringToHash("blast_projectile_Glove");

            InitializeArmOverride();

            states = new GloveStateFactory(this);
            CurrentState = states.Unequipped();
            CurrentState.EnterState();
        }

        private void Update()
        {
            if (gameplaySuppressed)
            {
                InputManager.Instance?.ConsumeGloveGestures();
                return;
            }

            // Refresh the target before states inspect this frame's input.
            canAttract = ObjectDetection();
            UpdateDetectionTint();
            CurrentState.UpdateStates();
            UpdateArmOverride();
            currentStateText = CurrentState.GetType().Name;
            currentSubstateText = CurrentState.CurrentSubstate()?.GetType().Name ?? "None";
            if (InputManager.Instance != null)
                InputManager.Instance.ConsumeGloveGestures();
        }

        private void FixedUpdate()
        {
            if (gameplaySuppressed)
                return;

            CurrentState?.FixedUpdateStates();
        }

        public void SetGameplaySuppressed(bool suppressed)
        {
            if (gameplaySuppressed == suppressed)
                return;

            gameplaySuppressed = suppressed;

            if (suppressed)
            {
                BlastWindow = false;
                canAttract = false;
                ClearChargeSlow();
                SettleSecondaryObjects();
                ReleaseHeldObject();
                ClearDetectionTint();
                InputManager.Instance?.ConsumeGloveGestures();

                if (armsAnimator != null && gloveLayerIndex >= 0)
                    armsAnimator.SetLayerWeight(gloveLayerIndex, 0f);

                if (leftHandIK != null)
                    leftHandIK.weight = baseLeftHandWeight;
            }
            else
            {
                if (armsAnimator != null && gloveLayerIndex >= 0)
                    armsAnimator.SetLayerWeight(gloveLayerIndex, raiseAmount);

                if (leftHandIK != null)
                    leftHandIK.weight = Mathf.Lerp(
                        baseLeftHandWeight,
                        0f,
                        raiseAmount);
            }
        }

        public void StartBlast()
        {
            BlastWindow = true;
        }

        public void EndBlast()
        {
            BlastWindow = false;
        }


        public bool ObjectDetection()
        {
            detectedObject = null;

            if (heldObject != null || waitingToRestorePlayerCollisions)
                return false;

            if (playerCamera == null)
                playerCamera = Camera.main;

            if (playerCamera == null)
                return false;

            Vector3 rayOrigin = playerCamera.transform.position;
            Vector3 rayDirection = playerCamera.transform.forward;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit directHit,
                detectDistance, attractableLayer, QueryTriggerInteraction.Collide))
            {
                detectedObject = GetDetectedObject(directHit);
                return detectedObject != null && heldObject == null;
            }


            int nearbyHitCount = Physics.SphereCastNonAlloc(
                rayOrigin,
                castRadius,
                rayDirection,
                detectionHits,
                detectDistance,
                attractableLayer,
                QueryTriggerInteraction.Collide);

            float bestAimDistanceSqr = float.PositiveInfinity;
            float bestForwardDistance = float.PositiveInfinity;

            for (int i = 0; i < nearbyHitCount; i++)
            {
                RaycastHit hit = detectionHits[i];
                GameObject candidate = GetDetectedObject(hit);
                if (candidate == null)
                    continue;

                Vector3 candidatePosition = hit.collider.bounds.center;
                Vector3 fromRayOrigin = candidatePosition - rayOrigin;
                float forwardDistance = Mathf.Clamp(
                    Vector3.Dot(fromRayOrigin, rayDirection), 0f, detectDistance);
                Vector3 closestPointOnRay = rayOrigin + rayDirection * forwardDistance;
                float aimDistanceSqr = (candidatePosition - closestPointOnRay).sqrMagnitude;

                if (aimDistanceSqr < bestAimDistanceSqr ||
                    (Mathf.Approximately(aimDistanceSqr, bestAimDistanceSqr) &&
                     forwardDistance < bestForwardDistance))
                {
                    bestAimDistanceSqr = aimDistanceSqr;
                    bestForwardDistance = forwardDistance;
                    detectedObject = candidate;
                }
            }

            return detectedObject != null && heldObject == null;
        }

        private static GameObject GetDetectedObject(RaycastHit hit)
        {
            if (hit.rigidbody != null)
                return hit.rigidbody.gameObject;

            return hit.collider != null ? hit.collider.gameObject : null;
        }

        // ── Detection tint ─────────────────────────────────────────
        // Non-destructive cyan tint on the primary target so the player can see which object will
        // latch. Uses a MaterialPropertyBlock (no material instancing) and is cleared the instant the
        // target changes, the object is grabbed, or the glove is put away.
        private void UpdateDetectionTint()
        {
            if (gloveEquipped && detectedObject != null && heldObject == null)
                ApplyDetectionTint(detectedObject);
            else
                ClearDetectionTint();
        }

        private void ApplyDetectionTint(GameObject target)
        {
            Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
            if (targetRenderer == null)
            {
                ClearDetectionTint();
                return;
            }

            if (targetRenderer == tintedRenderer)
                return;

            // A different object is now primary — drop the old one's tint first.
            ClearDetectionTint();

            Material material = targetRenderer.sharedMaterial;
            Color baseColor = Color.white;
            if (material != null)
            {
                if (material.HasProperty(BaseColorId)) baseColor = material.GetColor(BaseColorId);
                else if (material.HasProperty(ColorId)) baseColor = material.GetColor(ColorId);
            }

            Color tinted = Color.Lerp(baseColor, detectionTint, detectionTintStrength);
            detectionMpb.Clear();
            detectionMpb.SetColor(BaseColorId, tinted);
            detectionMpb.SetColor(ColorId, tinted);
            targetRenderer.SetPropertyBlock(detectionMpb);
            tintedRenderer = targetRenderer;
        }

        private void ClearDetectionTint()
        {
            if (tintedRenderer != null)
                tintedRenderer.SetPropertyBlock(null);

            tintedRenderer = null;
        }

        // ── Secondary attraction ───────────────────────────────────
        // While the primary object is being pulled in, give every other consumable in reach a gentler
        // tug toward the hand — weaker the farther it sits from the primary, so only one ever latches.
        // Called from GloveAttractState.FixedUpdateState so it runs on the physics clock.
        public void PullSecondaryObjects(GameObject primary, Vector3 towardPosition)
        {
            if (primary == null || secondaryAttractionForce <= 0f)
                return;

            Vector3 origin = playerCamera != null ? playerCamera.transform.position : transform.position;
            int count = Physics.OverlapSphereNonAlloc(
                origin, detectDistance, secondaryBuffer, attractableLayer,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Rigidbody body = secondaryBuffer[i] != null ? secondaryBuffer[i].attachedRigidbody : null;
                if (body == null)
                    continue;

                GameObject candidate = body.gameObject;
                if (candidate == primary || candidate == heldObject)
                    continue;

                // Nearer the primary → stronger pull; beyond reach → nothing.
                float distanceFromPrimary = Vector3.Distance(body.position, primary.transform.position);
                float weight = Mathf.Clamp01(1f - distanceFromPrimary / detectDistance);
                if (weight <= 0f)
                    continue;

                Vector3 pullDir = (towardPosition - body.position).normalized;
                body.AddForce(pullDir * (secondaryAttractionForce * weight), ForceMode.Acceleration);
                pulledSecondaryBodies.Add(body);
            }
        }

        /// <summary>
        /// Called when the attract pull ends (the primary latched, launched, or the pull was aborted).
        /// Bleeds the inward momentum off every non-primary object that was tugged in behind the primary
        /// so they settle in place instead of continuing to rush the player, then clears the tracking set.
        /// </summary>
        public void SettleSecondaryObjects()
        {
            foreach (Rigidbody body in pulledSecondaryBodies)
            {
                if (body == null)
                    continue;

                // The primary is handled by the attract/hold states themselves; never damp it here.
                if (heldObject != null && body.gameObject == heldObject)
                    continue;

                body.linearVelocity *= secondarySettleDamping;
            }

            pulledSecondaryBodies.Clear();
        }



        /// <summary>
        /// Crossfades to a glove state on the glove Animator layer. Existing
        /// states use the shared duration; a state may supply an override when a
        /// specific action needs a faster or slower blend.
        /// </summary>
        public void PlayGloveAnimation(
            int stateHash,
            float crossFadeDuration = -1f)
        {
            float duration = crossFadeDuration >= 0f
                ? crossFadeDuration
                : animationCrossFadeDuration;
            CrossFadeGloveAnimation(stateHash, duration);
        }

        public void PlayHoldAnimation(GloveHoldPose holdPose)
        {
            if (armsAnimator == null || gloveLayerIndex < 0)
                return;

            int requestedHash = holdPose == GloveHoldPose.Large
                ? largeHoldHash
                : holdHash;

            if (!armsAnimator.HasState(gloveLayerIndex, requestedHash))
                requestedHash = holdHash;

            CrossFadeGloveAnimation(
                requestedHash,
                animationCrossFadeDuration);
        }

        private bool CrossFadeGloveAnimation(
            int stateHash,
            float duration)
        {
            if (armsAnimator == null || gloveLayerIndex < 0)
                return false;

            if (!armsAnimator.HasState(gloveLayerIndex, stateHash))
            {
                if (warnedMissingAnimationStates.Add(stateHash))
                {
                    Debug.LogWarning(
                        $"[GloveStateMachine] Animator state hash "
                        + $"'{stateHash}' was not found on layer "
                        + $"'{gloveLayerName}'.",
                        this);
                }

                return false;
            }

            armsAnimator.CrossFadeInFixedTime(
                stateHash,
                Mathf.Max(0f, duration),
                gloveLayerIndex,
                0f);
            return true;
        }

        public void SetChargeSlow(float chargeNormalized)
        {
            if (player != null)
                player.ChargeSpeedMultiplier = Mathf.Lerp(1f, minChargeSpeed, chargeNormalized);
        }

        public void ClearChargeSlow()
        {
            if (player != null)
                player.ChargeSpeedMultiplier = 1f;
        }

        /// <summary>
        /// Fires the plasma blast for the charge branch. Called by <see cref="GloveBlastState"/> on enter.
        /// Origin and direction come from the player camera; damage and force (and, inside the projectile,
        /// the field radii) are scaled by how long the blast was charged. Falls back to a runtime plasma
        /// ball (see <see cref="GloveBlastVisual"/>) when no prefab is wired so the branch stays testable.
        /// </summary>
        public void SpawnBlast(float chargeNormalized)
        {
            chargeNormalized = Mathf.Clamp01(chargeNormalized);

            if (playerCamera == null)
                playerCamera = Camera.main;

            Vector3 dir = playerCamera != null ? playerCamera.transform.forward : transform.forward;
            Vector3 origin = (playerCamera != null ? playerCamera.transform.position : transform.position) + dir;

            GloveBlastProjectile projectile = blastPrefab != null
                ? Instantiate(blastPrefab, origin, Quaternion.LookRotation(dir))
                : GloveBlastVisual.Create(origin);

            float damage = Mathf.Lerp(blastMinDamage, blastMaxDamage, chargeNormalized);
            float force = Mathf.Lerp(blastMinForce, blastMaxForce, chargeNormalized);
            projectile.Launch(
                dir * blastSpeed,
                chargeNormalized,
                damage,
                force,
                player != null ? player.gameObject : gameObject);

            // Recoil-style kick back toward the player, harder the more the blast was charged.
            if (blastShake != null)
                blastShake.GenerateImpulse(-dir * Mathf.Lerp(blastMinShake, blastMaxShake, chargeNormalized));
        }

        /// <summary>
        /// Camera kick for the glove's attract and repulse actions, mirroring the gun's recoil shake so
        /// the glove reads with the same weight. Scaled by the object's mass and clamped so heavy objects
        /// hit hard without over-shaking. Called by <see cref="GloveAttractState"/> when a pull latches
        /// on, and by the throw/drop paths as an object leaves the glove.
        /// </summary>
        public void ShakeGlove(float mass)
        {
            if (attractShake)
            {
                float massScale = Mathf.Clamp01(mass / shakeMassForMax);
                float shake = Mathf.Lerp(minShake, maxShake, massScale);
                attractShake.GenerateImpulse(Camera.main.transform.forward * shake);
            }
        }

        public void PlayObjectCatchFovKick()
        {
            camStateMachine?.PlayObjectCatchFovKick();
        }

        public void IgnoreHeldObjectPlayerCollisions()
        {
            RestoreHeldObjectPlayerCollisions();

            if (heldObject == null)
                return;

            GameObject playerObject = player != null ? player.gameObject : gameObject;
            Collider[] heldColliders = heldObject.GetComponentsInChildren<Collider>(true);
            Collider[] playerColliders = playerObject.GetComponentsInChildren<Collider>(true);

            foreach (Collider heldCollider in heldColliders)
            {
                // Pickup and equipment triggers must remain active so pulling
                // an item to the player can still consume or equip it.
                if (heldCollider == null || heldCollider.isTrigger)
                    continue;

                foreach (Collider playerCollider in playerColliders)
                {
                    if (playerCollider == null
                        || playerCollider == heldCollider
                        || playerCollider.isTrigger)
                    {
                        continue;
                    }

                    bool wasIgnored = Physics.GetIgnoreCollision(heldCollider, playerCollider);
                    heldPlayerCollisionIgnores.Add(new CollisionIgnoreRecord
                    {
                        heldCollider = heldCollider,
                        playerCollider = playerCollider,
                        wasIgnored = wasIgnored
                    });

                    if (!wasIgnored)
                        Physics.IgnoreCollision(heldCollider, playerCollider, true);
                }
            }
        }

        public void RestoreHeldObjectPlayerCollisions()
        {
            foreach (CollisionIgnoreRecord record in heldPlayerCollisionIgnores)
            {
                if (record.heldCollider != null && record.playerCollider != null)
                {
                    Physics.IgnoreCollision(record.heldCollider, record.playerCollider,
                        record.wasIgnored);
                }
            }

            heldPlayerCollisionIgnores.Clear();
        }

        /// <summary>
        /// Parents the held object to the animated glove anchor while preserving
        /// the hierarchy it occupied before the glove took ownership.
        /// Attraction remains unparented and physics-driven.
        /// </summary>
        public void AttachHeldObjectToGlove()
        {
            RestoreHeldObjectHierarchy();

            if (heldObject == null || restPos == null)
                return;

            gloveParentedObject = heldObject.transform;
            heldObjectOriginalParent = gloveParentedObject.parent;
            heldObjectOriginalSiblingIndex =
                gloveParentedObject.GetSiblingIndex();
            gloveParentedObject.SetParent(restPos.transform, true);
        }

        /// <summary>
        /// Returns the glove-parented object to its original hierarchy without
        /// changing its current world position or rotation.
        /// </summary>
        public void RestoreHeldObjectHierarchy()
        {
            Transform objectToRestore = gloveParentedObject;
            Transform originalParent = heldObjectOriginalParent;
            int originalSiblingIndex = heldObjectOriginalSiblingIndex;

            gloveParentedObject = null;
            heldObjectOriginalParent = null;
            heldObjectOriginalSiblingIndex = 0;

            if (objectToRestore == null)
                return;

            objectToRestore.SetParent(originalParent, true);
            objectToRestore.SetSiblingIndex(originalSiblingIndex);
        }

        /// <summary>
        /// Clears glove ownership immediately when another system breaks,
        /// disables, or destroys the held object. Unlike a normal drop, there
        /// is no collision-delay window because the object is no longer usable.
        /// </summary>
        public void ClearLostHeldObject()
        {
            RestoreHeldObjectHierarchy();
            heldObject = null;
            RestoreHeldObjectPlayerCollisions();
        }

        public void DropHeldObjectWithCollisionDelay()
        {
            RestoreHeldObjectHierarchy();
            heldObject = null;

            if (restorePlayerCollisionsCoroutine != null)
                StopCoroutine(restorePlayerCollisionsCoroutine);

            if (postDropCollisionDelay <= 0f)
            {
                waitingToRestorePlayerCollisions = false;
                RestoreHeldObjectPlayerCollisions();
                restorePlayerCollisionsCoroutine = null;
                return;
            }

            waitingToRestorePlayerCollisions = true;
            restorePlayerCollisionsCoroutine = StartCoroutine(RestorePlayerCollisionsAfterDelay());
        }

        public void ReleaseHeldObject()
        {
            RestoreHeldObjectHierarchy();

            if (heldObject != null)
            {
                Rigidbody heldBody = heldObject.GetComponent<Rigidbody>();
                if (heldBody != null)
                {
                    if (playerCamera == null)
                        playerCamera = Camera.main;

                    Vector3 forward = playerCamera != null
                        ? playerCamera.transform.forward
                        : transform.forward;
                    Vector3 releaseOrigin = restPos != null
                        ? restPos.transform.position
                        : heldBody.position;
                    Vector3 playerVelocity = player != null && player.rb != null
                        ? player.rb.linearVelocity
                        : Vector3.zero;

                    heldBody.position = releaseOrigin + forward * dropForwardOffset;
                    heldBody.linearVelocity =
                        playerVelocity * playerVelocityInheritance +
                        forward * dropForwardVelocity;
                    heldBody.angularVelocity = Vector3.zero;
                }
            }

            // A gentle drop is deliberately silent — only catching and throwing kick the camera.
            DropHeldObjectWithCollisionDelay();
        }

        /// <summary>
        /// Hurls the attracted/held object hard along the camera forward — the katana-slash launch.
        /// Distinct from <see cref="ReleaseHeldObject"/>'s gentle drop; this is a deliberate throw.
        /// </summary>
        public void LaunchHeldObjectForward()
        {
            LaunchHeldObject();
        }

        /// <summary>
        /// Baseball-launches the glove's attracted object using the glove's
        /// authored launch speed and the player's camera aim. This deliberately
        /// does not share enemy-projectile perfect-parry return rules.
        /// </summary>
        public void LaunchHeldObjectAsBaseball()
        {
            LaunchHeldObject();
        }

        private void LaunchHeldObject()
        {
            RestoreHeldObjectHierarchy();

            if (heldObject != null)
            {
                Rigidbody heldBody = heldObject.GetComponent<Rigidbody>();
                if (heldBody != null)
                    LaunchGloveControlledRigidbody(heldBody);
            }

            DropHeldObjectWithCollisionDelay();
        }

        /// <summary>
        /// Launches the Rigidbody currently owned by the glove. No damage-source
        /// or enemy-instigator data is required for the physical launch.
        /// </summary>
        private bool LaunchGloveControlledRigidbody(Rigidbody body)
        {
            if (body == null)
                return false;

            if (playerCamera == null)
                playerCamera = Camera.main;

            Vector3 launchDirection = GetPhysicalProjectileAimDirection(
                body.worldCenterOfMass,
                body.gameObject);

            body.isKinematic = false;
            CurvedRigidbodyFlight existingFlight =
                body.GetComponent<CurvedRigidbodyFlight>();
            if (existingFlight != null)
                existingFlight.CancelFlight();

            if (katanaLaunchSpeed <= Mathf.Epsilon
                || launchDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            body.linearVelocity = launchDirection * katanaLaunchSpeed;
            body.angularVelocity = Random.insideUnitSphere * katanaLaunchSpin;

            VelocityImpactDamageSource impactDamageSource =
                body.GetComponent<VelocityImpactDamageSource>();
            if (impactDamageSource != null)
            {
                GameObject thrower = player != null
                    ? player.gameObject
                    : gameObject;
                impactDamageSource.Arm(thrower);
            }

            ShakeGlove(body.mass);
            return true;
        }

        /// <summary>
        /// True while this collider belongs to the primary object currently
        /// being attracted or held by the glove. Katana slash detection uses
        /// this to avoid damaging the player's own suspended projectile.
        /// </summary>
        public bool OwnsControlledObject(Collider candidate)
        {
            if (heldObject == null || candidate == null)
                return false;

            if (candidate.attachedRigidbody != null
                && candidate.attachedRigidbody.gameObject == heldObject)
            {
                return true;
            }

            Transform heldTransform = heldObject.transform;
            return candidate.transform == heldTransform
                || candidate.transform.IsChildOf(heldTransform);
        }

        private Vector3 GetPhysicalProjectileAimDirection(
            Vector3 projectileOrigin,
            GameObject projectileObject)
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            Vector3 rayOrigin = playerCamera != null
                ? playerCamera.transform.position
                : transform.position;
            Vector3 rayDirection = playerCamera != null
                ? playerCamera.transform.forward
                : transform.forward;
            Vector3 aimPoint = rayOrigin
                + rayDirection * physicalProjectileAimDistance;

            int hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                rayDirection,
                physicalProjectileAimHits,
                physicalProjectileAimDistance,
                physicalProjectileAimLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = physicalProjectileAimHits[i];
                if (hit.collider == null
                    || IsPhysicalProjectileAimIgnored(
                        hit.collider,
                        projectileObject)
                    || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                aimPoint = hit.point;
            }

            Vector3 towardAimPoint = aimPoint - projectileOrigin;
            return towardAimPoint.sqrMagnitude > Mathf.Epsilon
                ? towardAimPoint.normalized
                : rayDirection;
        }

        private bool IsPhysicalProjectileAimIgnored(
            Collider candidate,
            GameObject projectileObject)
        {
            if (candidate == null)
                return true;

            if (IsColliderPartOf(candidate, projectileObject)
                || IsColliderPartOf(candidate, gameObject))
            {
                return true;
            }

            return player != null
                && IsColliderPartOf(candidate, player.gameObject);
        }

        private static bool IsColliderPartOf(
            Collider candidate,
            GameObject root)
        {
            return root != null
                && (candidate.gameObject == root
                    || candidate.transform.IsChildOf(root.transform));
        }

        private IEnumerator RestorePlayerCollisionsAfterDelay()
        {
            yield return new WaitForSeconds(postDropCollisionDelay);
            RestoreHeldObjectPlayerCollisions();
            waitingToRestorePlayerCollisions = false;
            restorePlayerCollisionsCoroutine = null;
        }

        private void InitializeArmOverride()
        {
            if (armsAnimator == null)
            {
                Debug.LogWarning("[GloveStateMachine] No Arms Animator assigned.");
                return;
            }

            gloveLayerIndex = armsAnimator.GetLayerIndex(gloveLayerName);
            if (gloveLayerIndex < 0)
            {
                Debug.LogWarning($"[GloveStateMachine] Animator layer '{gloveLayerName}' was not found.");
            }

            if (leftHandIK == null)
            {
                TwoBoneIKConstraint[] constraints =
                    armsAnimator.GetComponentsInChildren<TwoBoneIKConstraint>(true);

                foreach (TwoBoneIKConstraint constraint in constraints)
                {
                    if (constraint.gameObject.name == "LeftHandIK")
                    {
                        leftHandIK = constraint;
                        break;
                    }
                }
            }

            if (leftHandIK != null)
                baseLeftHandWeight = leftHandIK.weight;
            else
                Debug.LogWarning("[GloveStateMachine] LeftHandIK constraint was not found.");
        }

        private void UpdateArmOverride()
        {
            float target = gloveEquipped ? 1f : 0f;
            raiseAmount = Mathf.MoveTowards(raiseAmount, target, raiseSpeed * Time.deltaTime);

            if (armsAnimator != null && gloveLayerIndex >= 0)
                armsAnimator.SetLayerWeight(gloveLayerIndex, raiseAmount);

            if (leftHandIK != null)
                leftHandIK.weight = Mathf.Lerp(baseLeftHandWeight, 0f, raiseAmount);

            // Keep the last glove pose active while its layer fades out. Resetting to the empty
            // state any earlier would remove the override immediately and make the hand snap back.
            if (unequippedAnimationPending && raiseAmount <= 0f)
            {
                PlayGloveAnimation(emptyHash);
                unequippedAnimationPending = false;
            }
        }

        public void SetEquipped(bool equipped)
        {
            gloveEquipped = equipped;
            unequippedAnimationPending = !equipped;

            if (InputManager.Instance != null)
                InputManager.Instance.gloveEquipped = equipped;
        }

        private void OnDisable()
        {
            RestoreHeldObjectHierarchy();

            if (restorePlayerCollisionsCoroutine != null)
            {
                StopCoroutine(restorePlayerCollisionsCoroutine);
                restorePlayerCollisionsCoroutine = null;
            }

            waitingToRestorePlayerCollisions = false;
            RestoreHeldObjectPlayerCollisions();
            ClearDetectionTint();
        }
    }
}
