using UnityEngine;
using UnityEngine.Animations.Rigging;
using Cinemachine;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class KatanaStateMachine : MonoBehaviour
    {
        private Arsenal arsenal;

        [Header("State Machines")]
        public GameObject player;
        [HideInInspector] public PlayerStateMachine stateMachine;
        [HideInInspector] public PlayerCrosshair crosshair;

        [Space]
        [Header("Mechanics")]
        public int slashTimeout;
        public int parryTimeout;
        

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
        [HideInInspector] public int slashHash, parryHash, stowHash;
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
        public bool ableToSlashInMiddleOfAnimation;
        public bool fullyEquipped;

        [HideInInspector] public bool selectedByArsenal;

        private void Awake()
        {
            states = new KatanaStateFactory(this);
            currentState = states.Idle();
            currentState.EnterState();

            slashHash = Animator.StringToHash("slash");
            parryHash = Animator.StringToHash("parry");
            stowHash = Animator.StringToHash("stow");

            stateMachine = player.GetComponent<PlayerStateMachine>();
            crosshair = player.GetComponentInChildren<PlayerCrosshair>();

            arsenal = GetComponent<Arsenal>();
        }

        private void Update()
        {
            if (ActiveKatanaScriptable != null)
                currentState.UpdateStates();
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

        public void AbleToSlashInMiddleOfAnimation()
        {
            ableToSlashInMiddleOfAnimation = true;
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
            if (KatanaScriptable == null) {
                anim.Play("empty_rest");
                return;
            }
    
            isSlashing = false;
            ableToSlashInMiddleOfAnimation = false;

            Spawn(KatanaScriptable);
            crosshair?.SetKatanaCrosshair(ActiveKatanaScriptable);

            currentState = states.Idle();
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