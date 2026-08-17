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
        [SerializeField] private Animator armsAnimator;
        [SerializeField] private TwoBoneIKConstraint leftHandIK;

        [Header("Arm Override")]
        [SerializeField] private string gloveLayerName = "Glove";
        [Tooltip("Raise/lower speed in normalized layer weight per second.")]
        [SerializeField] private float raiseSpeed = 6f;
        [Range(0f, 1f)]
        [SerializeField] private float raiseAmount;

        [Header("States")]
        [SerializeField] private float equipWaitTime = 1f;
        public bool blastWindow = true;

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
        [Header("Secondary Attraction")]
        [Tooltip("Pull applied to non-primary consumables in reach. Scaled down by distance from the primary.")]
        [SerializeField, Min(0f)] private float secondaryAttractionForce = 12f;
        [Header("Dropping")]
        [SerializeField, Min(0f)] private float dropForwardOffset = 0.4f;
        [SerializeField, Min(0f)] private float dropForwardVelocity = 1.5f;
        [SerializeField, Range(0f, 1f)] private float playerVelocityInheritance = 1f;
        [Tooltip("How long a dropped object continues ignoring only the player's colliders.")]
        [SerializeField, Min(0f)] private float postDropCollisionDelay = 0.35f;
        [Header("Katana Launch")]
        [Tooltip("Speed the attracted object is hurled along the camera forward when you click with the katana equipped.")]
        [SerializeField, Min(0f)] private float katanaLaunchSpeed = 20f;
        [Tooltip("Random tumble applied to the launched object.")]
        [SerializeField, Min(0f)] private float katanaLaunchSpin = 6f;

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
        // Read-only gate: lets Attract/Hold offer the katana-slash early launch without the glove
        // being a WeaponMode. The glove still lives alongside the Arsenal, it just reads its mode.
        public bool KatanaEquipped => arsenal != null && arsenal.currentMode == Arsenal.WeaponMode.Katana;
        public bool KatanaParrying => KatanaEquipped
            && arsenal.katanaStateMachine != null
            && arsenal.katanaStateMachine.isParrying;
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

        private struct CollisionIgnoreRecord
        {
            public Collider heldCollider;
            public Collider playerCollider;
            public bool wasIgnored;
        }

        private readonly List<CollisionIgnoreRecord> heldPlayerCollisionIgnores = new();
        private readonly RaycastHit[] detectionHits = new RaycastHit[16];
        private readonly Collider[] secondaryBuffer = new Collider[16];
        private readonly RaycastHit[] physicalProjectileAimHits = new RaycastHit[16];
        private Arsenal arsenal;
        private MaterialPropertyBlock detectionMpb;
        private Renderer tintedRenderer;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private Coroutine restorePlayerCollisionsCoroutine;
        private bool waitingToRestorePlayerCollisions;

        [HideInInspector] public int emptyHash, idleHash, chargeHash, attractHash, blastHash, blastProjectileHash, holdHash;
        private int largeHoldHash;

        private void Awake()
        {
            playerCamera = Camera.main;
            arsenal = GetComponent<Arsenal>();
            detectionMpb = new MaterialPropertyBlock();
            if (blastShake == null)
                blastShake = GetComponent<CinemachineImpulseSource>();
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
            CurrentState?.FixedUpdateStates();
        }

        public void StartBlast()
        {
            blastWindow = true;
        }

        public void EndBlast()
        {
            blastWindow = false;
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
            }
        }



        public void PlayGloveAnimation(int stateHash)
        {
            if (armsAnimator != null && gloveLayerIndex >= 0)
                armsAnimator.Play(stateHash, gloveLayerIndex, 0f);
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

            armsAnimator.Play(requestedHash, gloveLayerIndex, 0f);
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
                // Trigger contacts must stay active so pickups and other gameplay volumes can
                // react while the object's solid geometry passes harmlessly through the player.
                if (heldCollider == null || heldCollider.isTrigger)
                    continue;

                foreach (Collider playerCollider in playerColliders)
                {
                    if (playerCollider == null || playerCollider == heldCollider ||
                        playerCollider.isTrigger)
                        continue;

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

        public void DropHeldObjectWithCollisionDelay()
        {
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

            DropHeldObjectWithCollisionDelay();
        }

        /// <summary>
        /// Hurls the attracted/held object hard along the camera forward — the katana-slash launch.
        /// Distinct from <see cref="ReleaseHeldObject"/>'s gentle drop; this is a deliberate throw.
        /// </summary>
        public void LaunchHeldObjectForward()
        {
            if (heldObject != null)
            {
                Rigidbody heldBody = heldObject.GetComponent<Rigidbody>();
                if (heldBody != null)
                {
                    if (playerCamera == null)
                        playerCamera = Camera.main;

                    Vector3 launchDirection = GetPhysicalProjectileAimDirection(
                        heldBody.worldCenterOfMass);

                    // Attracting keeps the body dynamic; a held body is kinematic — make sure it's
                    // dynamic before we hand it a velocity.
                    heldBody.isKinematic = false;
                    heldBody.linearVelocity = launchDirection * katanaLaunchSpeed;
                    heldBody.angularVelocity = Random.insideUnitSphere * katanaLaunchSpin;

                    // Only a deliberate high-speed launch becomes an attack. Objects without
                    // the opt-in damage source remain ordinary physics props.
                    VelocityImpactDamageSource impactDamageSource =
                        heldObject.GetComponent<VelocityImpactDamageSource>();
                    if (impactDamageSource != null)
                    {
                        GameObject thrower = player != null
                            ? player.gameObject
                            : gameObject;
                        impactDamageSource.Arm(thrower);
                    }
                }
            }

            DropHeldObjectWithCollisionDelay();
        }

        private Vector3 GetPhysicalProjectileAimDirection(
            Vector3 projectileOrigin)
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
                    || IsPhysicalProjectileAimIgnored(hit.collider)
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

        private bool IsPhysicalProjectileAimIgnored(Collider candidate)
        {
            if (candidate == null)
                return true;

            if (IsColliderPartOf(candidate, heldObject)
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
