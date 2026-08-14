using UnityEngine;
using UnityEngine.Animations.Rigging;
using Cinemachine;
using System.Collections;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class KatanaStateMachine : MonoBehaviour
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

        [Header("Animation")]
        public GameObject Katana;
        public Animator anim;
        [HideInInspector]
        public int readyHash, slashHash, parryHash, stowHash, slashBlendHash, slashAnimationHash;
        public KatanaScriptableObject ActiveKatanaScriptable;

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

        [HideInInspector] public bool selectedByArsenal;

        private void Awake()
        {
            states = new KatanaStateFactory(this);
            currentState = states.Idle();
            currentState.EnterState();

            readyHash = Animator.StringToHash("ready");
            slashHash = Animator.StringToHash("slash");
            parryHash = Animator.StringToHash("parry");
            stowHash = Animator.StringToHash("stow");
            slashBlendHash = Animator.StringToHash("Blend");
            slashAnimationHash = Animator.StringToHash("slash_Katana");

            stateMachine = player.GetComponent<PlayerStateMachine>();
            camStateMachine = player.GetComponent<CamStateMachine>();
            crosshair = player.GetComponentInChildren<PlayerCrosshair>();

            arsenal = GetComponent<Arsenal>();
            katanaDriver = GetComponent<KatanaDriver>();
        }

        private void Update()
        {
            if (ActiveKatanaScriptable != null)
                currentState.UpdateStates();

            currentStateText = CurrentState.GetType().Name;
        }

        public void Despawn(KatanaScriptableObject activeKatana)
        {
            if (Katana != null)
                Katana.SetActive(false);

            activeKatana?.Despawn(this);
            ActiveKatanaScriptable = null;
        }

        private void Spawn(KatanaScriptableObject activeKatana)
        {
            if (Katana != null)
                Katana.SetActive(true);

            activeKatana.Spawn(player, Katana, this);
            ActiveKatanaScriptable = activeKatana;
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
        }

        public void SetupKatana()
        {
            fullyEquipped = false;
            isSlashing = false;
            isParrying = false;
            ableToSlashInMiddleOfAnimation = false;

            Spawn(KatanaScriptable);
            crosshair?.SetKatanaCrosshair(ActiveKatanaScriptable);

            currentState = states.Idle();
            currentState.EnterState();

            if (handIK != null)
                handIK.weight = 1;

            anim.Play("equip_Katana");

            if (Katana != null)
                camShake = Katana.GetComponent<CinemachineImpulseSource>();

            ResetAnimationFlags();
        }

        private void ResetAnimationFlags()
        {
            anim.SetBool(readyHash, false);
            anim.SetBool(slashHash, false);
            anim.SetBool(parryHash, false);
            anim.SetBool(stowHash, false);
        }

        // ── Animation Events ─────────────────────────────────────

        public void EndEquip()
        {
            fullyEquipped = true;
        }

        public void EndSlash()
        {
            ableToSlashInMiddleOfAnimation = false;
            isSlashing = false;


        }

        public void SlashChain ()
        {
            ableToSlashInMiddleOfAnimation = true;
            camStateMachine.SlowCam(false);
            
        }


        private IEnumerator ParryWindow()
        {
            Debug.Log("Parry window started");
            isParryWindowActive = true;
            yield return new WaitForSeconds(parryWindow);
            isParryWindowActive = false;
            parryWindowCoroutine = null;
        }

        public void EndParry()
        {
            ableToSlashInMiddleOfAnimation = false;
            isParrying = false;
            if (parryWindowCoroutine != null)
                StopCoroutine(parryWindowCoroutine);

            parryWindowCoroutine = StartCoroutine(ParryWindow());
        }
    

        // ── Save / Load ──────────────────────────────────────────

        public void SaveData(ref KatanaSaveData data)
        {
            data.KatanaScriptable = KatanaScriptable;
            data.ActiveKatanaScriptable = ActiveKatanaScriptable;
        }

        public void LoadData(KatanaSaveData data)
        {
            // Restore the katana reference (whether or not it's the active weapon)
            KatanaScriptable = data.KatanaScriptable;
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

            ResetAnimationFlags();
            ableToSlashInMiddleOfAnimation = false;

            Spawn(KatanaScriptable);
            crosshair?.SetKatanaCrosshair(ActiveKatanaScriptable);

            currentState = states.Idle();
            anim.Play("equip_Katana");
            currentState.EnterState();

            if (handIK != null)
                handIK.weight = 1;

            if (Katana != null)
                camShake = Katana.GetComponent<CinemachineImpulseSource>();

            ResetAnimationFlags();

            // Skip equip animation — go straight to ready
            fullyEquipped = true;
        }
    }

    [System.Serializable]
    public struct KatanaSaveData
    {
        public KatanaScriptableObject KatanaScriptable;
        public KatanaScriptableObject ActiveKatanaScriptable;
    }
}
