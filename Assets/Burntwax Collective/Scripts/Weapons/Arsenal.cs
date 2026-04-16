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


            if (gunStateMachine.CurrentState.CurrentSubstate() != gunStateMachine.states.Shoot())
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
                    else
                    {

                        SwitchToGuns(selectedID);

                        return;
                    }
                }

                if (hasScrollInput)
                {

                    ScrollToGun();
                    return;
                }

            }








        }

        private int GetScrollID()
        {
            if (gunStateMachine.CurrentGuns.Count == 0)
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
                nextID = gunStateMachine.CurrentGuns.Count - 1;
            else if (nextID >= gunStateMachine.CurrentGuns.Count)
                nextID = 0;
            return nextID;
        }

        private bool ReadWeaponHotkey(out int selectedID)
        {
            selectedID = currentID;

            if (InputManager.Instance.katanaSelect) { selectedID = -1; return true; }

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


            return false;
        }

        public void ScrollToGun()
        {

            if (currentMode == WeaponMode.Katana)
            {
                int id = lastID;
                lastID = currentID;
                currentMode = WeaponMode.Guns;
                katanaStateMachine.Despawn(katanaStateMachine.ActiveKatanaScriptable);
                gunStateMachine.SwitchToGun(id);
                currentID = id;


            }
            else
            {
                lastID = currentID;
                gunStateMachine.SwitchToGun(GetScrollID());
                currentID = GetScrollID();
            }





        }

        public void SwitchToGuns(int selectedID)
        {
            if (gunStateMachine.CurrentGuns.Count == 0)
                return;
            lastID = currentID;
            if (currentMode == WeaponMode.Katana)
            {
                currentMode = WeaponMode.Guns;
                katanaStateMachine.Despawn(katanaStateMachine.ActiveKatanaScriptable);

            }
            gunStateMachine.SwitchToGun(selectedID);
            currentID = selectedID;
        }

        public void SwitchToKatana()
        {
            lastID = currentID;
            if (katanaStateMachine.KatanaScriptable == null)
                return;

            if (currentMode == WeaponMode.Guns)
            {
                currentMode = WeaponMode.Katana;
                gunStateMachine.Despawn(gunStateMachine.ActiveGunScriptable);
            }
            katanaStateMachine.SetupKatana();
            currentID = -1;
        }
    }
}