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
        [SerializeField] private GameObject player;
        [HideInInspector] public PlayerStateMachine stateMachine;
        [HideInInspector] public PlayerCrosshair crosshair;

        [Space]
        [Header("Cinemachine Required")]
        public CinemachineImpulseSource camShake;

        [Header("Animation Rigging Required")]
        public Rig handIK;

        [Header("Animation")]
        public Animator anim;

        [Header("Owned Guns")]
        public List<GunScriptableObject> CurrentGuns = new();
        public List<GameObject> OrderedGuns = new();

        [Header("Current Gun")]
        public GunScriptableObject ActiveGunScriptable;
        public GameObject CurrentGunModel;
        public ProceduralRecoil recoil;

        [HideInInspector] public int stowHash;
        [HideInInspector] public int aimHash;
        [HideInInspector] public int reloadHash;
        [HideInInspector] public int proceduralShootHash;
        [HideInInspector] public int incrementalReloadHash;
        [HideInInspector] public int shotWhileIncReloadHash;

        public int currentID;

        private GunBaseState currentState;
        public GunStateFactory states;

        private readonly Dictionary<int, GunScriptableObject> gunLookup = new();

        private AudioSource playerAudioSource;

        public GunBaseState CurrentState
        {
            get => currentState;
            set => currentState = value;
        }

        public bool isReloading;
        public bool isIncrementalReloading;
        public bool isShooting;
        public bool ableToShootInMiddleOfAnimation;
        public bool fullyEquipped;

        private void Awake()
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

            if (player != null)
            {
                stateMachine = player.GetComponent<PlayerStateMachine>();
                crosshair = player.GetComponentInChildren<PlayerCrosshair>();
                playerAudioSource = player.GetComponent<AudioSource>();
            }

            RebuildGunLookup();
        }

        private void Update()
        {
            if (ActiveGunScriptable == null)
                return;

            currentState.UpdateStates();

            

            HandleEmptyClip();
        }

        private void RebuildGunLookup()
        {
            gunLookup.Clear();

            for (int i = 0; i < CurrentGuns.Count; i++)
            {
                GunScriptableObject gun = CurrentGuns[i];
                if (gun != null)
                {
                    gunLookup[gun.ID] = gun;
                }
            }
        }

        public void HandleGunSwitching()
        {
            if (PauseMenu.Instance.Paused)
                return;

            bool hasWeaponNumberInput = ReadWeaponHotkey(out int selectedID);
            bool hasScrollInput = InputManager.Instance.mouseScroll != 0f;

            if ((hasWeaponNumberInput || hasScrollInput) && !isShooting)
            {
                int targetID = hasScrollInput ? GetScrollID() : selectedID;
                SwitchToGun(targetID);
            }
        }

        private bool ReadWeaponHotkey(out int selectedID)
        {
            selectedID = currentID;

            if (!InputManager.Instance.weaponSelectID)
                return false;

            if (Input.GetKeyDown(KeyCode.Alpha1)) { selectedID = 0; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { selectedID = 1; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { selectedID = 2; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { selectedID = 3; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha5)) { selectedID = 4; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha6)) { selectedID = 5; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha7)) { selectedID = 6; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha8)) { selectedID = 7; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha9)) { selectedID = 8; return true; }

            return false;
        }

        private int GetScrollID()
        {
            if (CurrentGuns.Count == 0)
                return currentID;

            int nextID = currentID;

            if (InputManager.Instance.mouseScroll > 0f)
            {
                nextID++;
            }
            else if (InputManager.Instance.mouseScroll < 0f)
            {
                nextID--;
            }

            if (nextID < 0)
                nextID = CurrentGuns.Count - 1;
            else if (nextID >= CurrentGuns.Count)
                nextID = 0;

            return nextID;
        }

        private bool SwitchToGun(int newID)
        {
            if (ActiveGunScriptable != null && newID == ActiveGunScriptable.ID)
                return false;

            if (!gunLookup.TryGetValue(newID, out GunScriptableObject newGun) || newGun == null)
            {
                Debug.LogError($"No GunScriptableObject found for gun ID: {newID}");
                return false;
            }

            currentID = newID;

            if (ActiveGunScriptable != null)
            {
                Despawn(ActiveGunScriptable);
            }

            ActiveGunScriptable = newGun;
            SetupGun();

            return true;
        }

        private void Despawn(GunScriptableObject activeGun)
        {
            if (CurrentGunModel != null)
            {
                CurrentGunModel.SetActive(false);
            }

            activeGun?.Despawn(this);
        }

        private void Spawn(GunScriptableObject activeGun)
        {
            if (currentID < 0 || currentID >= OrderedGuns.Count)
            {
                Debug.LogError($"OrderedGuns is missing an entry for currentID {currentID}");
                return;
            }

            CurrentGunModel = OrderedGuns[currentID];

            if (CurrentGunModel == null)
            {
                Debug.LogError($"OrderedGuns[{currentID}] is null.");
                return;
            }

            CurrentGunModel.SetActive(true);
            activeGun.Spawn(player, CurrentGunModel, this);
        }

        public void Pickup(GunScriptableObject gun)
        {
            if (gun == null || CurrentGuns.Contains(gun))
                return;

            CurrentGuns.Add(gun);
            gunLookup[gun.ID] = gun;

            if (ActiveGunScriptable != null)
            {
                Despawn(ActiveGunScriptable);
            }

            ActiveGunScriptable = gun;
            currentID = gun.ID;

            SetupGun();
            ActiveGunScriptable.AmmoConfig.Enable();
        }

        private void SetupGun()
        {
            if (ActiveGunScriptable == null)
                return;

            fullyEquipped = false;
            isReloading = false;
            isIncrementalReloading = false;
            isShooting = false;
            ableToShootInMiddleOfAnimation = false;

            Spawn(ActiveGunScriptable);

            crosshair?.SetGunCrosshair(ActiveGunScriptable);

            currentState = states.Rest();

            if (handIK != null)
            {
                handIK.weight = 1f;
            }

            anim.Play("equip_" + ActiveGunScriptable.Type);

            if (CurrentGunModel != null)
            {
                camShake = CurrentGunModel.GetComponent<CinemachineImpulseSource>();
            }

            ResetAnimationFlags();
        }

        private void ResetAnimationFlags()
        {
            anim.SetBool(aimHash, false);
            anim.SetBool(stowHash, false);
            anim.SetBool(reloadHash, false);
            anim.SetBool(proceduralShootHash, false);
            anim.SetBool(incrementalReloadHash, false);
        }

        private void HandleEmptyClip()
        {
            if (!IsOutOfAmmo())
                return;

            if (!InputManager.Instance.shootIsPressed)
                return;

            GunBaseState superState = CurrentState.CurrentSuperstate();
            if (superState == states.Reload() || superState == states.Stow())
                return;

            if (playerAudioSource != null)
            {
                ActiveGunScriptable.AudioConfig.PlayEmptyClip(playerAudioSource);
            }

            InputManager.Instance.shootIsPressed = false;
        }

        public bool CanShoot()
        {
            return ActiveGunScriptable != null &&
                   ActiveGunScriptable.AmmoConfig.currentClip > 0 &&
                   !isShooting;
        }

        private bool IsOutOfAmmo()
        {
            return ActiveGunScriptable != null &&
                   ActiveGunScriptable.AmmoConfig.currentClip == 0 &&
                   ActiveGunScriptable.AmmoConfig.currentAmmo == 0;
        }

        // Animation event
        public void EndReload()
        {
            if (ActiveGunScriptable != null && !ActiveGunScriptable.incrementalReload)
            {
                ActiveGunScriptable.EndReload();
            }

            isReloading = false;
        }

        // Animation event
        public void EndEquip()
        {
            fullyEquipped = true;
        }

        // Animation event
        public void EndIncrementalReload()
        {
            ActiveGunScriptable?.EndIncrementalReload();
        }

        // Animation event
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
            return ActiveGunScriptable != null &&
                   InputManager.Instance.reloadIsPressed &&
                   ActiveGunScriptable.CanReload();
        }

        public bool IncrementalReload()
        {
            return ActiveGunScriptable != null && ActiveGunScriptable.CanReload();
        }

        public bool AutoReload()
        {
            return ActiveGunScriptable != null &&
                   ActiveGunScriptable.CanReload() &&
                   ActiveGunScriptable.AmmoConfig.currentClip == 0;
        }

        public void SaveData(ref GunSaveData data)
        {
            if (ActiveGunScriptable == null)
            {
                data.GunModel = null;
                data.ActiveGunScriptable = null;
                data.CurrentGuns ??= new List<GunScriptableObject>();
                data.CurrentGuns.Clear();
                data.currentState = null;
                data.clipAmmo = new List<int>();
                data.reserveAmmo = new List<int>();
                return;
            }

            data.GunModel = CurrentGunModel;
            data.ActiveGunScriptable = ActiveGunScriptable;
            data.CurrentGuns = new List<GunScriptableObject>(CurrentGuns);
            data.currentState = currentState;

            data.clipAmmo = new List<int>(CurrentGuns.Count);
            data.reserveAmmo = new List<int>(CurrentGuns.Count);

            for (int i = 0; i < CurrentGuns.Count; i++)
            {
                GunScriptableObject gun = CurrentGuns[i];
                data.clipAmmo.Add(gun.AmmoConfig.currentClip);
                data.reserveAmmo.Add(gun.AmmoConfig.currentAmmo);
            }
        }

        public void LoadData(GunSaveData data)
        {
            if (data.ActiveGunScriptable == null)
            {
                if (CurrentGunModel != null)
                    CurrentGunModel.SetActive(false);

                ActiveGunScriptable = null;
                CurrentGunModel = null;
                anim.Play("empty_rest");
                CurrentGuns.Clear();
                gunLookup.Clear();
                return;
            }

            if (ActiveGunScriptable != null)
            {
                Despawn(ActiveGunScriptable);
            }

            CurrentGuns = data.CurrentGuns ?? new List<GunScriptableObject>();
            RebuildGunLookup();

            ActiveGunScriptable = data.ActiveGunScriptable;
            currentID = ActiveGunScriptable.ID;

            SetupGun();

            currentState = states.Rest();
            currentState.EnterState();
            fullyEquipped = true;

            int ammoCount = Mathf.Min(CurrentGuns.Count, data.clipAmmo.Count, data.reserveAmmo.Count);
            for (int i = 0; i < ammoCount; i++)
            {
                CurrentGuns[i].AmmoConfig.currentClip = data.clipAmmo[i];
                CurrentGuns[i].AmmoConfig.currentAmmo = data.reserveAmmo[i];
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