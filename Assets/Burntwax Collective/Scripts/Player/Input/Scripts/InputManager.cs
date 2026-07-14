using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
        public int weaponSelectID { get; private set; } = -1;
        public bool HasWeaponSelectInput { get { return weaponSelectID >= 0; } }
        [HideInInspector] public bool katanaSelect;

        // ── Glove gesture API ─────────────────────────────────────
        // The Gloves button (F) is a plain Button now; all tap / double-tap / hold-charge
        // disambiguation happens here so the GloveStateMachine only reads semantic flags.
        //   • gloveEquipped        – written BY GloveStateMachine every frame (single source of
        //                            truth). The recognizer reads it to decide whether to defer a
        //                            single tap (only deferred while equipped), and the ADS gate /
        //                            GloveStateMachine uses it for the arm override too.
        //   • gloveTap             – committed single tap (one-frame pulse). Instant while
        //                            unequipped (equip is snappy); deferred by the double-tap
        //                            window while equipped so it can't be the first half of a
        //                            double tap.
        //   • gloveDoubleTap       – one-frame pulse, only emitted while equipped.
        //   • gloveCharging        – continuous, true while a hold is charging (equipped only).
        //   • gloveChargeNormalized– 0..1 charge progress, capped at gloveChargeDuration.
        //   • gloveBlastFired      – one-frame pulse on release of a charge; gloveBlastCharge holds
        //                            the captured 0..1 charge at that instant.
        // Pulses are cleared by GloveStateMachine via ConsumeGloveGestures() after it ticks.
        [HideInInspector] public bool gloveEquipped;
        [HideInInspector] public bool gloveTap;
        [HideInInspector] public bool gloveDoubleTap;
        [HideInInspector] public bool gloveCharging;
        [HideInInspector] public float gloveChargeNormalized;
        [HideInInspector] public bool gloveBlastFired;
        [HideInInspector] public float gloveBlastCharge;

        [Header("Glove Gesture Tuning")]
        [SerializeField] float gloveHoldThreshold = 0.18f;   // press longer than this = charge, not tap
        [SerializeField] float gloveDoubleTapWindow = 0.25f; // second tap must land within this
        [SerializeField] float gloveChargeDuration = 1f;     // seconds to reach full charge (cap)

        // recognizer runtime state
        bool gloveButtonDown;
        float glovePressStartTime;
        bool glovePendingTap;
        float glovePendingTapTime;
        bool gloveChargeActive;


        [HideInInspector] public bool aimIsPressed = false;
        [HideInInspector] public bool attackIsPressed = false;

        [HideInInspector] public bool slash;
        [HideInInspector] public bool parry;
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
            arsenal = FindAnyObjectByType<Arsenal>();
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
            input.OnFoot.Gloves.started += OnGlovePressed;
            input.OnFoot.Gloves.canceled += OnGloveReleased;
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
            input.OnFoot.Gloves.started -= OnGlovePressed;
            input.OnFoot.Gloves.canceled -= OnGloveReleased;
            input.OnFoot.Save.started -= SetSave;
            input.OnFoot.Save.canceled -= SetSave;
            input.OnFoot.Load.started -= SetLoad;
            input.OnFoot.Load.canceled -= SetLoad;

            input.OnFoot.Disable();
        }

        // ── MODIFIED Update ────────────────────────────────────
        private void Update()
        {
            // Glove gestures are independent of weapon mode (the glove lives alongside the arsenal),
            // so tick them before the katana-only early-return below.
            TickGloveGestures();

            if (arsenal && arsenal.currentMode != Arsenal.WeaponMode.Katana)
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
        private void SetSelect(InputAction.CallbackContext ctx)
        {
            weaponSelectID = GetWeaponIDFromControl(ctx.control);
        }

        private int GetWeaponIDFromControl(InputControl control)
        {
            KeyControl key = control as KeyControl;
            if (key == null)
                return -1;

            switch (key.keyCode)
            {
                case Key.Digit1:
                case Key.Numpad1:
                    return 0;
                case Key.Digit2:
                case Key.Numpad2:
                    return 1;
                case Key.Digit3:
                case Key.Numpad3:
                    return 2;
                case Key.Digit4:
                case Key.Numpad4:
                    return 3;
                case Key.Digit5:
                case Key.Numpad5:
                    return 4;
                case Key.Digit6:
                case Key.Numpad6:
                    return 5;
                case Key.Digit7:
                case Key.Numpad7:
                    return 6;
                case Key.Digit8:
                case Key.Numpad8:
                    return 7;
                case Key.Digit9:
                case Key.Numpad9:
                    return 8;
                default:
                    return -1;
            }
        }

        public void ConsumeWeaponSwitchInput()
        {
            weaponSelectID = -1;
            mouseScroll = 0f;
        }
        private void SetKatanaSelect(InputAction.CallbackContext ctx) => katanaSelect = ctx.started;
        private void SetInteract(InputAction.CallbackContext ctx) => interactIsPressed = ctx.started;

        // ── Glove gesture recognizer ──────────────────────────────
        // The Gloves button is a plain Button; we get raw press/release here and derive
        // tap / double-tap / hold-charge in TickGloveGestures().
        private void OnGlovePressed(InputAction.CallbackContext ctx)
        {
            gloveButtonDown = true;
            glovePressStartTime = Time.unscaledTime;
            gloveChargeActive = false;
        }

        private void OnGloveReleased(InputAction.CallbackContext ctx)
        {
            gloveButtonDown = false;

            if (gloveChargeActive)
            {
                // A charge was in progress → fire the blast with whatever charge we reached.
                gloveBlastFired = true;
                gloveBlastCharge = gloveChargeNormalized;
                gloveChargeActive = false;
                gloveCharging = false;
                gloveChargeNormalized = 0f;
                glovePendingTap = false;   // a charge is never also a tap
                return;
            }

            // Released before the hold threshold → it's a tap.
            if (!gloveEquipped)
            {
                // Unequipped: the only meaningful action is equip, so commit instantly (no double
                // tap to disambiguate against).
                gloveTap = true;
            }
            else if (glovePendingTap && Time.unscaledTime - glovePendingTapTime <= gloveDoubleTapWindow)
            {
                // Second quick tap → double tap.
                gloveDoubleTap = true;
                glovePendingTap = false;
            }
            else
            {
                // First tap → wait to see if a second one lands (committed in TickGloveGestures).
                glovePendingTap = true;
                glovePendingTapTime = Time.unscaledTime;
            }
        }

        private void TickGloveGestures()
        {
            // Promote a held press into a charge (only while equipped — a hold from unequipped just
            // equips on release).
            if (gloveButtonDown && !gloveChargeActive && gloveEquipped
                && Time.unscaledTime - glovePressStartTime >= gloveHoldThreshold)
            {
                gloveChargeActive = true;
            }

            if (gloveChargeActive)
            {
                gloveCharging = true;
                gloveChargeNormalized = Mathf.Clamp01(
                    (Time.unscaledTime - glovePressStartTime) / gloveChargeDuration);
            }
            else
            {
                gloveCharging = false;
            }

            // Commit a pending single tap once the double-tap window closes with no second tap.
            if (glovePendingTap && Time.unscaledTime - glovePendingTapTime > gloveDoubleTapWindow)
            {
                gloveTap = true;
                glovePendingTap = false;
            }
        }

        /// <summary>
        /// Clears the one-frame glove gesture pulses. Called by GloveStateMachine at the end of its
        /// Update so each tap / double-tap / blast is consumed exactly once.
        /// </summary>
        public void ConsumeGloveGestures()
        {
            gloveTap = false;
            gloveDoubleTap = false;
            gloveBlastFired = false;
        }
        private void SetSave(InputAction.CallbackContext ctx) => saveIsPressed = ctx.started;
        private void SetLoad(InputAction.CallbackContext ctx) => loadIsPressed = ctx.started;
    }
}
