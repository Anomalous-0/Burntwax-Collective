using UnityEngine;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class Arsenal : MonoBehaviour
    {
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
            HandleGunSelectionInputs();
        }

        private void HandleKatanaToggle()
        {
            if (!Input.GetKeyDown(KeyCode.Q))
                return;

            if (currentMode == WeaponMode.Katana)
            {
                SwitchToGuns();
            }
            else if (gunStateMachine.CurrentState.CurrentSubstate() != gunStateMachine.states.Shoot())
            {
                SwitchToKatana();
            }
        }

        private void HandleGunSelectionInputs()
        {

            int directGunID = GetDirectGunIDInput();
            if (directGunID != -1 && directGunID != gunStateMachine.currentID && gunStateMachine.CurrentGuns.Exists(g => g.ID == directGunID))
            {

                if (currentMode == WeaponMode.Katana)
                {
                    SwitchToGuns();
                    return;
                }
                gunStateMachine.SelectGunByID(directGunID);
                return;
            }

            float scroll = InputManager.Instance.mouseScroll;
            if (scroll != 0)
            {
                if (currentMode == WeaponMode.Katana)
                {
                    SwitchToGuns();
                    return;
                }

                gunStateMachine.ScrollGun(scroll);
            }
        }

        private int GetDirectGunIDInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) return 3;
            if (Input.GetKeyDown(KeyCode.Alpha5)) return 4;
            if (Input.GetKeyDown(KeyCode.Alpha6)) return 5;
            if (Input.GetKeyDown(KeyCode.Alpha7)) return 6;
            if (Input.GetKeyDown(KeyCode.Alpha8)) return 7;
            if (Input.GetKeyDown(KeyCode.Alpha9)) return 8;

            return -1;
        }

        public void SwitchToGuns()
        {
            if (katanaStateMachine != null)
                katanaStateMachine.StowFromArsenal();

            if (gunStateMachine != null)
                gunStateMachine.EquipFromArsenal();

            currentMode = WeaponMode.Guns;
        }

        public void SwitchToKatana()
        {
            if (katanaStateMachine == null || !katanaStateMachine.HasKatana())
                return;

            if (gunStateMachine != null)
                gunStateMachine.StowFromArsenal();

            katanaStateMachine.EquipFromArsenal();
            currentMode = WeaponMode.Katana;
        }
    }
}