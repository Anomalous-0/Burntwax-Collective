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

            crosshair = player.GetComponentInChildren<PlayerCrosshair>();
            playerAudioSource = player.GetComponent<AudioSource>();

            RebuildGunLookup();

            arsenal = GetComponent<Arsenal>();
        }

        private void Update()
        {
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

        public bool SwitchToGun(int newID)
        {
            if (newID == arsenal.currentID)
                return false;

            if (!gunLookup.TryGetValue(newID, out GunScriptableObject newGun) || newGun == null)
            {
                Debug.LogError($"No GunScriptableObject found for gun ID: {newID}");
                return false;
            }

            currentID = newID;

            if (ActiveGunScriptable != null)
                Despawn(ActiveGunScriptable);

            ActiveGunScriptable = newGun;
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
            data.CurrentGuns = new List<GunScriptableObject>(CurrentGuns);
            data.ActiveGunScriptable = ActiveGunScriptable;
            data.GunModel = CurrentGunModel;
            data.currentID = currentID;

            int count = CurrentGuns.Count;
            data.clipAmmo = new List<int>(count);
            data.reserveAmmo = new List<int>(count);

            for (int i = 0; i < count; i++)
            {
                GunScriptableObject gun = CurrentGuns[i];
                if (gun != null)
                {
                    data.clipAmmo.Add(gun.AmmoConfig.currentClip);
                    data.reserveAmmo.Add(gun.AmmoConfig.currentAmmo);
                }
                else
                {
                    data.clipAmmo.Add(0);
                    data.reserveAmmo.Add(0);
                }
            }
        }

        public void LoadData(GunSaveData data)
        {
            // Restore inventory without activating anything — Arsenal handles activation
            CurrentGuns = data.CurrentGuns ?? new List<GunScriptableObject>();
            RebuildGunLookup();

            // Restore ammo counts
            int ammoCount = Mathf.Min(CurrentGuns.Count, data.clipAmmo?.Count ?? 0, data.reserveAmmo?.Count ?? 0);
            for (int i = 0; i < ammoCount; i++)
            {
                CurrentGuns[i].AmmoConfig.currentClip = data.clipAmmo[i];
                CurrentGuns[i].AmmoConfig.currentAmmo = data.reserveAmmo[i];
            }
        }

        /// <summary>
        /// Called by Arsenal.LoadData when guns are the active weapon mode.
        /// Spawns the correct gun model and enters a ready state.
        /// </summary>
        public void SetupAfterLoad(GunSaveData data)
        {
            if (data.ActiveGunScriptable == null)
            {
                if (CurrentGunModel != null)
                    CurrentGunModel.SetActive(false);

                ActiveGunScriptable = null;
                CurrentGunModel = null;
                Debug.Log("GunStateMachine.SetupAfterLoad: No active gun to set up.");
                anim.Play("empty_rest");
                return;
            }

            ActiveGunScriptable = data.ActiveGunScriptable;
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
        public List<GunScriptableObject> CurrentGuns;
        public GameObject GunModel;
        public GunScriptableObject ActiveGunScriptable;
        public int currentID;
        public List<int> clipAmmo;
        public List<int> reserveAmmo;
    }
}