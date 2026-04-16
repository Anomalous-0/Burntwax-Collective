using System.Diagnostics;
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
            Katana
        }

        [Header("References")]
        [HideInInspector] public GunStateMachine gunStateMachine;
        [HideInInspector] public KatanaStateMachine katanaStateMachine;

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

            SwitchToGuns();
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

            
            if (hasWeaponNumberInput && selectedID != currentID)
            {
                if (selectedID == -1)
                {
                    lastID = currentID;
                    SwitchToKatana();
                    currentID = selectedID;
                    return;
                }
                else
                {
                    lastID = currentID;
                    SwitchToGuns();
                    gunStateMachine.HandleGunSwitching(selectedID);
                    currentID = selectedID;
                }

            }
        }

        private void HandleGunSelectionInputs()
        {

            // int directGunID = GetDirectGunIDInput();
            // if (directGunID != -1 && directGunID != gunStateMachine.currentID && gunStateMachine.CurrentGuns.Exists(g => g.ID == directGunID))
            // {

            //     if (currentMode == WeaponMode.Katana)
            //     {
            //         SwitchToGuns();
            //         return;
            //     }
            //     gunStateMachine.SelectGunByID(directGunID);
            //     return;
            // }

            // float scroll = InputManager.Instance.mouseScroll;
            // if (scroll != 0)
            // {
            //     if (currentMode == WeaponMode.Katana)
            //     {
            //         SwitchToGuns();
            //         return;
            //     }

            //     gunStateMachine.ScrollGun(scroll);
            // }
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
            // if (Input.GetKeyDown(KeyCode.Alpha5)) { selectedID = 4; return true; }
            // if (Input.GetKeyDown(KeyCode.Alpha6)) { selectedID = 5; return true; }
            // if (Input.GetKeyDown(KeyCode.Alpha7)) { selectedID = 6; return true; }
            // if (Input.GetKeyDown(KeyCode.Alpha8)) { selectedID = 7; return true; }
            // if (Input.GetKeyDown(KeyCode.Alpha9)) { selectedID = 8; return true; }
            if (InputManager.Instance.katanaSelect) { selectedID = -1; return true; }

            return false;
        }

        public void SwitchToGuns()
        {
            // if (katanaStateMachine != null)
            // katanaStateMachine.StowFromArsenal();

            // if (gunStateMachine != null)
            // gunStateMachine.EquipFromArsenal();

            currentMode = WeaponMode.Guns;
        }

        public void SwitchToKatana()
        {
            // if (katanaStateMachine == null || !katanaStateMachine.HasKatana())
            //     return;

            // if (gunStateMachine != null)
            // gunStateMachine.StowFromArsenal();

            // katanaStateMachine.EquipFromArsenal();
            currentMode = WeaponMode.Katana;
        }
    }
}