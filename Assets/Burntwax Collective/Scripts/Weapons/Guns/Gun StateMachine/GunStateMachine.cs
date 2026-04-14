using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Cinemachine;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class GunStateMachine : MonoBehaviour
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

        public int currentID;

        [Header("Animation")]
        public Animator anim;
        [HideInInspector] public int stowHash, aimHash, reloadHash, proceduralShootHash, incrementalReloadHash, shotWhileIncReloadHash;

        public List<GunScriptableObject> CurrentGuns;
        public List<GameObject> OrderedGuns;

        [Space]
        [Header("Current Gun")]
        public GunScriptableObject ActiveGunScriptable;
        public GameObject CurrentGunModel;
        public ProceduralRecoil recoil;

        private GunBaseState currentState;
        public GunStateFactory states;

        public GunBaseState CurrentState { get { return currentState; } set { currentState = value; } }

        public bool isReloading = false;
        public bool isIncrementalReloading = false;
        public bool isShooting = false;

        public bool ableToShootInMiddleOfAnimation = false;
        public bool fullyEquipped;

        [HideInInspector] public bool selectedByArsenal = true;
        [HideInInspector] public int lastGunID = -1;

        void Awake()
        {
            states = new GunStateFactory(this);
            currentState = states.Rest();
            currentState.EnterState();

            aimHash = Animator.StringToHash("aim");
            stowHash = Animator.StringToHash("stow");
            reloadHash = Animator.StringToHash("reload");
            proceduralShootHash = Animator.StringToHash("proceduralShoot");
            incrementalReloadHash = Animator.StringToHash("incrementalReload");
            shotWhileIncReloadHash = Animator.StringToHash("shotWhileIncReload");

            stateMachine = player.GetComponent<PlayerStateMachine>();
            crosshair = player.GetComponentInChildren<PlayerCrosshair>();
        }

        void Start()
        {
        }

        private void Update()
        {
            if (!selectedByArsenal || PauseMenu.Instance.Paused)
                return;

            if (ActiveGunScriptable != null)
            {
                EmptyClip();
            }
        }

        private void FixedUpdate()
        {
            if (!selectedByArsenal)
                return;

            if (ActiveGunScriptable != null)
            {
                currentState.UpdateStates();
            }
        }

        private void Despawn(GunScriptableObject activeGun)
        {
            if (CurrentGunModel != null)
                CurrentGunModel.SetActive(false);

            activeGun.Despawn(this);
        }

        private void Spawn(GunScriptableObject activeGun)
        {
            DisableAllGunModels();

            if (OrderedGuns == null || currentID < 0 || currentID >= OrderedGuns.Count)
            {
                Debug.LogError($"OrderedGuns does not contain a valid model for gun ID {currentID}");
                return;
            }

            CurrentGunModel = OrderedGuns[currentID];
            CurrentGunModel.SetActive(true);
            activeGun.Spawn(player, CurrentGunModel, this);
        }

        public bool HasGun()
        {
            return CurrentGuns != null && CurrentGuns.Count > 0;
        }

        public void EquipFromArsenal()
        {
            selectedByArsenal = true;

            if (CurrentGuns != null && CurrentGuns.Count > 0)
            {
                GunScriptableObject gunToEquip = null;

                if (lastGunID != -1)
                    gunToEquip = CurrentGuns.Find(g => g.ID == lastGunID);

                if (gunToEquip == null)
                {
                    gunToEquip = ActiveGunScriptable != null ? ActiveGunScriptable : CurrentGuns[0];

                    anim.Play("empty_rest");
                    return;
                }

                ActiveGunScriptable = gunToEquip;
                currentID = gunToEquip.ID;
                SetupGun();
            }
            else
            {
                if (CurrentGunModel != null)
                    CurrentGunModel.SetActive(false);

                if (handIK != null)
                    handIK.weight = 1;

                anim.Play("empty_rest");
            }
        }

        public void StowFromArsenal()
        {
            selectedByArsenal = false;

            isReloading = false;
            isIncrementalReloading = false;
            isShooting = false;
            ableToShootInMiddleOfAnimation = false;
            fullyEquipped = false;

            if (CurrentGunModel != null)
                CurrentGunModel.SetActive(false);

            if (handIK != null)
                handIK.weight = 0;

            anim.SetBool(aimHash, false);
            anim.SetBool(stowHash, false);
            anim.SetBool(reloadHash, false);
            anim.SetBool(proceduralShootHash, false);
            anim.SetBool(incrementalReloadHash, false);

            anim.Play("empty_rest");
        }

        public void SelectGunByID(int gunID)
        {
            if (CurrentGuns == null || CurrentGuns.Count == 0)
                return;

            GunScriptableObject gun = CurrentGuns.Find(g => g.ID == gunID);
            if (gun == null)
                return;

            if (ActiveGunScriptable != null && ActiveGunScriptable.ID == gunID && selectedByArsenal)
                return;

            if (ActiveGunScriptable != null && CurrentGunModel != null)
                Despawn(ActiveGunScriptable);

            ActiveGunScriptable = gun;
            currentID = gun.ID;
            SetupGun();
        }

        public void ScrollGun(float scrollValue)
        {
            if (CurrentGuns == null || CurrentGuns.Count == 0)
                return;

            int currentIndex = CurrentGuns.FindIndex(g => g.ID == currentID);
            if (currentIndex < 0)
                currentIndex = 0;

            if (scrollValue > 0)
            {
                currentIndex++;
                if (currentIndex >= CurrentGuns.Count)
                    currentIndex = 0;
            }
            else if (scrollValue < 0)
            {
                currentIndex--;
                if (currentIndex < 0)
                    currentIndex = CurrentGuns.Count - 1;
            }

            SelectGunByID(CurrentGuns[currentIndex].ID);
        }

        public void Pickup(GunScriptableObject gun)
        {
            if (!CurrentGuns.Contains(gun))
            {
                CurrentGuns.Add(gun);
            }

            gun.AmmoConfig.Enable();

            ActiveGunScriptable = gun;
            currentID = gun.ID;
            lastGunID = gun.ID;

            Arsenal arsenal = GetComponent<Arsenal>();
            if (arsenal != null)
            {
                arsenal.ForceSwitchToGuns();
            }
            else
            {
                selectedByArsenal = true;
                SetupGun();
            }
        }

        private void SetupGun()
        {
            if (ActiveGunScriptable == null)
                return;

            lastGunID = currentID;

            fullyEquipped = false;
            isReloading = false;
            isIncrementalReloading = false;
            isShooting = false;
            ableToShootInMiddleOfAnimation = false;

            Spawn(ActiveGunScriptable);
            crosshair.SetGunCrosshair(ActiveGunScriptable);

            currentState = states.Rest();
            currentState.EnterState();

            if (handIK != null)
                handIK.weight = 1;

            anim.Play("equip_" + ActiveGunScriptable.Type);

            if (CurrentGunModel != null)
                camShake = CurrentGunModel.GetComponent<CinemachineImpulseSource>();

            anim.SetBool(aimHash, false);
            anim.SetBool(stowHash, false);
            anim.SetBool(reloadHash, false);
            anim.SetBool(proceduralShootHash, false);
            anim.SetBool(incrementalReloadHash, false);
        }

        private void EmptyClip()
        {
            if (ClipEmpty()
                && InputManager.Instance.shootIsPressed
                && CurrentState.CurrentSuperstate() != states.Reload()
                && CurrentState.CurrentSuperstate() != states.Stow())
            {
                ActiveGunScriptable.AudioConfig.PlayEmptyClip(stateMachine.GetComponent<AudioSource>());
                InputManager.Instance.shootIsPressed = false;
            }
        }

        public bool CanShoot()
        {
            return ActiveGunScriptable != null
                && ActiveGunScriptable.AmmoConfig.currentClip > 0
                && !isShooting;
        }

        private bool ClipEmpty()
        {
            return ActiveGunScriptable != null
                && ActiveGunScriptable.AmmoConfig.currentClip == 0
                && ActiveGunScriptable.AmmoConfig.currentAmmo == 0;
        }

        public void EndReload()
        {
            if (!ActiveGunScriptable.incrementalReload)
            {
                ActiveGunScriptable.EndReload();
            }

            isReloading = false;
        }

        public void EndEquip()
        {
            fullyEquipped = true;
        }

        public void EndIncrementalReload()
        {
            ActiveGunScriptable.EndIncrementalReload();
        }

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
            return ActiveGunScriptable != null
                && InputManager.Instance.reloadIsPressed
                && ActiveGunScriptable.CanReload();
        }

        public bool IncrementalReload()
        {
            return ActiveGunScriptable != null && ActiveGunScriptable.CanReload();
        }

        public bool AutoReload()
        {
            return ActiveGunScriptable != null
                && ActiveGunScriptable.CanReload()
                && ActiveGunScriptable.AmmoConfig.currentClip == 0;
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
                data.lastGunID = lastGunID;
                return;
            }

            data.GunModel = CurrentGunModel;
            data.ActiveGunScriptable = ActiveGunScriptable;
            data.ActiveGunScriptable.ID = currentID;
            data.CurrentGuns = CurrentGuns;
            data.currentState = currentState;
            data.lastGunID = lastGunID;

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
                lastGunID = data.lastGunID;
                return;
            }

            if (ActiveGunScriptable != null && CurrentGunModel != null)
                Despawn(ActiveGunScriptable);

            currentID = data.ActiveGunScriptable.ID;
            CurrentGuns = data.CurrentGuns;
            ActiveGunScriptable = data.ActiveGunScriptable;
            CurrentGunModel = data.GunModel;
            lastGunID = data.lastGunID;

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

        private void DisableAllGunModels()
        {
            if (OrderedGuns == null) return;

            foreach (GameObject gunObj in OrderedGuns)
            {
                if (gunObj != null)
                    gunObj.SetActive(false);
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
        public int lastGunID;
    }
}