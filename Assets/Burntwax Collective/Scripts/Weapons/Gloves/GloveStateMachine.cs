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
        [SerializeField] private GameObject gloveModel;
        [SerializeField] private Rig handIK;
        [Tooltip("Where an attracted object flies to and orbits. Auto-created under the glove model if left empty.")]
        [SerializeField] private Transform gloveHandAnchor;
        [Tooltip("The Arms animator that owns the masked left-arm Glove layer.")]
        [SerializeField] private Animator armsAnimator;
        [Tooltip("The left-hand IK constraint. Its weight is released while the glove layer is raised.")]
        [SerializeField] private TwoBoneIKConstraint leftHandIK;
        [SerializeField] private CinemachineImpulseSource camShake;

        [Header("Arm Override")]
        [SerializeField] private string gloveLayerName = "Glove";
        [Tooltip("Raise/lower speed in normalized layer weight per second.")]
        [SerializeField] private float raiseSpeed = 6f;
        [Range(0f, 1f)]
        [SerializeField] private float raiseAmount;

        [Header("States")]
        [SerializeField] private float equipWaitTime = 1f;

        [SerializeField] private bool enterAttract = false;
        [SerializeField] private bool enterCharge = false;

        [Header("Detection")]
        [SerializeField] private float detectDistance = 9f;        // max reach of the glove
        [SerializeField] private float castRadius = 0.6f;          // spherecast radius = aim margin of error
        [SerializeField] private LayerMask attractableLayer;       // set to the Consumable layer

        [Header("Highlight")]
        [SerializeField] private Color attractTint = Color.cyan;
        [Range(0f, 1f)][SerializeField] private float tintStrength = 0.5f;

        [Header("Attract Physics")]
        [SerializeField] private bool canAttract = false;

        [Header("Charge Blast")]
        [Range(0f, 1f)][SerializeField] private float minChargeSpeed = 0.45f; // player speed scale at full charge
        [Tooltip("Optional. If empty, a placeholder emissive sphere is spawned at runtime.")]
        [SerializeField] private GloveBlastProjectile blastPrefab;
        [SerializeField] private float blastSpeed = 45f;
        [SerializeField] private float blastMinDamage = 8f;
        [SerializeField] private float blastMaxDamage = 40f;
        [SerializeField] private float blastMinForce = 6f;
        [SerializeField] private float blastMaxForce = 30f;
        public bool CanAttractObject { get { return canAttract; } set { canAttract = value; } }
        public bool EnterAttract { get { return enterAttract; } set { enterAttract = value; } }
        public bool EnterCharge { get { return enterCharge; } set { enterCharge = value; } }
        // ── Exposed to states ──
        public float DetectDistance => detectDistance;
        public LayerMask AttractableLayer => attractableLayer;
        public float EquipWaitTime => equipWaitTime;
        public bool KatanaEquipped => arsenal != null && arsenal.currentMode == Arsenal.WeaponMode.Katana;

        public GloveBaseState CurrentState;
        public GloveStateFactory states;

        // ── Wired refs ──
        private Arsenal arsenal;
        private GunStateMachine gunStateMachine;
        private KatanaStateMachine katanaStateMachine;
        private int gloveLayerIndex = -1;
        private float baseLeftHandWeight = 1f;
        private bool gloveEquipped;
        private bool unequippedAnimationPending;

        public int idleHash, emptyHash, attractHash, blastHash, blastProjectileHash, holdHash;


        private MaterialPropertyBlock mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            arsenal = GetComponent<Arsenal>();
            gunStateMachine = GetComponent<GunStateMachine>();
            katanaStateMachine = GetComponent<KatanaStateMachine>();

            mpb = new MaterialPropertyBlock();
            idleHash = Animator.StringToHash("idle_Glove");
            emptyHash = Animator.StringToHash("empty_Glove");
            blastHash = Animator.StringToHash("blast_Glove");
            blastProjectileHash = Animator.StringToHash("blastProjectile_Glove");

            InitializeArmOverride();

            states = new GloveStateFactory(this);
            CurrentState = states.Unequipped();
            CurrentState.EnterState();
        }

        private void Update()
        {
            CurrentState.UpdateStates();
            UpdateArmOverride();
            currentStateText = CurrentState.GetType().Name;
            currentSubstateText = CurrentState.CurrentSubstate()?.GetType().Name ?? "None";

            // Every glove gesture is consumed within this frame's state processing above, so clear
            // the one-frame pulses now.
            if (InputManager.Instance != null)
                InputManager.Instance.ConsumeGloveGestures();
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

        /*public void SpawnBlast(float chargeNormalized)
        {
            Vector3 origin = gloveHandAnchor != null ? gloveHandAnchor.position
                            : (Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward : transform.position);
            Vector3 dir = Camera.main != null ? Camera.main.transform.forward : transform.forward;

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
        } */

    }
}
