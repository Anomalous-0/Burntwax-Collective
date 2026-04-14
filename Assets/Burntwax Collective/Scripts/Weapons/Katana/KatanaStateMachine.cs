using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Cinemachine;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class KatanaStateMachine : MonoBehaviour
    {

        [Header("State Machines")]
        public PlayerStateMachine stateMachine;
        public GameObject player;

        public PlayerCrosshair crosshair;

        [Space]
        [Header("Cinemachine Required")]
        public CinemachineImpulseSource camShake;

        [Header("Animation Rigging Required")]
        public Rig handIK;

        public int currentID;


        [Header("Animation")]
        [SerializeField] public Transform KatanaParent;



        public Animator anim;



        [HideInInspector] public int stowHash, aimHash, reloadHash, proceduralShootHash, incrementalReloadHash, shotWhileIncReloadHash;

        public KatanaScriptableObject ActiveKatanaScriptable;
        public GameObject Katana;

        [Space]





        private KatanaBaseState currentState;
        public KatanaStateFactory states;



        public KatanaBaseState CurrentState { get { return currentState; } set { currentState = value; } }


        public bool isReloading = false;
        public bool isIncrementalReloading = false;
        public bool isShooting = false;

        public bool ableToSlashInMiddleOfAnimation = false;
        public bool fullyEquipped;

        void Awake()
        {
            states = new KatanaStateFactory(this);
            currentState = states.Idle();
            currentState.EnterState();
            // handIK.weight = 0;

            aimHash = Animator.StringToHash("aim");
            stowHash = Animator.StringToHash("stow");
            reloadHash = Animator.StringToHash("reload");
            proceduralShootHash = Animator.StringToHash("proceduralShoot");
            incrementalReloadHash = Animator.StringToHash("incrementalReload");
            shotWhileIncReloadHash = Animator.StringToHash("shotWhileIncReload");
        }

        void Start()
        {

            // if (CurrentGunModel)
            //     CurrentGunModel.gameObject.SetActive(false);
            // ActiveGunScriptable = null;
            // CurrentGunModel = null;
            // anim.Play("empty_rest");
            // CurrentGuns.Clear();
        }



        private void Update()
        {

        }

        private void FixedUpdate()
        {
            if (ActiveKatanaScriptable != null)
            {
                currentState.UpdateStates();
            }
        }



        private void Despawn(KatanaScriptableObject activeKatana)
        {
            Katana.SetActive(false);
            activeKatana.Despawn(this);
        }

        private void Spawn(KatanaScriptableObject activeKatana)
        {
            Katana.SetActive(true);
            activeKatana.Spawn(player, Katana, this);
        }

        private void WeaponSelect()
        {
            if (InputManager.Instance.katanaSelect)
            {

            }
        }

        private void WeaponSwitch()
        {
          
        }


        public void Pickup(KatanaScriptableObject katana)
        {

            ActiveKatanaScriptable = katana;


            SetupKatana();




        }


        private void SetupKatana()
        {
            fullyEquipped = false;
            isReloading = false;
            Spawn(ActiveKatanaScriptable);
            crosshair.SetKatanaCrosshair(ActiveKatanaScriptable);
            currentState = states.Idle();
            if (handIK != null)
                handIK.weight = 1;
            anim.Play("equip_katana");

            camShake = Katana.GetComponent<CinemachineImpulseSource>();

            anim.SetBool(aimHash, false);
            anim.SetBool(stowHash, false);
            anim.SetBool(reloadHash, false);
            anim.SetBool(proceduralShootHash, false);
            anim.SetBool(incrementalReloadHash, false);
        }



        //Animation event
        public void EndEquip()
        {
            fullyEquipped = true;
        }


        //Animation event
        public void EndSlash()
        {
            ableToSlashInMiddleOfAnimation = false;
            isShooting = false;
        }

        public void AbleToSlashInMiddleOfAnimation()
        {
            ableToSlashInMiddleOfAnimation = true;
        }

        public void SaveData(ref GunSaveData data)
        {

        }

        public void LoadData(GunSaveData data)
        {


        }
    }

    [System.Serializable]
    public struct KatanaSaveData
    {


    }
}