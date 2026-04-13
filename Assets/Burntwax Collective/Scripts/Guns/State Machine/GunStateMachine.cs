using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Cinemachine;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public partial class GunStateMachine : MonoBehaviour
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
        [SerializeField] public Transform GunParent;



        public Animator anim;



        [HideInInspector] public int stowHash, aimHash, reloadHash, proceduralShootHash, incrementalReloadHash, shotWhileIncReloadHash;

        public List<GunScriptableObject> CurrentGuns;
        public List<GameObject> OrderedGuns;

        [Space]


        [Header("Current Gun")]
        public GunScriptableObject ActiveGunScriptable;
        public GameObject CurrentGunModel;
        public ProceduralRecoil recoil;


        [Space]



        private GunBaseState currentState;
        public GunStateFactory states;



        public GunBaseState CurrentState { get { return currentState; } set { currentState = value; } }


        public bool isReloading = false;
        public bool isIncrementalReloading = false;
        public bool isShooting = false;

        public bool ableToShootInMiddleOfAnimation = false;
        public bool fullyEquipped;

        void Awake()
        {
            states = new GunStateFactory(this);
            currentState = states.Rest();
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
            if (ActiveGunScriptable != null && !PauseMenu.Instance.Paused)
            {
                WeaponSelect();


                if ((InputManager.Instance.weaponSelectID || InputManager.Instance.mouseScroll != 0) && !isShooting)
                {
                    WeaponSwitch();
                }
                EmptyClip();
            }

        }

        private void FixedUpdate()
        {
            if (ActiveGunScriptable)
            {
                currentState.UpdateStates();
            }
        }



        private void Despawn(GunScriptableObject activeGun)
        {
            CurrentGunModel.SetActive(false);
            activeGun.Despawn(this);
        }

        private void Spawn(GunScriptableObject activeGun)
        {
            CurrentGunModel = OrderedGuns[currentID];
            CurrentGunModel.SetActive(true);
            activeGun.Spawn(player, CurrentGunModel, this);

        }

        private void WeaponSelect()
        {
            if (InputManager.Instance.weaponSelectID)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    currentID = 0;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    currentID = 1;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    currentID = 2;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    currentID = 3;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    currentID = 4;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    currentID = 5;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha7))
                {
                    currentID = 6;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha8))
                {
                    currentID = 7;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha9))
                {
                    currentID = 8;
                }
            }
        }

        private void WeaponSwitch()
        {
            if (InputManager.Instance.mouseScroll != 0)
            {
                ScrollID();
            }
            else if (InputManager.Instance.weaponSelectID)
            {

                WeaponSelect();
            }

            if (currentID != ActiveGunScriptable.ID)
            {
                GunScriptableObject gun = CurrentGuns.Find(gun => gun.ID == currentID);
                if (gun == null)
                {
                    Debug.LogError($"No GunScriptableObject found for GunType: {gun}");
                    return;
                }
                Despawn(ActiveGunScriptable);

                ActiveGunScriptable = gun;
                SetupGun();
            }
        }

        private void ScrollID()
        {
            if (InputManager.Instance.mouseScroll > 0)
            {
                currentID++;
                if (currentID > CurrentGuns.Count - 1)
                {
                    currentID = 0;
                }
            }
            else if (InputManager.Instance.mouseScroll < 0)
            {
                currentID--;
                if (currentID < 0)
                {
                    currentID = CurrentGuns.Count - 1;
                }
            }



        }

        public void Pickup(GunScriptableObject gun)
        {
            if (!CurrentGuns.Contains(gun))
            {
                CurrentGuns.Add(gun);
                if (ActiveGunScriptable != null)
                {
                    Despawn(ActiveGunScriptable);
                }

                ActiveGunScriptable = gun;
                currentID = gun.ID;


                SetupGun();
                ActiveGunScriptable.AmmoConfig.Enable();

            }

        }


        private void SetupGun()
        {
            fullyEquipped = false;
            isReloading = false;
            Spawn(ActiveGunScriptable);
            crosshair.SetGunCrosshair(ActiveGunScriptable);
            currentState = states.Rest();
            if (handIK != null)
                handIK.weight = 1;
            anim.Play("equip_" + ActiveGunScriptable.Type);

            camShake = CurrentGunModel.GetComponent<CinemachineImpulseSource>();

            anim.SetBool(aimHash, false);
            anim.SetBool(stowHash, false);
            anim.SetBool(reloadHash, false);
            anim.SetBool(proceduralShootHash, false);
            anim.SetBool(incrementalReloadHash, false);
        }




        private void EmptyClip()
        {
            if (ClipEmpty() && InputManager.Instance.shootIsPressed && CurrentState.CurrentSuperstate() != states.Reload() && CurrentState.CurrentSuperstate() != states.Stow() && ClipEmpty() && InputManager.Instance.shootIsPressed)
            {
                ActiveGunScriptable.AudioConfig.PlayEmptyClip(stateMachine.GetComponent<AudioSource>());
                InputManager.Instance.shootIsPressed = false;
            }
        }

        public bool CanShoot()
        {
            return ActiveGunScriptable.AmmoConfig.currentClip > 0 && !isShooting;
        }

        private bool ClipEmpty()
        {
            return ActiveGunScriptable.AmmoConfig.currentClip == 0 && ActiveGunScriptable.AmmoConfig.currentAmmo == 0;
        }

        //Animation event
        public void EndReload()
        {
            if (!ActiveGunScriptable.incrementalReload)
            {

                ActiveGunScriptable.EndReload();
            }
            isReloading = false;


        }

        //Animation event
        public void EndEquip()
        {
            fullyEquipped = true;
        }


        //Animation event
        public void EndIncrementalReload()
        {
            ActiveGunScriptable.EndIncrementalReload();
        }


        //Animation event
        public void EndShoot()
        {
            ableToShootInMiddleOfAnimation = false;
            isShooting = false;
        }

        public void AbleToShootInMiddleOfAnimation()
        {
            ableToShootInMiddleOfAnimation = true;
        }





        public bool ManualReload()
        {
            return InputManager.Instance.reloadIsPressed && ActiveGunScriptable.CanReload();
            // return  !isReloading && InputManager.Instance.reloadIsPressed && GunSelector.ActiveGun.CanReload();
        }


        public bool IncrementalReload()
        {
            return ActiveGunScriptable.CanReload();
        }

        public bool AutoReload()
        {
            return ActiveGunScriptable.CanReload() && ActiveGunScriptable.AmmoConfig.currentClip == 0;
        }

        public void SaveData(ref GunSaveData data)
        {
            if (ActiveGunScriptable == null)
            {
                data.GunModel = null;
                data.ActiveGunScriptable = null;
                if (CurrentGuns.Count > 0)
                    data.CurrentGuns.Clear();
                data.currentState = null;
                return;
            }
            data.GunModel = CurrentGunModel;
            data.ActiveGunScriptable = ActiveGunScriptable;
            data.ActiveGunScriptable.ID = currentID;
            data.CurrentGuns = CurrentGuns;
            data.currentState = currentState;

            data.clipAmmo = new List<int>();
            data.reserveAmmo = new List<int>();
            foreach (GunScriptableObject gun in CurrentGuns)
            {
                data.clipAmmo.Add(gun.AmmoConfig.currentClip);
                data.reserveAmmo.Add(gun.AmmoConfig.currentAmmo);
            }
        }

        public void LoadData(GunSaveData data)
        {
            if (data.ActiveGunScriptable == null)
            {
                if (CurrentGunModel)
                    CurrentGunModel.gameObject.SetActive(false);
                ActiveGunScriptable = null;
                CurrentGunModel = null;
                anim.Play("empty_rest");
                CurrentGuns.Clear();
                return;
            }
            if (ActiveGunScriptable != null)
                Despawn(ActiveGunScriptable);

            currentID = data.ActiveGunScriptable.ID;
            CurrentGuns = data.CurrentGuns;
            ActiveGunScriptable = data.ActiveGunScriptable;
            CurrentGunModel = data.GunModel;
            Spawn(ActiveGunScriptable);
            SetupGun();

            currentState = data.currentState;
            currentState = states.Rest();
            currentState.EnterState();
            fullyEquipped = true;


            foreach (GunScriptableObject gun in CurrentGuns)
            {
                int index = CurrentGuns.IndexOf(gun);
                gun.AmmoConfig.currentClip = data.clipAmmo[index];
                gun.AmmoConfig.currentAmmo = data.reserveAmmo[index];
            }

        }
    }

    [System.Serializable]
    public struct GunSaveData
    {
        public List<GunScriptableObject> CurrentGuns;
        public GameObject GunModel;
        public GunScriptableObject ActiveGunScriptable;
        public List<int> clipAmmo;
        public List<int> reserveAmmo;
        public GunBaseState currentState;


    }
}