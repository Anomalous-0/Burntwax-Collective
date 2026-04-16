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
        [Header("Cinemachine Required")]
        public CinemachineImpulseSource camShake;

        [Header("Animation Rigging Required")]
        public Rig handIK;

        [Header("Animation")]
        public GameObject Katana;
        public Animator anim;
        [HideInInspector] public int slashHash, parryHash, stowHash;
        public KatanaScriptableObject ActiveKatanaScriptable;

        private KatanaBaseState currentState;
        public KatanaStateFactory states;

        public KatanaBaseState CurrentState { get { return currentState; } set { currentState = value; } }

        [Space]
        public bool isSlashing = false;
        public bool ableToSlashInMiddleOfAnimation = false;
        public bool fullyEquipped;

        [HideInInspector] public bool selectedByArsenal = false;

        void Awake()
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

        void Start()
        {
        }

        private void Update()
        {
            if (ActiveKatanaScriptable != null)
            {
                currentState.UpdateStates();
            }
        }

        public void Despawn(KatanaScriptableObject activeKatana)
        {
            if (Katana != null)
                Katana.SetActive(false);

            activeKatana.Despawn(this);
        }

        private void Spawn(KatanaScriptableObject activeKatana)
        {
            if (Katana != null)
                Katana.SetActive(true);

            activeKatana.Spawn(player, Katana, this);
        }

        public bool HasKatana()
        {
            return ActiveKatanaScriptable != null;
        }


        public void Pickup(KatanaScriptableObject katana)
        {
            arsenal.lastID = arsenal.currentID;
            arsenal.currentID = -1;

            
            ActiveKatanaScriptable = katana;
            SetupKatana();
        }

        private void SetupKatana()
        {
            fullyEquipped = false;
            isSlashing = false;
            ableToSlashInMiddleOfAnimation = false;

            Spawn(ActiveKatanaScriptable);
            crosshair.SetKatanaCrosshair(ActiveKatanaScriptable);

            currentState = states.Idle();
            currentState.EnterState();

            if (handIK != null)
                handIK.weight = 1;

            anim.Play("equip_Katana");

            if (Katana != null)
                camShake = Katana.GetComponent<CinemachineImpulseSource>();

            anim.SetBool(slashHash, false);
            anim.SetBool(parryHash, false);
            anim.SetBool(stowHash, false);
        }

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

        public void SaveData(ref KatanaSaveData data)
        {
        }

        public void LoadData(KatanaSaveData data)
        {
        }
    }

    [System.Serializable]
    public struct KatanaSaveData
    {
    }
}