using UnityEngine;

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

            HandleKatanaToggle();
            gunStateMachine.HandleGunSwitching();
           
            // HandleGunSelectionInputs();
        }

        private void HandleKatanaToggle()
        {
            if (InputManager.Instance.katanaSelect)
            {
                if (currentMode == WeaponMode.Katana)
                {
                    // Switch to Gun system
                }

                else if (gunStateMachine.CurrentState.CurrentSubstate() != gunStateMachine.states.Shoot())
                {
                    // Switch to Katana system, currentID is -1
                    lastID = currentID;
                    currentID = -1;

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

        private void WeaponHotkeySelect()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) currentID = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) currentID = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) currentID = 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) currentID = 3;
            if (Input.GetKeyDown(KeyCode.Alpha5)) currentID = 4;
            if (Input.GetKeyDown(KeyCode.Alpha6)) currentID = 5;
            if (Input.GetKeyDown(KeyCode.Alpha7)) currentID = 6;
            if (Input.GetKeyDown(KeyCode.Alpha8)) currentID = 7;
            if (Input.GetKeyDown(KeyCode.Alpha9)) currentID = 8;


        }

        public void SwitchToGuns()
        {
            if (katanaStateMachine != null)
                // katanaStateMachine.StowFromArsenal();

                if (gunStateMachine != null)
                    // gunStateMachine.EquipFromArsenal();

                    currentMode = WeaponMode.Guns;
        }

        public void SwitchToKatana()
        {
            if (katanaStateMachine == null || !katanaStateMachine.HasKatana())
                return;

            if (gunStateMachine != null)
                // gunStateMachine.StowFromArsenal();

                // katanaStateMachine.EquipFromArsenal();
                currentMode = WeaponMode.Katana;
        }
    }
}