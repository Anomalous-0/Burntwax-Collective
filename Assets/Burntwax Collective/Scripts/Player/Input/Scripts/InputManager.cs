using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Burntwax
{
    public class InputManager : MonoBehaviour
    {
        private Arsenal arsenal;

        public static InputManager Instance;
        public Vector2 MoveInput = Vector2.zero;
        public Vector2 LookInput = Vector2.zero;
        public bool InvertMouseY { get; private set; } = true;

        [HideInInspector] public bool interactIsPressed = false;
        public bool moveIsPressed { get; private set; } = false;
        public bool crouchIsPressed { get; private set; } = false;
        [HideInInspector] public bool jumpIsPressed = false;
        [HideInInspector] public bool pauseIsPressed = false;

        public float mouseScroll { get; private set; } = 0;
        public bool weaponSelectID { get; private set; }
        [HideInInspector] public bool katanaSelect;
        [HideInInspector] public bool useGloves = false;
        public bool gloveShootIsPressed { get; private set; } = false;

        public bool aimIsPressed = false;
        public bool attackIsPressed = false;

        public bool slash;
        public bool parry;
        public bool reloadIsPressed { get; private set; } = false;
        public bool sprintIsPressed { get; private set; } = false;

        public bool saveIsPressed { get; private set; } = false;
        public bool loadIsPressed { get; private set; } = false;

        // ── NEW: Directional tracking ──────────────────────────
        [HideInInspector] public bool isTrackingSlash = false;
        [HideInInspector] public bool isTrackingParry = false;
        [HideInInspector] public Vector2 slashStartDirection;
        [HideInInspector] public Vector2 parryStartDirection;
        // ────────────────────────────────────────────────────────

        PlayerInput input;

        void Awake()
        {
            Instance = this;
            arsenal = FindFirstObjectByType<Arsenal>();
        }

        // ── OnEnable / OnDisable remain IDENTICAL to your current code ──
        // (subscribe/unsubscribe all the same actions — omitted for brevity,
        //  nothing changes here)

        private void OnEnable()
        {
            input = new PlayerInput();
            input.OnFoot.Enable();

            input.OnFoot.Move.performed += SetMove;
            input.OnFoot.Move.canceled += SetMove;
            input.OnFoot.Look.performed += SetLook;
            input.OnFoot.Look.canceled += SetLook;
            input.OnFoot.Sprint.started += SetSprint;
            input.OnFoot.Sprint.canceled += SetSprint;
            input.OnFoot.Aim.started += SetAim;
            input.OnFoot.Aim.canceled += SetAim;
            input.OnFoot.Crouch.started += SetCrouch;
            input.OnFoot.Crouch.canceled += SetCrouch;
            input.OnFoot.Jump.started += SetJump;
            input.OnFoot.Jump.canceled += SetJump;
            input.OnFoot.Pause.started += SetPause;
            input.OnFoot.Pause.canceled += SetPause;
            input.OnFoot.Attack.started += SetAttack;
            input.OnFoot.Attack.canceled += SetAttack;
            input.OnFoot.Reload.started += SetReload;
            input.OnFoot.Reload.canceled += SetReload;
            input.OnFoot.WeaponScroll.performed += SetScroll;
            input.OnFoot.WeaponScroll.canceled += SetScroll;
            input.OnFoot.WeaponSelect.started += SetSelect;
            input.OnFoot.WeaponSelect.canceled += SetSelect;
            input.OnFoot.KatanaSelect.started += SetKatanaSelect;
            input.OnFoot.KatanaSelect.canceled += SetKatanaSelect;
            input.OnFoot.Interact.started += SetInteract;
            input.OnFoot.Interact.canceled += SetInteract;
            input.OnFoot.Gloves.started += SetGloves;
            input.OnFoot.Gloves.canceled += SetGloves;
            input.OnFoot.GloveShoot.started += SetGloveShoot;
            input.OnFoot.GloveShoot.canceled += SetGloveShoot;
            input.OnFoot.Save.started += SetSave;
            input.OnFoot.Save.canceled += SetSave;
            input.OnFoot.Load.started += SetLoad;
            input.OnFoot.Load.canceled += SetLoad;
        }

        private void OnDisable()
        {
            input.OnFoot.Move.performed -= SetMove;
            input.OnFoot.Move.canceled -= SetMove;
            input.OnFoot.Look.performed -= SetLook;
            input.OnFoot.Look.canceled -= SetLook;
            input.OnFoot.Sprint.started -= SetSprint;
            input.OnFoot.Sprint.canceled -= SetSprint;
            input.OnFoot.Aim.started -= SetAim;
            input.OnFoot.Aim.canceled -= SetAim;
            input.OnFoot.Crouch.started -= SetCrouch;
            input.OnFoot.Crouch.canceled -= SetCrouch;
            input.OnFoot.Jump.started -= SetJump;
            input.OnFoot.Jump.canceled -= SetJump;
            input.OnFoot.Pause.started -= SetPause;
            input.OnFoot.Pause.canceled -= SetPause;
            input.OnFoot.Attack.started -= SetAttack;
            input.OnFoot.Attack.canceled -= SetAttack;
            input.OnFoot.Reload.started -= SetReload;
            input.OnFoot.Reload.canceled -= SetReload;
            input.OnFoot.WeaponScroll.performed -= SetScroll;
            input.OnFoot.WeaponScroll.canceled -= SetScroll;
            input.OnFoot.WeaponSelect.started -= SetSelect;
            input.OnFoot.WeaponSelect.canceled -= SetSelect;
            input.OnFoot.KatanaSelect.started -= SetKatanaSelect;
            input.OnFoot.KatanaSelect.canceled -= SetKatanaSelect;
            input.OnFoot.Interact.started -= SetInteract;
            input.OnFoot.Interact.canceled -= SetInteract;
            input.OnFoot.GloveShoot.started -= SetGloveShoot;
            input.OnFoot.GloveShoot.canceled -= SetGloveShoot;
            input.OnFoot.Save.started -= SetSave;
            input.OnFoot.Save.canceled -= SetSave;
            input.OnFoot.Load.started -= SetLoad;
            input.OnFoot.Load.canceled -= SetLoad;

            input.OnFoot.Disable();
        }

        // ── MODIFIED Update ────────────────────────────────────
        private void Update()
        {
            if (arsenal.currentMode != Arsenal.WeaponMode.Katana)
                return;

            bool enoughForce = Math.Abs(LookInput.magnitude) > 0.95f;

            // ── SLASH tracking ──
            if (enoughForce && attackIsPressed && !isTrackingSlash)
            {
                // First qualifying frame — trigger state transition + start tracking
                isTrackingSlash = true;
                slashStartDirection = LookInput.normalized;
                slash = true;  // consumed by KatanaIdleState.CheckSwitchStates()
            }
            else if (isTrackingSlash && !attackIsPressed)
            {
                // Button released — stop tracking
                // (the state's timeout will handle returning to idle)
                isTrackingSlash = false;
            }

            // ── PARRY tracking ──
            if (enoughForce && aimIsPressed && !isTrackingParry)
            {
                isTrackingParry = true;
                parryStartDirection = LookInput.normalized;
                parry = true;
            }
            else if (isTrackingParry && !aimIsPressed)
            {
                isTrackingParry = false;
            }
        }
        private void SetMove(InputAction.CallbackContext ctx)
        {
            MoveInput = ctx.ReadValue<Vector2>();
            moveIsPressed = !(MoveInput == Vector2.zero);
        }

        private void SetLook(InputAction.CallbackContext ctx)
        {
            LookInput = ctx.ReadValue<Vector2>();
        }

        private void SetSprint(InputAction.CallbackContext ctx) => sprintIsPressed = ctx.started;
        private void SetAim(InputAction.CallbackContext ctx) => aimIsPressed = ctx.started;
        private void SetCrouch(InputAction.CallbackContext ctx) => crouchIsPressed = ctx.started;
        private void SetJump(InputAction.CallbackContext ctx) => jumpIsPressed = ctx.started;
        private void SetPause(InputAction.CallbackContext ctx) => pauseIsPressed = ctx.started;
        private void SetAttack(InputAction.CallbackContext ctx) => attackIsPressed = ctx.started;
        private void SetReload(InputAction.CallbackContext ctx) => reloadIsPressed = ctx.started;
        private void SetScroll(InputAction.CallbackContext ctx) => mouseScroll = ctx.ReadValue<float>();
        private void SetSelect(InputAction.CallbackContext ctx) => weaponSelectID = ctx.started;
        private void SetKatanaSelect(InputAction.CallbackContext ctx) => katanaSelect = ctx.started;
        private void SetInteract(InputAction.CallbackContext ctx) => interactIsPressed = ctx.started;
        private void SetGloves(InputAction.CallbackContext ctx) => useGloves = ctx.started;
        private void SetGloveShoot(InputAction.CallbackContext ctx) => gloveShootIsPressed = ctx.started;
        private void SetSave(InputAction.CallbackContext ctx) => saveIsPressed = ctx.started;
        private void SetLoad(InputAction.CallbackContext ctx) => loadIsPressed = ctx.started;
    }
}