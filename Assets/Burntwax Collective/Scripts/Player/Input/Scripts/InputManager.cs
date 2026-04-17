using UnityEngine;
using UnityEngine.InputSystem;

namespace Burntwax
{
    public class InputManager : MonoBehaviour
    {


        public static InputManager Instance;
        public Vector2 MoveInput = Vector2.zero;
        public Vector2 LookInput = Vector2.zero;
        public bool InvertMouseY { get; private set; } = true;

        public bool interactIsPressed = false;

        public bool moveIsPressed { get; private set; } = false;

        [HideInInspector] public bool crouchIsPressed { get; private set; } = false;
        [HideInInspector] public bool jumpIsPressed /*{ get; private set; }*/ = false;
        [HideInInspector] public bool pauseIsPressed = false;


        public float mouseScroll { get; private set; } = 0;
        public bool weaponSelectID { get; private set; }

        [HideInInspector] public bool katanaSelect;

        [HideInInspector] public bool useGloves = false;
        [HideInInspector] public bool gloveShootIsPressed { get; private set; } = false;


        public bool slash;
        public bool parry;


        public bool aimHeld { get; private set; } = false;
        public bool aimPressedThisFrame { get; private set; } = false;
        public bool aimReleasedThisFrame { get; private set; } = false;
        public bool aimIsPressed => aimHeld;

        public bool attackHeld { get; private set; } = false;
        public bool attackPressedThisFrame { get; private set; } = false;
        public bool attackReleasedThisFrame { get; private set; } = false;
        public bool attackIsPressed
        {
            get => attackHeld;
            set => attackHeld = value;
        }

        public Vector2 dragStartScreenPos { get; private set; } = Vector2.zero;
        public Vector2 currentScreenPos { get; private set; } = Vector2.zero;
        public Vector2 dragDelta { get; private set; } = Vector2.zero;
        public Vector2 dragDeltaNormalized { get; private set; } = Vector2.zero;

        public bool reloadIsPressed { get; private set; } = false;
        public bool sprintIsPressed { get; private set; } = false;


        public bool saveIsPressed { get; private set; } = false;
        public bool loadIsPressed { get; private set; } = false;

        [SerializeField] Arsenal arsenal;

        PlayerInput input;
        private bool slashDragActive = false;
        private bool hasReleaseDragVector = false;
        private Vector2 releaseDragVector = Vector2.zero;

        void Awake()
        {
            Instance = this;

            if (arsenal == null)
            {
                arsenal = GetComponentInParent<Arsenal>();
            }

            Instance = this;
            if (arsenal == null)
            {
                arsenal = FindFirstObjectByType<Arsenal>();
            }
        }

        private void OnEnable()
        {
            input = new PlayerInput();
            input.OnFoot.Enable();
            // The increment "subscribes" to the fucntion and plus or minusing it prevents memory leaks
            input.OnFoot.Move.performed += SetMove;
            input.OnFoot.Move.canceled += SetMove;

            input.OnFoot.Look.performed += SetLook;
            input.OnFoot.Look.canceled += SetLook;

            input.OnFoot.Sprint.started += SetSprint;
            input.OnFoot.Sprint.canceled += SetSprint;

            input.OnFoot.Aim.started += SetAim;
            input.OnFoot.Aim.performed += SetAim;
            input.OnFoot.Aim.canceled += SetAim;

            input.OnFoot.Crouch.started += SetCrouch;
            input.OnFoot.Crouch.canceled += SetCrouch;

            input.OnFoot.Jump.started += SetJump;
            input.OnFoot.Jump.canceled += SetJump;

            input.OnFoot.Pause.started += SetPause;
            input.OnFoot.Pause.canceled += SetPause;

            input.OnFoot.Attack.started += SetAttack;
            input.OnFoot.Attack.performed += SetAttack;
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

        // Disabling "unsubscribes"
        private void OnDisable()
        {
            input.OnFoot.Move.performed -= SetMove;
            input.OnFoot.Move.canceled -= SetMove;

            input.OnFoot.Look.performed -= SetLook;
            input.OnFoot.Look.canceled -= SetLook;

            input.OnFoot.Sprint.started -= SetSprint;
            input.OnFoot.Sprint.canceled -= SetSprint;

            input.OnFoot.Aim.started -= SetAim;
            input.OnFoot.Aim.performed -= SetAim;
            input.OnFoot.Aim.canceled -= SetAim;

            input.OnFoot.Crouch.started -= SetCrouch;
            input.OnFoot.Crouch.canceled -= SetCrouch;

            input.OnFoot.Jump.started -= SetJump;
            input.OnFoot.Jump.canceled -= SetJump;

            input.OnFoot.Pause.started -= SetPause;
            input.OnFoot.Pause.canceled -= SetPause;

            input.OnFoot.Attack.started -= SetAttack;
            input.OnFoot.Attack.performed -= SetAttack;
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


        private void Update()
        {


        }

        private void LateUpdate()
        {
            attackPressedThisFrame = false;
            attackReleasedThisFrame = false;
            aimPressedThisFrame = false;
            aimReleasedThisFrame = false;
        }

        // These two functions handle the reading of the look and moving 2d values given from the player
        private void SetMove(InputAction.CallbackContext ctx)
        {
            MoveInput = ctx.ReadValue<Vector2>();
            moveIsPressed = !(MoveInput == Vector2.zero);
        }

        private void SetLook(InputAction.CallbackContext ctx)
        {
            LookInput = ctx.ReadValue<Vector2>();
        }

        // checks if context has "started" 
        private void SetSprint(InputAction.CallbackContext ctx)
        {
            sprintIsPressed = ctx.started;
        }

        private void SetAim(InputAction.CallbackContext ctx)
        {
            bool isPressed = !ctx.canceled && ctx.ReadValueAsButton();

            if (ctx.started)
            {
                aimPressedThisFrame = true;
                if (CanCaptureKatanaDrag())
                {
                    BeginSlashDrag();
                }
            }

            if (ctx.canceled && aimHeld)
            {
                aimReleasedThisFrame = true;
            }

            aimHeld = isPressed;
            FinalizeSlashDrag();
        }

        private void SetCrouch(InputAction.CallbackContext ctx)
        {
            crouchIsPressed = ctx.started;
        }

        private void SetJump(InputAction.CallbackContext ctx)
        {
            jumpIsPressed = ctx.started;
        }

        private void SetPause(InputAction.CallbackContext ctx)
        {
            pauseIsPressed = ctx.started;
        }

        private void SetAttack(InputAction.CallbackContext ctx)
        {
            attackIsPressed = ctx.started;
            bool isPressed = !ctx.canceled && ctx.ReadValueAsButton();

            if (ctx.started)
            {
                attackPressedThisFrame = true;
                if (CanCaptureKatanaDrag())
                {
                    BeginSlashDrag();
                }
            }

            if (ctx.canceled && attackHeld)
            {
                attackReleasedThisFrame = true;
            }

            attackHeld = isPressed;
            FinalizeSlashDrag();
        }

        private void SetReload(InputAction.CallbackContext ctx)
        {
            reloadIsPressed = ctx.started;
        }

        private void SetScroll(InputAction.CallbackContext ctx)
        {
            mouseScroll = ctx.ReadValue<float>();
        }

        private void SetSelect(InputAction.CallbackContext ctx)
        {
            weaponSelectID = ctx.started;
        }

        private void SetKatanaSelect(InputAction.CallbackContext ctx)
        {
            katanaSelect = ctx.started;
        }

        private void SetInteract(InputAction.CallbackContext ctx)
        {
            interactIsPressed = ctx.started;
        }

        private void SetGloves(InputAction.CallbackContext ctx)
        {
            useGloves = ctx.started;
        }

        private void SetGloveShoot(InputAction.CallbackContext ctx)
        {
            gloveShootIsPressed = ctx.started;
        }


        private void SetSave(InputAction.CallbackContext ctx)
        {
            saveIsPressed = ctx.started;
        }

        private void SetLoad(InputAction.CallbackContext ctx)
        {
            loadIsPressed = ctx.started;
        }



        public void BeginSlashDrag()
        {
        }


        private void FinalizeSlashDrag()
        {
        }

        private bool CanCaptureKatanaDrag()
        {
            return arsenal != null && arsenal.currentMode == Arsenal.WeaponMode.Katana;
        }

        private Vector2 ReadMouseScreenPosition()
        {
            if (Mouse.current == null)
            {
                return Vector2.zero;
            }

            return Mouse.current.position.ReadValue();
        }
    }
}
