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
        public GunStateMachine gunStateMachine;
        public KatanaStateMachine katanaStateMachine;

        [Header("Current Mode")]
        public WeaponMode currentMode = WeaponMode.Guns;

        [Header("Switch Settings")]
        [SerializeField] private float switchCooldown = 0.15f;

        private float switchTimer;

        private void Start()
        {
            if (gunStateMachine == null)
                gunStateMachine = GetComponent<GunStateMachine>();

            if (katanaStateMachine == null)
                katanaStateMachine = GetComponent<KatanaStateMachine>();

            ForceSwitchToGuns();
        }

        private void Update()
        {
            if (PauseMenu.Instance != null && PauseMenu.Instance.Paused)
                return;

            if (switchTimer > 0f)
                switchTimer -= Time.deltaTime;

            HandleKatanaToggle();
            HandleGunSelectionInputs();
        }

        private void HandleKatanaToggle()
        {
            if (!Input.GetKeyDown(KeyCode.Q))
                return;

            if (switchTimer > 0f)
                return;

            if (currentMode == WeaponMode.Katana)
            {
                SwitchToGuns();
            }
            else if (gunStateMachine.anim.GetCurrentAnimatorStateInfo(0).IsName("equip_" + gunStateMachine.ActiveGunScriptable.Type))
            {
                SwitchToKatana();
            }
        }

        private void HandleGunSelectionInputs()
        {
            if (switchTimer > 0f)
                return;

            int directGunID = GetDirectGunIDInput();
            if (directGunID != -1)
            {
                SwitchToGuns();
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

        public void SwitchToKatana()
        {
            if (currentMode == WeaponMode.Katana)
                return;

            if (katanaStateMachine == null || !katanaStateMachine.HasKatana())
                return;

            if (gunStateMachine != null)
                gunStateMachine.StowFromArsenal();

            katanaStateMachine.EquipFromArsenal();
            currentMode = WeaponMode.Katana;
            switchTimer = switchCooldown;
        }

        public void SwitchToGuns()
        {
            if (currentMode == WeaponMode.Guns)
                return;

            if (katanaStateMachine != null)
                katanaStateMachine.StowFromArsenal();

            if (gunStateMachine != null)
                gunStateMachine.EquipFromArsenal();

            currentMode = WeaponMode.Guns;
            switchTimer = switchCooldown;
        }

        public void ForceSwitchToGuns()
        {
            if (katanaStateMachine != null)
                katanaStateMachine.StowFromArsenal();

            if (gunStateMachine != null)
                gunStateMachine.EquipFromArsenal();

            currentMode = WeaponMode.Guns;
            switchTimer = 0f;
        }

        public void ForceSwitchToKatana()
        {
            if (katanaStateMachine == null || !katanaStateMachine.HasKatana())
                return;

            if (gunStateMachine != null)
                gunStateMachine.StowFromArsenal();

            katanaStateMachine.EquipFromArsenal();
            currentMode = WeaponMode.Katana;
            switchTimer = 0f;
        }
    }
}