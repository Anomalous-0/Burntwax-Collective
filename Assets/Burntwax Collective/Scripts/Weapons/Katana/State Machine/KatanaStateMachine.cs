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


        [Space]





        private KatanaBaseState currentState;
        public KatanaStateFactory states;



        public KatanaBaseState CurrentState { get { return currentState; } set { currentState = value; } }
        [Space]

        public bool isSlashing = false;

        public bool ableToSlashInMiddleOfAnimation = false;
        public bool fullyEquipped;

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
            isSlashing = false;
            Spawn(ActiveKatanaScriptable);
            crosshair.SetKatanaCrosshair(ActiveKatanaScriptable);
            currentState = states.Idle();
            if (handIK != null)
                handIK.weight = 1;
            anim.Play("equip_katana");

            camShake = Katana.GetComponent<CinemachineImpulseSource>();

            anim.SetBool(slashHash, false);
            anim.SetBool(parryHash, false);
            anim.SetBool(stowHash, false);

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