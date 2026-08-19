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
            Guns,
            Katana,

            Glove
        }

        [Header("References")]
        [HideInInspector] public GunStateMachine gunStateMachine;
        [HideInInspector] public KatanaStateMachine katanaStateMachine;

        [HideInInspector] public GloveStateMachine gloveStateMachine;

        [Header("Current Mode")]
        public WeaponMode currentMode = WeaponMode.Guns;

        public int currentID = -1;
        public int lastID;

        private void Start()
        {
            if (gunStateMachine == null)
                gunStateMachine = GetComponent<GunStateMachine>();

            if (katanaStateMachine == null)
                katanaStateMachine = GetComponent<KatanaStateMachine>();

            if (gloveStateMachine == null)
                gloveStateMachine = GetComponent<GloveStateMachine>();
        }

        private void Update()
        {
            if (PauseMenu.Instance != null && PauseMenu.Instance.Paused)
                return;

            WeaponSwitch();
        }

        private void WeaponSwitch()
        {
            bool hasWeaponNumberInput = ReadWeaponHotkey(out int selectedID);
            bool hasScrollInput = InputManager.Instance.mouseScroll != 0f;

            if (gunStateMachine.CurrentGuns.Count > 0 && katanaStateMachine.fullyEquipped && 
            katanaStateMachine.CurrentState != katanaStateMachine.states.Parry() && 
            katanaStateMachine.CurrentState != katanaStateMachine.states.LightSlash() &&
            katanaStateMachine.CurrentState != katanaStateMachine.states.HeavyCharge() &&
            katanaStateMachine.CurrentState != katanaStateMachine.states.HeavySlash() &&
            gunStateMachine.CurrentState.CurrentSubstate() != gunStateMachine.states.Shoot())
            {
                if (hasWeaponNumberInput)
                {
                    if (selectedID == -1)
                    {
                        InputManager.Instance.katanaSelect = false;
                        if (currentMode == WeaponMode.Katana)
                        {
                            ScrollToGun();
                            return;
                        }
                        SwitchToKatana();
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

            if (InputManager.Instance.katanaSelect) { selectedID = -1; return true; }

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
            if (currentMode == WeaponMode.Katana)
            {

                if (!katanaStateMachine.fullyEquipped)
                    return;
                int id = lastID;
                lastID = currentID;
                currentMode = WeaponMode.Guns;
                katanaStateMachine.Despawn(katanaStateMachine.ActiveKatanaScriptable);
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



            if (currentMode == WeaponMode.Katana)
            {
                currentMode = WeaponMode.Guns;
                katanaStateMachine.Despawn(katanaStateMachine.ActiveKatanaScriptable);
            }
            lastID = currentID;
            gunStateMachine.SwitchToGun(selectedID);
            currentID = selectedID;
        }

        public void SwitchToKatana()
        {
            lastID = currentID;

            if (currentMode == WeaponMode.Guns)
            {
                currentMode = WeaponMode.Katana;
                gunStateMachine.Despawn(gunStateMachine.ActiveGunScriptable);
                gunStateMachine.fullyEquipped = false;
            }

            katanaStateMachine.SetupKatana();
            currentID = -1;
        }

        // ── Unified Save / Load ──────────────────────────────────

        public void SaveData(ref ArsenalSaveData data)
        {
            data.currentMode = currentMode;
            data.currentID = currentID;
            data.lastID = lastID;

            gunStateMachine.SaveData(ref data.gunData);
            katanaStateMachine.SaveData(ref data.katanaData);
        }

        public void LoadData(ArsenalSaveData data)
        {
            // Despawn whatever is currently active to start clean
            if (gunStateMachine.ActiveGunScriptable != null)
                gunStateMachine.Despawn(gunStateMachine.ActiveGunScriptable);

            if (katanaStateMachine.ActiveKatanaScriptable != null)
                katanaStateMachine.Despawn(katanaStateMachine.ActiveKatanaScriptable);



            // Restore arsenal state
            currentMode = data.currentMode;
            currentID = data.currentID;
            lastID = data.lastID;

            // Load sub-system data
            gunStateMachine.LoadData(data.gunData);
            katanaStateMachine.LoadData(data.katanaData);

            // Activate the correct weapon based on saved mode
            switch (currentMode)
            {
                case WeaponMode.Guns:

                    gunStateMachine.SetupAfterLoad(data.gunData);
                    break;


                case WeaponMode.Katana:

                    katanaStateMachine.SetupAfterLoad();
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
        public KatanaSaveData katanaData;
    }
}
