using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class Arsenal : MonoBehaviour
    {
        [HideInInspector]
        public enum WeaponMode
        {
            Guns = 0,
            [InspectorName("Wakizashi")]
            DualBlade = 1,

            Glove = 2
        }

        [Header("References")]
        [HideInInspector] public GunStateMachine gunStateMachine;
        [UnityEngine.Serialization.FormerlySerializedAs("katanaStateMachine")]
        [HideInInspector] public DualBladeStateMachine dualBladeStateMachine;

        [HideInInspector] public GloveStateMachine gloveStateMachine;

        [Header("Current Mode")]
        public WeaponMode currentMode = WeaponMode.Guns;

        public int currentID = -1;
        public int lastID;

        public bool GameplaySuppressed { get; private set; }

        private GameObject suppressedGunModel;
        private bool suppressedGunWasActive;

        private void Start()
        {
            if (gunStateMachine == null)
                gunStateMachine = GetComponent<GunStateMachine>();

            if (dualBladeStateMachine == null)
                dualBladeStateMachine = GetComponent<DualBladeStateMachine>();

            if (gloveStateMachine == null)
                gloveStateMachine = GetComponent<GloveStateMachine>();
        }

        private void Update()
        {
            if (PauseMenu.Instance != null && PauseMenu.Instance.Paused)
                return;

            if (GameplaySuppressed)
                return;

            WeaponSwitch();
        }

        public void SetGameplaySuppressed(bool suppressed)
        {
            if (GameplaySuppressed == suppressed)
                return;

            GameplaySuppressed = suppressed;

            if (suppressed)
            {
                suppressedGunModel = gunStateMachine != null
                    ? gunStateMachine.CurrentGunModel
                    : null;
                suppressedGunWasActive = suppressedGunModel != null
                    && suppressedGunModel.activeSelf;

                if (suppressedGunModel != null)
                    suppressedGunModel.SetActive(false);

            }
            else
            {
                if (suppressedGunModel != null)
                    suppressedGunModel.SetActive(suppressedGunWasActive);


                suppressedGunModel = null;
            }

            if (gloveStateMachine != null)
                gloveStateMachine.SetGameplaySuppressed(suppressed);

            dualBladeStateMachine?.RefreshBladeModels();
        }

        private void WeaponSwitch()
        {
            if (InputManager.Instance == null)
                return;

            bool hasWeaponNumberInput = ReadWeaponHotkey(out int selectedID);
            bool hasScrollInput = InputManager.Instance.mouseScroll != 0f;

            if (CanSwitchActiveWeapon())
            {
                if (hasWeaponNumberInput)
                {
                    if (selectedID == -1)
                    {
                        InputManager.Instance.dualBladeSelect = false;
                        if (currentMode == WeaponMode.DualBlade)
                        {
                            ScrollToGun();
                            return;
                        }
                        SwitchToDualBlade();
                        return;
                    }

                    SwitchToGuns(selectedID);
                    return;
                }

                if (hasScrollInput)
                {
                    ScrollToGun();
                }
            }
        }

        private bool CanSwitchActiveWeapon()
        {
            if (GameplaySuppressed)
                return false;

            // An inactive weapon's equip flag is normally false. Only the
            // currently selected weapon may block a switch with its action.
            if (currentMode == WeaponMode.DualBlade)
            {
                if (dualBladeStateMachine == null || !dualBladeStateMachine.fullyEquipped)
                    return false;

                DualBladeBaseState state = dualBladeStateMachine.CurrentState;
                DualBladeStateFactory states = dualBladeStateMachine.states;
                return state != states.Parry()
                    && state != states.LightSlash()
                    && state != states.HeavyCharge()
                    && state != states.HeavySlash()
                    && state != states.ParriedRecoil();
            }

            // No active gun means there is no gun animation to wait for.
            if (gunStateMachine == null || gunStateMachine.ActiveGunScriptable == null)
                return true;

            return gunStateMachine.fullyEquipped
                && gunStateMachine.CurrentState?.CurrentSubstate() != gunStateMachine.states.Shoot();
        }

        private int GetScrollID()
        {
            if (gunStateMachine.CurrentGuns.Count == 0)
                return currentID;

            int nextID = currentID;

            if (InputManager.Instance.mouseScroll > 0f)
                nextID++;
            else if (InputManager.Instance.mouseScroll < 0f)
                nextID--;

            int count = gunStateMachine.CurrentGuns.Count;
            if (nextID < 0)
                nextID = count - 1;
            else if (nextID >= count)
                nextID = 0;

            return nextID;
        }

        private bool ReadWeaponHotkey(out int selectedID)
        {
            selectedID = currentID;

            if (InputManager.Instance.dualBladeSelect) { selectedID = -1; return true; }

            if (!InputManager.Instance.HasWeaponSelectInput)
                return false;

            if (Input.GetKeyDown(KeyCode.Alpha1)) { selectedID = 0; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { selectedID = 1; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { selectedID = 2; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { selectedID = 3; return true; }

            return false;
        }

        public void ScrollToGun()
        {
            // Q must leave the blades equipped if there is no gun to return to.
            if (gunStateMachine == null || gunStateMachine.CurrentGuns.Count == 0)
                return;

            if (currentMode == WeaponMode.DualBlade)
            {

                if (!dualBladeStateMachine.fullyEquipped)
                    return;
                int id = lastID;
                lastID = currentID;
                currentMode = WeaponMode.Guns;
                dualBladeStateMachine.Despawn(dualBladeStateMachine.ActiveDualBladeScriptable);
                gunStateMachine.SwitchToGun(id);
                currentID = id;
            }
            else
            {
                int scrollID = GetScrollID();
                lastID = currentID;
                gunStateMachine.SwitchToGun(scrollID);
                currentID = scrollID;
            }
        }

        public void SwitchToGuns(int selectedID)
        {
            if (gunStateMachine.CurrentGuns.Count == 0)
                return;



            if (currentMode == WeaponMode.DualBlade)
            {
                currentMode = WeaponMode.Guns;
                dualBladeStateMachine.Despawn(dualBladeStateMachine.ActiveDualBladeScriptable);
            }
            lastID = currentID;
            gunStateMachine.SwitchToGun(selectedID);
            currentID = selectedID;
        }

        public void SwitchToDualBlade()
        {
            // Validate ownership before hiding the current gun or changing mode.
            if (dualBladeStateMachine == null
                || dualBladeStateMachine.DualBladeScriptable == null
                || currentMode == WeaponMode.DualBlade
                || !CanSwitchActiveWeapon())
                return;

            lastID = currentID;

            if (currentMode == WeaponMode.Guns && gunStateMachine != null)
            {
                gunStateMachine.Despawn(gunStateMachine.ActiveGunScriptable);
                gunStateMachine.fullyEquipped = false;
            }

            currentMode = WeaponMode.DualBlade;
            dualBladeStateMachine.SetupDualBlade();
            currentID = -1;
        }

        // ── Unified Save / Load ──────────────────────────────────

        public void SaveData(ref ArsenalSaveData data)
        {
            data.currentMode = currentMode;
            data.currentID = currentID;
            data.lastID = lastID;

            gunStateMachine.SaveData(ref data.gunData);
            dualBladeStateMachine.SaveData(ref data.dualBladeData);
        }

        public void LoadData(ArsenalSaveData data)
        {
            // Despawn whatever is currently active to start clean
            if (gunStateMachine.ActiveGunScriptable != null)
                gunStateMachine.Despawn(gunStateMachine.ActiveGunScriptable);

            if (dualBladeStateMachine.ActiveDualBladeScriptable != null)
                dualBladeStateMachine.Despawn(dualBladeStateMachine.ActiveDualBladeScriptable);



            // Restore arsenal state
            currentMode = data.currentMode;
            currentID = data.currentID;
            lastID = data.lastID;

            // Load sub-system data
            gunStateMachine.LoadData(data.gunData);
            dualBladeStateMachine.LoadData(data.dualBladeData);

            // Activate the correct weapon based on saved mode
            switch (currentMode)
            {
                case WeaponMode.Guns:

                    gunStateMachine.SetupAfterLoad(data.gunData);
                    break;


                case WeaponMode.DualBlade:

                    dualBladeStateMachine.SetupAfterLoad();
                    break;
            }
        }
    }

    [System.Serializable]
    public struct ArsenalSaveData
    {
        public Arsenal.WeaponMode currentMode;
        public int currentID;
        public int lastID;
        public GunSaveData gunData;
        [UnityEngine.Serialization.FormerlySerializedAs("katanaData")]
        public DualBladeSaveData dualBladeData;
    }
}
