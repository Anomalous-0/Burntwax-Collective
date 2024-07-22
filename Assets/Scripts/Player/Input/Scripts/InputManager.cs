using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour {
    
    public Vector2 MoveInput { get; private set; } = Vector2.zero;
    public Vector2 LookInput { get; private set; } = Vector2.zero; 
    public bool InvertMouseY { get; private set; } = true;

    public bool interactIsPressed = false;
    public bool aimIsPressed  = false;
    public bool moveIsPressed { get; private set; } = false;
    public bool sprintIsPressed  = false;
    public bool crouchIsPressed { get; private set; } = false;
    public bool jumpIsPressed { get; private set; } = false;
    
    public bool shootIsPressed = false;
    public int scrolled = 0;

    public bool useGloves = false;
    public bool gloveShootIsPressed { get; private set; } = false;

    PlayerInput input;

    private void OnEnable(){
        // enable things like on foot, swimming, in air, wall climing???
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
        input.OnFoot.Aim.canceled += SetAim;

        input.OnFoot.Crouch.started += SetCrouch;
        input.OnFoot.Crouch.canceled += SetCrouch;

        input.OnFoot.Jump.started += SetJump;
        input.OnFoot.Jump.canceled += SetJump;

        input.OnFoot.Shoot.started += SetShoot;
        input.OnFoot.Shoot.canceled += SetShoot;

        input.OnFoot.WeaponScroll.started += SetScroll;
        input.OnFoot.WeaponScroll.canceled += SetScroll;

        input.OnFoot.Interact.started += SetInteract;
        input.OnFoot.Interact.canceled += SetInteract;

        input.OnFoot.Gloves.started += SetGloves;
        input.OnFoot.Gloves.canceled += SetGloves;

        input.OnFoot.GloveShoot.started += SetGloveShoot;
        input.OnFoot.GloveShoot.canceled += SetGloveShoot;

        
    }

    // Disabling "unsubscribes"
    private void OnDisable(){
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

        input.OnFoot.Shoot.started -= SetShoot;
        input.OnFoot.Shoot.canceled -= SetShoot;

        input.OnFoot.WeaponScroll.started -= SetScroll;
        input.OnFoot.WeaponScroll.canceled -= SetScroll;

        input.OnFoot.Interact.started -= SetInteract;
        input.OnFoot.Interact.canceled -= SetInteract;

        input.OnFoot.GloveShoot.started -= SetGloveShoot;
        input.OnFoot.GloveShoot.canceled -= SetGloveShoot;


        input.OnFoot.Disable();
    }


    private void Update() { 
       
       
    }

    // These two functions handle the reading of the look and moving 2d values given from the player
    private void SetMove(InputAction.CallbackContext ctx) {
        MoveInput = ctx.ReadValue<Vector2>();
        moveIsPressed = !(MoveInput == Vector2.zero);
    }

    private void SetLook(InputAction.CallbackContext ctx) {
        LookInput = ctx.ReadValue<Vector2>();
    }

    // checks if context has "started" 
    private void SetSprint(InputAction.CallbackContext ctx) {
        sprintIsPressed = ctx.started;
    }

    private void SetAim(InputAction.CallbackContext ctx) {
        aimIsPressed = ctx.started;
    }

    private void SetCrouch(InputAction.CallbackContext ctx) {
        crouchIsPressed = ctx.started;
    }

    private void SetJump(InputAction.CallbackContext ctx) {
        jumpIsPressed = ctx.started;
    }

    private void SetShoot(InputAction.CallbackContext ctx) {
        shootIsPressed = ctx.started;
    }

    private void SetScroll(InputAction.CallbackContext ctx) {
        scrolled = ctx.ReadValue<int>();
    }

    private void SetInteract(InputAction.CallbackContext ctx) {
        interactIsPressed = ctx.started;
    }

    private void SetGloves(InputAction.CallbackContext ctx) {
        useGloves = ctx.started;
    }

    private void SetGloveShoot(InputAction.CallbackContext ctx) {
        gloveShootIsPressed = ctx.started;
    }

}

//attract = ctx.ReadValue<float>();
