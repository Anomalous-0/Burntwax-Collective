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
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject gloveModel;
        [SerializeField] private Rig handIK;
        [Tooltip("Where an attracted object flies to and orbits. Auto-created under the glove model if left empty.")]
        [SerializeField] private Transform gloveHandAnchor;
        [Tooltip("The Arms animator that owns the masked Glove layer (same one GloveController drives). " +
                 "Used to switch the raised glove between palm-up idle and the stop-sign action pose.")]
        [SerializeField] private Animator armsAnimator;
        [SerializeField] private string stopSignParam = "stopSign";
        [SerializeField] private CinemachineImpulseSource camShake;

        [Header("Detection")]
        [SerializeField] private float detectDistance = 9f;        // max reach of the glove
        [SerializeField] private float castRadius = 0.6f;          // spherecast radius = aim margin of error
        [SerializeField] private LayerMask attractableLayer;       // set to the Consumable layer

        [Header("Highlight")]
        [SerializeField] private Color attractTint = Color.cyan;
        [Range(0f, 1f)][SerializeField] private float tintStrength = 0.5f;

        [Header("Attract Physics")]
        [Tooltip("Tractor-beam top speed when the object is far from the hand.")]
        [SerializeField] private float maxApproachSpeed = 14f;
        [Tooltip("Within this distance of the hand the object eases its speed down, so it doesn't whip " +
                 "past and there's a slow window a katana swing can catch.")]
        [SerializeField] private float approachSlowRadius = 2.5f;
        [Tooltip("Approach speed never eases below this — keeps the object catching the hand even when " +
                 "you walk/sprint backward (should stay above the player's move speed).")]
        [SerializeField] private float minApproachSpeed = 8f;
        [SerializeField] private float latchDistance = 0.4f;       // object within this of the anchor = latched
        [Tooltip("Extra gap kept between the held object's surface and the palm so it never clips through.")]
        [SerializeField] private float palmClearance = 0.05f;
        [Tooltip("Peak tumble speed (deg/sec) on each axis while the object hovers in the palm. 0 = still.")]
        [SerializeField] private float spinMaxSpeed = 140f;
        [Tooltip("How fast the per-axis tumble speeds drift/change over time (higher = more restless).")]
        [SerializeField] private float spinVariation = 0.4f;
        [Tooltip("Leave (0,0,0) to hold the object exactly at the anchor (place the anchor in front of " +
                 "the palm). Set a local-space direction to push it out along that axis; it's auto-" +
                 "extended to clear the object's size + Palm Clearance.")]
        [SerializeField] private Vector3 heldLocalOffset = Vector3.zero;
        [SerializeField] private float launchImpulse = 25f;        // outward throw impulse
        [Tooltip("A katana swing can only launch the attracted object once it's within this distance of " +
                 "the hand — stops players spamming attract-from-afar + katana fling.")]
        [SerializeField] private float katanaLaunchDistance = 2.5f;

        [Header("Charge Blast")]
        [Range(0f, 1f)][SerializeField] private float minChargeSpeed = 0.45f; // player speed scale at full charge
        [Tooltip("Optional. If empty, a placeholder emissive sphere is spawned at runtime.")]
        [SerializeField] private GloveBlastProjectile blastPrefab;
        [SerializeField] private float blastSpeed = 45f;
        [SerializeField] private float blastMinDamage = 8f;
        [SerializeField] private float blastMaxDamage = 40f;
        [SerializeField] private float blastMinForce = 6f;
        [SerializeField] private float blastMaxForce = 30f;

        [Header("State Timing")]
        [SerializeField] private float equipWaitTime = 0.05f;
        [SerializeField] private float substateWaitTime = 0.05f;

        // ── Legacy runtime flags kept for save/anim hooks ──
        public bool FullyEquipped { get; private set; }
        public bool IsFiring { get; private set; }
        public bool IsReloading { get; private set; }

        // ── Exposed to states ──
        public float DetectDistance => detectDistance;
        public LayerMask AttractableLayer => attractableLayer;
        public bool CanAttractObject => detectedPrimary != null;
        public bool HasAttractedObject => primaryTarget != null;
        public float EquipWaitTime => equipWaitTime;
        public float SubstateWaitTime => substateWaitTime;
        public bool KatanaEquipped => arsenal != null && arsenal.currentMode == Arsenal.WeaponMode.Katana;

        public GloveBaseState CurrentState;
        public GloveStateFactory states;

        // ── Wired refs ──
        private Arsenal arsenal;
        private PlayerStateMachine playerMovement;
        private GunStateMachine gunStateMachine;
        private KatanaStateMachine katanaStateMachine;
        private int stopSignHash;

        // ── Detection / highlight state ──
        private Rigidbody detectedPrimary;               // best candidate while idle
        private Renderer tintedRenderer;
        private MaterialPropertyBlock mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // ── Active attract (exactly one object at a time) ──
        private class Attracted
        {
            public Rigidbody rb;
            public bool wasKinematic;
            public bool wasUsingGravity;
            public bool wasDetectingCollisions;
            public Transform originalParent;
            public MonoBehaviour[] suppressedPickups;
        }
        private Rigidbody primaryTarget;                 // the one and only object being attracted
        private Attracted primaryRecord;                 // its cached original physics state
        private bool latched;
        private float spinSeed;                          // per-object offset so each tumbles uniquely
        private Vector3 heldLocalPos;                    // computed hold position (clears the palm by the object's size)

        private Camera Cam => Camera.main;

        private void Awake()
        {
            states = new GloveStateFactory(this);
            CurrentState = states.Unequipped();
            CurrentState.EnterState();

            arsenal = GetComponent<Arsenal>();
            gunStateMachine = GetComponent<GunStateMachine>();
            katanaStateMachine = GetComponent<KatanaStateMachine>();
            if (player != null) playerMovement = player.GetComponent<PlayerStateMachine>();

            mpb = new MaterialPropertyBlock();
            stopSignHash = Animator.StringToHash(stopSignParam);

            if (gloveHandAnchor == null)
            {
                var anchorGo = new GameObject("GloveHandAnchor");
                anchorGo.transform.SetParent(gloveModel != null ? gloveModel.transform : transform, false);
                anchorGo.transform.localPosition = new Vector3(-0.2f, -0.1f, 0.5f); // placeholder in front of hand
                gloveHandAnchor = anchorGo.transform;
            }
        }

        private void Update()
        {
            CurrentState.UpdateStates();
            currentStateText = CurrentState.GetType().Name;
            currentSubstateText = CurrentState.CurrentSubstate()?.GetType().Name ?? "None";

            // Every glove gesture is consumed within this frame's state processing above, so clear
            // the one-frame pulses now.
            if (InputManager.Instance != null)
                InputManager.Instance.ConsumeGloveGestures();
        }

        // ── Equip publishing ─────────────────────────────────────
        public void SetEquipped(bool equipped)
        {
            if (InputManager.Instance != null)
                InputManager.Instance.gloveEquipped = equipped;

            if (!equipped)
            {
                ClearDetection();
                DropAttract();
                SetStopSign(false);
                ClearChargeSlow();
            }
        }

        public void SetStopSign(bool on)
        {
            if (armsAnimator != null)
                armsAnimator.SetBool(stopSignHash, on);
        }

        // ── Detection + highlight (used by Idle) ─────────────────
        public void RunDetection()
        {
            Rigidbody best = null;
            float bestDist = float.MaxValue;

            if (Cam != null)
            {
                var hits = Physics.SphereCastAll(Cam.transform.position, castRadius,
                    Cam.transform.forward, detectDistance, attractableLayer, QueryTriggerInteraction.Collide);

                foreach (var h in hits)
                {
                    Rigidbody rb = h.rigidbody != null ? h.rigidbody : h.collider.attachedRigidbody;
                    if (rb == null) continue;
                    // Nearest along the sweep is the primary. (distance 0 = started inside; treat as nearest)
                    float d = h.distance <= 0f ? 0f : h.distance;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = rb;
                    }
                }
            }

            detectedPrimary = best;
            ApplyTint(best != null ? best.GetComponentInChildren<Renderer>() : null);
        }

        public void ClearDetection()
        {
            detectedPrimary = null;
            ApplyTint(null);
        }

        private void ApplyTint(Renderer target)
        {
            if (target == tintedRenderer) return;

            // Restore the previously tinted renderer.
            if (tintedRenderer != null)
            {
                tintedRenderer.GetPropertyBlock(mpb);
                mpb.Clear();
                tintedRenderer.SetPropertyBlock(mpb);
            }

            tintedRenderer = target;

            if (tintedRenderer != null)
            {
                Color baseCol = Color.white;
                var sm = tintedRenderer.sharedMaterial;
                if (sm != null)
                {
                    if (sm.HasProperty(BaseColorId)) baseCol = sm.GetColor(BaseColorId);
                    else if (sm.HasProperty(ColorId)) baseCol = sm.GetColor(ColorId);
                }
                Color tinted = Color.Lerp(baseCol, attractTint, tintStrength);

                tintedRenderer.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, tinted);
                mpb.SetColor(ColorId, tinted);
                tintedRenderer.SetPropertyBlock(mpb);
            }
        }

        // ── Attract lifecycle (used by Attract / Hold / ReleaseObject) ──
        // Exactly ONE object is attracted: the tinted target captured at the moment of the double-tap.
        public void BeginAttract()
        {
            DropAttract();
            latched = false;
            spinSeed = Random.Range(0f, 100f);
            primaryTarget = detectedPrimary;
            if (primaryTarget != null)
                primaryRecord = CapturePrimary(primaryTarget);

            // We've committed the target; stop tinting the idle candidate.
            ClearDetection();
        }

        private Attracted CapturePrimary(Rigidbody rb)
        {
            var a = new Attracted
            {
                rb = rb,
                wasKinematic = rb.isKinematic,
                wasUsingGravity = rb.useGravity,
                wasDetectingCollisions = rb.detectCollisions,
                originalParent = rb.transform.parent,
                suppressedPickups = SuppressPickups(rb.gameObject, true)
            };
            rb.useGravity = false;
            // While it's flying to / orbiting the hand it must not bonk the world or shove the player.
            rb.detectCollisions = false;
            return a;
        }

        /// <summary>Eases the object toward the hand. Returns true once it has latched.</summary>
        public bool TickAttract()
        {
            if (primaryTarget == null || gloveHandAnchor == null) return false;

            Vector3 anchor = gloveHandAnchor.position;
            Vector3 toAnchor = anchor - primaryTarget.position;
            float dist = toAnchor.magnitude;

            if (!primaryTarget.isKinematic && dist > 0.0001f)
            {
                // Tractor-beam ease-in: full speed far away, easing down as it nears the hand so it
                // doesn't whip past — but never below minApproachSpeed, so it always out-paces the
                // player (e.g. back-pedalling) and actually reaches the hand to latch.
                float speed = Mathf.Max(minApproachSpeed, maxApproachSpeed * Mathf.Clamp01(dist / approachSlowRadius));
                primaryTarget.linearVelocity = (toAnchor / dist) * speed;
            }

            if (dist <= latchDistance)
            {
                latched = true;
                // Fully suspend physics and ride the palm: parent to the anchor so it tracks the hand
                // exactly (no inertia, no per-frame lag). Physics resumes only on release (repulsion).
                primaryTarget.linearVelocity = Vector3.zero;
                primaryTarget.angularVelocity = Vector3.zero;
                primaryTarget.isKinematic = true;
                primaryTarget.transform.SetParent(gloveHandAnchor, true);
                // Sit exactly where the anchor is placed (that's the "in front of the palm" spot you
                // set). Only if you give a Held Local Offset do we push along THAT direction, and then
                // auto-extend it to at least the object's radius + clearance so a big object can't clip
                // the palm. We never invent an outward direction ourselves (the rig's local axes vary).
                if (heldLocalOffset.sqrMagnitude < 1e-6f)
                {
                    heldLocalPos = Vector3.zero;
                }
                else
                {
                    float minDist = BoundingRadius(primaryTarget) + palmClearance;
                    heldLocalPos = heldLocalOffset.normalized * Mathf.Max(heldLocalOffset.magnitude, minDist);
                }
                primaryTarget.transform.localPosition = heldLocalPos;
                return true;
            }
            return false;
        }

        /// <summary>Holds the latched object pinned in front of the palm with a lazy 3-axis tumble.</summary>
        public void TickHold()
        {
            if (primaryTarget == null || gloveHandAnchor == null) return;

            // Parented to the anchor, so it already tracks the palm; keep it pinned clear of the palm.
            primaryTarget.transform.localPosition = heldLocalPos;

            if (spinMaxSpeed != 0f)
            {
                // Each axis gets its own Perlin-noise speed that smoothly drifts within
                // [-spinMaxSpeed, +spinMaxSpeed] over time, so the tumble looks organic and each
                // object (different spinSeed) spins differently.
                float t = Time.time * spinVariation;
                float sx = (Mathf.PerlinNoise(t, spinSeed + 0.0f) - 0.5f) * 2f;
                float sy = (Mathf.PerlinNoise(t, spinSeed + 5.3f) - 0.5f) * 2f;
                float sz = (Mathf.PerlinNoise(t, spinSeed + 11.7f) - 0.5f) * 2f;
                Vector3 spin = new Vector3(sx, sy, sz) * spinMaxSpeed;
                primaryTarget.transform.Rotate(spin * Time.deltaTime, Space.Self);
            }
        }

        /// <summary>True only while the attracted object is close enough to the hand for a katana launch.</summary>
        public bool KatanaLaunchInRange()
        {
            if (primaryTarget == null || gloveHandAnchor == null) return false;
            return Vector3.Distance(primaryTarget.position, gloveHandAnchor.position) <= katanaLaunchDistance;
        }

        /// <summary>Throws the attracted object out along the camera forward.</summary>
        public void LaunchAttractedObject()
        {
            if (primaryTarget != null && primaryRecord != null)
            {
                Vector3 dir = Cam != null ? Cam.transform.forward : transform.forward;
                RestoreAttracted(primaryRecord);
                primaryTarget.linearVelocity = Vector3.zero;   // drop the inward tractor velocity first
                primaryTarget.angularVelocity = Vector3.zero;
                primaryTarget.AddForce(dir * launchImpulse, ForceMode.Impulse);
            }
            primaryRecord = null;
            primaryTarget = null;
            latched = false;
        }

        // Releases the attracted object back to normal physics WITHOUT throwing it (used on unequip).
        private void DropAttract()
        {
            if (primaryRecord != null)
                RestoreAttracted(primaryRecord);
            primaryRecord = null;
            primaryTarget = null;
            latched = false;
        }

        private void RestoreAttracted(Attracted a)
        {
            if (a.rb == null) return;
            // Unparent first (back to its original parent) so physics resumes in world space.
            a.rb.transform.SetParent(a.originalParent, true);
            a.rb.isKinematic = a.wasKinematic;
            a.rb.useGravity = a.wasUsingGravity;
            a.rb.detectCollisions = a.wasDetectingCollisions;
            SuppressPickups(a.rb.gameObject, false, a.suppressedPickups);
        }

        // Half the object's bounding-box diagonal — a radius that covers it in ANY orientation, so a
        // tumbling held object never dips into the palm.
        private float BoundingRadius(Rigidbody rb)
        {
            Collider[] cols = rb.GetComponentsInChildren<Collider>();
            if (cols.Length > 0)
            {
                Bounds b = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
                return b.extents.magnitude;
            }
            Renderer rend = rb.GetComponentInChildren<Renderer>();
            return rend != null ? rend.bounds.extents.magnitude : 0.25f;
        }

        // Disable/re-enable *Pickup behaviours so an attracted consumable isn't auto-collected while
        // it's flying to / orbiting the hand. Returns the behaviours it disabled (to restore later).
        private MonoBehaviour[] SuppressPickups(GameObject go, bool suppress, MonoBehaviour[] known = null)
        {
            if (!suppress)
            {
                if (known != null)
                    foreach (var mb in known)
                        if (mb != null) mb.enabled = true;
                return null;
            }

            var list = new List<MonoBehaviour>();
            foreach (var mb in go.GetComponents<MonoBehaviour>())
            {
                if (mb != null && mb.enabled && mb.GetType().Name.EndsWith("Pickup"))
                {
                    mb.enabled = false;
                    list.Add(mb);
                }
            }
            return list.ToArray();
        }

        // ── Charge blast (used by ChargeBlast / Blast) ───────────
        public void SetChargeSlow(float chargeNormalized)
        {
            if (playerMovement != null)
                playerMovement.ChargeSpeedMultiplier = Mathf.Lerp(1f, minChargeSpeed, chargeNormalized);
        }

        public void ClearChargeSlow()
        {
            if (playerMovement != null)
                playerMovement.ChargeSpeedMultiplier = 1f;
        }

        public void SpawnBlast(float chargeNormalized)
        {
            Vector3 origin = gloveHandAnchor != null ? gloveHandAnchor.position
                            : (Cam != null ? Cam.transform.position + Cam.transform.forward : transform.position);
            Vector3 dir = Cam != null ? Cam.transform.forward : transform.forward;

            GloveBlastProjectile proj;
            if (blastPrefab != null)
            {
                proj = Instantiate(blastPrefab, origin, Quaternion.LookRotation(dir));
            }
            else
            {
                // Runtime placeholder: emissive sphere so the feature is testable before real VFX.
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "GloveBlast (placeholder)";
                go.transform.position = origin;
                go.transform.localScale = Vector3.one * 0.4f;
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col); // the projectile makes its own trigger spheres
                var rb = go.AddComponent<Rigidbody>();
                rb.useGravity = false;
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = attractTint;
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", attractTint * 2f);
                }
                proj = go.AddComponent<GloveBlastProjectile>();
            }

            float damage = Mathf.Lerp(blastMinDamage, blastMaxDamage, chargeNormalized);
            float force = Mathf.Lerp(blastMinForce, blastMaxForce, chargeNormalized);
            proj.Launch(dir * blastSpeed, chargeNormalized, damage, force);

            if (camShake != null) camShake.GenerateImpulse(dir);
            IsFiring = true;
        }

        public void EndFire() => IsFiring = false;
    }
}
