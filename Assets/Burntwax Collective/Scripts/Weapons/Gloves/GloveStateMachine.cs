using System.Collections;
using System.Collections.Generic;
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
        public bool BlastEndAnim = false;

        [SerializeField] private bool enterAttract = false;
        [SerializeField] private bool enterCharge = false;

        [Header("Detection")]
        public GameObject detectedObject;
        [SerializeField] private bool canAttract = false;
        [SerializeField] private float detectDistance = 9f;        // max reach of the glove
        [SerializeField] private float castRadius = 0.6f;          // spherecast radius = aim margin of error
        [SerializeField] private LayerMask attractableLayer;       // set to the Consumable layer

        [Header("Holding")]
        public GameObject heldObject;
        public GameObject restPos;
        [Header("Attraction")]
        [SerializeField, Min(0f)] private float attractionForce = 35f;
        [SerializeField] private float restDistance = 0.8f;
        [SerializeField] private float holdLerpSpeed = 18f;
        [Tooltip("How long a dropped object continues ignoring only the player's colliders.")]
        [SerializeField, Min(0f)] private float postDropCollisionDelay = 0.35f;

        [Header("Charge Blast")]
        [Range(0f, 1f)][SerializeField] private float minChargeSpeed = 0.45f; // player speed scale at full charge
        public bool CanAttractObject { get { return canAttract; } set { canAttract = value; } }
        public bool EnterAttract { get { return enterAttract; } set { enterAttract = value; } }
        public bool EnterCharge { get { return enterCharge; } set { enterCharge = value; } }
        // ── Exposed to states ──
        public float EquipWaitTime => equipWaitTime;
        public float AttractionForce => attractionForce;
        public float RestDistance => restDistance;
        public float HoldLerpSpeed => holdLerpSpeed;

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
        private Coroutine restorePlayerCollisionsCoroutine;
        private bool waitingToRestorePlayerCollisions;

        public int emptyHash, idleHash, chargeHash, attractHash, blastHash, blastProjectileHash, holdHash;

        private void Awake()
        {
            playerCamera = Camera.main;
            emptyHash = Animator.StringToHash("empty_Glove");
            idleHash = Animator.StringToHash("idle_Glove");
            chargeHash = Animator.StringToHash("charge_blast_Glove");
            blastHash = Animator.StringToHash("blast_Glove");
            holdHash = Animator.StringToHash("hold_Glove");
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

        public void BlastFinished()
        {
            BlastEndAnim = true;
        }



        public void PlayGloveAnimation(int stateHash)
        {
            if (armsAnimator != null && gloveLayerIndex >= 0)
                armsAnimator.Play(stateHash, gloveLayerIndex, 0f);
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
        }
    }
}
