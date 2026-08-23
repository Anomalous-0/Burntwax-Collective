using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Cinemachine;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class GunStateMachine : MonoBehaviour
    {
        private Arsenal arsenal;

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

        [Header("Save/Load Catalog")]
        [Tooltip("Every gun asset that can be restored from a save file. IDs must be unique.")]
        [SerializeField] private List<GunScriptableObject> gunCatalog = new();

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
        private readonly Dictionary<int, GunScriptableObject> gunCatalogLookup = new();

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

            crosshair = player.GetComponentInChildren<PlayerCrosshair>();
            playerAudioSource = player.GetComponent<AudioSource>();

            RebuildGunCatalogLookup();
            RebuildGunLookup();

            arsenal = GetComponent<Arsenal>();
        }

        private void Update()
        {
            if (stateMachine != null && stateMachine.GameplayActionsLocked)
                return;

            HandleWeaponSwitchInput();

            if (ActiveGunScriptable != null)
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
                    gunLookup[gun.ID] = gun;
            }
        }

        private void RebuildGunCatalogLookup()
        {
            gunCatalogLookup.Clear();

            if (gunCatalog == null)
                return;

            foreach (GunScriptableObject gun in gunCatalog)
            {
                if (gun == null)
                    continue;

                if (gunCatalogLookup.ContainsKey(gun.ID))
                    Debug.LogWarning($"Gun save catalog contains duplicate ID {gun.ID} ('{gun.name}').", this);

                gunCatalogLookup[gun.ID] = gun;
            }
        }

        private void HandleWeaponSwitchInput()
        {
            if (InputManager.Instance == null || currentState.CurrentSubstate() == states.Shoot())
                return;

            bool hasHotkeyInput = InputManager.Instance.HasWeaponSelectInput;
            bool hasScrollInput = Mathf.Abs(InputManager.Instance.mouseScroll) > 0.01f;

            if (!hasHotkeyInput && !hasScrollInput)
                return;

            int requestedID = hasHotkeyInput
                ? InputManager.Instance.weaponSelectID
                : GetScrolledGunID(InputManager.Instance.mouseScroll);

            InputManager.Instance.ConsumeWeaponSwitchInput();

            if (requestedID < 0)
                return;

            SwitchToGun(requestedID);
        }

        private int GetScrolledGunID(float scrollValue)
        {
            if (CurrentGuns == null || CurrentGuns.Count == 0)
                return -1;

            int direction = scrollValue > 0f ? 1 : -1;
            int currentIndex = CurrentGuns.FindIndex(gun => gun != null && gun.ID == currentID);

            if (currentIndex < 0 && ActiveGunScriptable != null)
                currentIndex = CurrentGuns.FindIndex(gun => gun != null && gun.ID == ActiveGunScriptable.ID);

            if (currentIndex < 0)
                currentIndex = 0;

            for (int i = 0; i < CurrentGuns.Count; i++)
            {
                currentIndex = (currentIndex + direction + CurrentGuns.Count) % CurrentGuns.Count;
                GunScriptableObject gun = CurrentGuns[currentIndex];

                if (gun != null)
                    return gun.ID;
            }

            return -1;
        }

        private bool HasWeaponModelForID(int weaponID)
        {
            if (OrderedGuns == null)
            {
                Debug.LogError("OrderedGuns is null.");
                return false;
            }

            if (weaponID < 0 || weaponID >= OrderedGuns.Count)
            {
                Debug.LogError($"OrderedGuns is missing an entry for weapon ID {weaponID}.");
                return false;
            }

            if (OrderedGuns[weaponID] == null)
            {
                Debug.LogError($"OrderedGuns[{weaponID}] is null.");
                return false;
            }

            return true;
        }

        public bool SwitchToGun(int newID)
        {
            if (ActiveGunScriptable != null &&
                ActiveGunScriptable.ID == newID &&
                arsenal != null &&
                arsenal.currentMode == Arsenal.WeaponMode.Guns)
            {
                return false;
            }

            if (!gunLookup.TryGetValue(newID, out GunScriptableObject newGun) || newGun == null)
            {
                // This can happen when the player presses a hotkey for a weapon they have not picked up yet.
                Debug.LogWarning($"Cannot switch to gun ID {newID} because it is not in CurrentGuns.");
                return false;
            }

            if (!HasWeaponModelForID(newID))
                return false;

            if (arsenal != null && arsenal.currentMode == Arsenal.WeaponMode.Katana)
            {
                KatanaStateMachine katana = GetComponent<KatanaStateMachine>();

                if (katana != null && katana.ActiveKatanaScriptable != null)
                    katana.Despawn(katana.ActiveKatanaScriptable);
            }

            if (ActiveGunScriptable != null)
                Despawn(ActiveGunScriptable);

            currentID = newID;
            ActiveGunScriptable = newGun;

            if (arsenal != null)
            {
                arsenal.lastID = arsenal.currentID;
                arsenal.currentID = currentID;
                arsenal.currentMode = Arsenal.WeaponMode.Guns;
            }

            SetupGun();

            return true;
        }

        public void Despawn(GunScriptableObject activeGun)
        {
            if (CurrentGunModel != null)
                CurrentGunModel.SetActive(false);

            activeGun?.Despawn(this);
            ActiveGunScriptable = null;
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

            KatanaStateMachine katana = GetComponent<KatanaStateMachine>();

            if (arsenal.currentMode == Arsenal.WeaponMode.Katana)
                katana.Despawn(katana.ActiveKatanaScriptable);

            if (ActiveGunScriptable != null)
                Despawn(ActiveGunScriptable);

            ActiveGunScriptable = gun;
            currentID = gun.ID;

            arsenal.lastID = arsenal.currentID;
            arsenal.currentID = currentID;

            SetupGun();
            ActiveGunScriptable.AmmoConfig.Enable();
            arsenal.currentMode = Arsenal.WeaponMode.Guns;
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
                handIK.weight = 1f;

            anim.Play("equip_" + ActiveGunScriptable.Type);

            if (CurrentGunModel != null)
                camShake = CurrentGunModel.GetComponent<CinemachineImpulseSource>();

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
            if (!IsOutOfAmmo() || !InputManager.Instance.attackIsPressed)
                return;

            GunBaseState superState = CurrentState.CurrentSuperstate();
            if (superState == states.Reload() || superState == states.Stow())
                return;

            if (playerAudioSource != null)
                ActiveGunScriptable.AudioConfig.PlayEmptyClip(playerAudioSource);

            InputManager.Instance.attackIsPressed = false;
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

        // ── Animation Events ─────────────────────────────────────

        public void EndReload()
        {
            if (ActiveGunScriptable != null && !ActiveGunScriptable.incrementalReload)
                ActiveGunScriptable.EndReload();

            isReloading = false;
        }

        public void EndEquip()
        {
            fullyEquipped = true;
        }

        public void EndIncrementalReload()
        {
            ActiveGunScriptable?.EndIncrementalReload();
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

        // ── Reload Helpers ───────────────────────────────────────

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

        // ── Save / Load ──────────────────────────────────────────

        public void SaveData(ref GunSaveData data)
        {
            data.currentID = currentID;
            data.activeGunID = ActiveGunScriptable != null ? ActiveGunScriptable.ID : -1;
            data.ownedGunIDs = new List<int>();
            data.ammo = new List<GunAmmoSaveData>();

            if (CurrentGuns == null)
                return;

            foreach (GunScriptableObject gun in CurrentGuns)
            {
                if (gun == null)
                    continue;

                data.ownedGunIDs.Add(gun.ID);
                data.ammo.Add(new GunAmmoSaveData
                {
                    gunID = gun.ID,
                    clipAmmo = gun.AmmoConfig.currentClip,
                    reserveAmmo = gun.AmmoConfig.currentAmmo
                });

                if (!gunCatalogLookup.ContainsKey(gun.ID))
                    Debug.LogWarning($"Owned gun '{gun.name}' (ID {gun.ID}) is not in the save/load catalog and cannot be restored after restarting the game.", this);
            }
        }

        public void LoadData(GunSaveData data)
        {
            // Restore inventory without activating anything — Arsenal handles activation
            
            if (gunCatalogLookup.Count == 0)
                RebuildGunCatalogLookup();

            CurrentGuns = new List<GunScriptableObject>();

            if (data.ownedGunIDs != null)
            {
                foreach (int gunID in data.ownedGunIDs)
                {
                    if (gunCatalogLookup.TryGetValue(gunID, out GunScriptableObject gun) && gun != null)
                    {
                        if (!CurrentGuns.Contains(gun))
                            CurrentGuns.Add(gun);
                    }
                    else
                    {
                        Debug.LogWarning($"Saved gun ID {gunID} is not present in the GunStateMachine save/load catalog.", this);
                    }
                }
            }

            RebuildGunLookup();

            // Restore ammo counts
            if (data.ammo == null)
                return;

            foreach (GunAmmoSaveData ammoData in data.ammo)
            {
                if (!gunLookup.TryGetValue(ammoData.gunID, out GunScriptableObject gun) || gun == null)
                    continue;

                gun.AmmoConfig.currentClip = ammoData.clipAmmo;
                gun.AmmoConfig.currentAmmo = ammoData.reserveAmmo;
            }
        }

        /// <summary>
        /// Called by Arsenal.LoadData when guns are the active weapon mode.
        /// Spawns the correct gun model and enters a ready state.
        /// </summary>
        public void SetupAfterLoad(GunSaveData data)
        {
            if (!gunLookup.TryGetValue(data.activeGunID, out GunScriptableObject savedActiveGun) || savedActiveGun == null)
            {
                if (CurrentGunModel != null)
                    CurrentGunModel.SetActive(false);

                ActiveGunScriptable = null;
                CurrentGunModel = null;
                Debug.Log("GunStateMachine.SetupAfterLoad: No active gun to set up.");
                anim.Play("empty_rest");
                return;
            }

            ActiveGunScriptable = savedActiveGun;
            currentID = data.currentID;

            SetupGun();

            // Skip equip animation — go straight to ready
            currentState = states.Rest();
            currentState.EnterState();
            fullyEquipped = true;
        }
    }

    [System.Serializable]
    public struct GunSaveData
    {
        public List<int> ownedGunIDs;
        public int activeGunID;
        public int currentID;
        public List<GunAmmoSaveData> ammo;
    }

    [System.Serializable]
    public struct GunAmmoSaveData
    {
        public int gunID;
        public int clipAmmo;
        public int reserveAmmo;
    }
}
