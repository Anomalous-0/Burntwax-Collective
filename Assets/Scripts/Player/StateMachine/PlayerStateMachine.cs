using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour {

    public Transform orientation;
    public Transform player;
    public InputManager input;
    public Rigidbody rb;
    public CapsuleCollider capsuleCollider;
    public CamStateMachine cam;

    [Header("Movement")]

    float yRot;
    [SerializeField] float playerVelocity;
    float moveMultiplier;
    [SerializeField] float groundMultiplier = 1f;
    [SerializeField] float airMultiplier = .7f;
    [SerializeField] float wallMultiplier = 1.2f;
    
    [SerializeField]  float walkVelocity;
    [SerializeField]  float sprintVelocity;
    [SerializeField]  float groundDrag;
    [SerializeField]  float airDrag;
    [SerializeField] float wallDrag;

    float horizontalInput;
    float verticalInput;
    [HideInInspector] public Vector3 moveDirection;


    [Header("Glove")]
    public GloveStateMachine glove;

    [Header("Crouching")]

    [SerializeField] float crouchVelocity;
    [SerializeField] float crouchYScale;
    float startYScale;

    [Header("Jumping")]
    [SerializeField] float jumpForce;
    [SerializeField] bool readyToJump = true;
    // [SerializeField] float jumpCooldown;
    // float cooldownTime;
    

    [Header("Ground Checking")]
    [SerializeField] public bool playerIsGrounded = true;
    [SerializeField] [Range(0.0f, 1.8f)] float groundCheckRadiusMultiplier = 0.9f;
    [SerializeField] [Range(-0.95f, 1.05f)] float groundCheckDistance = 0.01f;
        
    public LayerMask heldRigidbody;
    RaycastHit groundCheckHit = new RaycastHit();
    Vector3 playerCenterPoint;

    
    [Header("Wall Running")]
    [SerializeField] LayerMask wall;
    [SerializeField] float wallRunForce;

    [SerializeField] float wallJumpForce;
    [SerializeField] float wallCheckDistance;
    [SerializeField] float sphereCastRadius; 
    [SerializeField] float minHeight;
    [SerializeField] float wallRunSwitch;
    [SerializeField] RaycastHit leftWallHit;
    [SerializeField] RaycastHit rightWallHit;
    [SerializeField] RaycastHit frontWallHit;
    [SerializeField] RaycastHit backWallHit;
    [SerializeField] bool wallLeft;
    [SerializeField] bool wallRight;
    [SerializeField] bool wallFront;
    [SerializeField] bool wallBack;
    

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    RaycastHit slopeHit;

    public PlayerBaseState currentState;
    public PlayerStateFactory states;

    
    // For states
    public PlayerBaseState CurrentState {get { return currentState; } set { currentState = value; } }
    
    
    // Movement
    public float PlayerVelocity {get { return playerVelocity; } set { playerVelocity = value; } }
    public float MoveMultiplier {get { return moveMultiplier; } set { moveMultiplier = value; } }
    public float VerticalInput {get { return verticalInput; } set { verticalInput = value; } }
    public float HorizontalInput {get { return horizontalInput; } set { horizontalInput = value; }}

    public float GroundMultiplier {get {return groundMultiplier;}}
    public float AirMultiplier {get {return airMultiplier; }}

    public float WallMultiplier {get {return wallMultiplier;}}
    public float SprintVelocity {get { return sprintVelocity; }}
    public float WalkVelocity {get { return walkVelocity; }}
    public float GroundDrag {get { return groundDrag;} }
    public float AirDrag {get { return airDrag;} }
    public float WallDrag {get { return wallDrag;}}


    // Crouching

    public Vector3 LocalScale {get {return transform.localScale;} set { transform.localScale = value;}}
    public float CrouchVelocity {get { return crouchVelocity; }}
    public float StartYScale {get { return startYScale;}}
    public float CrouchYScale {get {return crouchYScale;}}


    // Jumping State
    public float JumpForce {get { return jumpForce; }}
    public bool ReadyToJump  {get { return readyToJump;} set { readyToJump = value; } }



    // Wall Running State
    public LayerMask Wall {get {return wall;} }
    public float WallRunSwitch {get {return wallRunSwitch;} set { wallRunSwitch = value; } }
    // public bool PlayerIsWallRunning{ get {return playerIsWallRunning;} set {playerIsWallRunning = value; } }
    public float WallRunForce {get {return wallRunForce;} }

    public float WallJumpForce {get {return wallJumpForce;} }
    // public float MaxWallRunTime{ get {return maxWallRunTime;} }
    public float WallCheckDistance { get {return wallCheckDistance;}}
    public float SphereCastRadius {get {return sphereCastRadius;}}
    public float MinHeight {get {return minHeight;}}
    // public Vector3 WallNormal {get {return wallNormal;} }
    // public Vector3 WallForward {get {return wallForward;} }
    public RaycastHit RightWallHit {get {return rightWallHit;} set {rightWallHit = value;}}
    public RaycastHit LeftWallHit {get {return leftWallHit;} set {leftWallHit = value;}} 
    public RaycastHit FrontWallHit {get {return frontWallHit;} set {frontWallHit = value;}}
    public RaycastHit BackWallHit {get {return backWallHit;} set {backWallHit = value;}}
    public bool WallLeft {get {return wallLeft;} set {wallLeft = value;}}
    public bool WallRight {get {return wallRight;} set {wallRight = value; }}
    public bool WallFront {get {return wallFront;} set {wallFront = value;}}
    public bool WallBack {get {return wallBack;} set {wallBack = value; }}



    void Start() {
        //rb = GetComponent<Rigidbody>();
        startYScale = transform.localScale.y;
    }
    


    private void Awake(){


        // playerFullHeight = capsuleCollider.height;
        // playerCrouchedHeight = playerFullHeight - crouchAmount;

        states = new PlayerStateFactory(this);
        currentState = states.Grounded();
        currentState.EnterState();
        wallRunSwitch = 0;
    }

    private void LateUpdate() {
        SetRotation();
    }

    private void Update() {
        currentState.UpdateStates();
        
        playerIsGrounded = PlayerGroundCheck();
        GetMoveInput();

       
       
        
        
        
        
        // WallCheck();
    }

    private void FixedUpdate(){
        
        SetOrientation();
        
        PlayerMove();
        SpeedControl();
    
    }

    private void GetMoveInput() {
        horizontalInput = input.MoveInput.x;
        verticalInput = input.MoveInput.y;
    }

    private void SetOrientation() {
        orientation = Camera.main.transform;
    }

    private void SetRotation() {
        player.transform.forward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z).normalized;
    }

    private void PlayerMove() {
        if(currentState != states.Wallrun()) {
            moveDirection = player.forward * verticalInput + player.right * horizontalInput;
        }
        else { 
            moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput; 
        }
        
        rb.AddForce(moveDirection.normalized * 10f * playerVelocity * moveMultiplier);
    }



    private bool PlayerGroundCheck(){
        float sphereCastRadius = capsuleCollider.radius * groundCheckRadiusMultiplier;
        Physics.SphereCast(playerCenterPoint, sphereCastRadius, Vector3.down, out groundCheckHit);

        float sphereCastTravelDistance = capsuleCollider.bounds.extents.y - sphereCastRadius + groundCheckDistance;
        if(Physics.SphereCast(rb.position, sphereCastRadius, Vector3.down, out groundCheckHit, sphereCastTravelDistance)) {
            if(glove.isActiveAndEnabled && glove.currentState == glove.states.Hold()) {
                if(groundCheckHit.collider.GetComponent<Pickupable>()) {
                    return false;
                }

            }
            return true;
        }
        return false;
    }

   
   
   
   
   
   
   private void SpeedControl() {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if(flatVel.magnitude > playerVelocity)
        {
            Vector3 limitedVel = flatVel.normalized * playerVelocity;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }



    private bool CheckSlope() {
        if(Physics.Raycast(playerCenterPoint, Vector3.down, out slopeHit, capsuleCollider.height * 0.5f + 0.3f )) {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 SlopeHandle() {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }



    // private void PlayerSprint(){
    //     Vector3 calculatedPlayerSprintSpeed = playerMoveInput;
    //     if(input.sprintIsPressed && input.moveIsPressed && !playerIsCrouching) {
    //         playerVelocity = 2;
    //     }
    //     else {
    //         playerVelocity = 1;
    //     }
        
        // if (input.sprintIsPressed && input.moveIsPressed && !playerIsCrouching && (timeSprinting < sprintMax)) {
        //     timeResting = 0.0f;
        //     timeSprinting += Time.deltaTime;
        //     calculatedPlayerSprintSpeed *= sprintMultiplier;
        // }
        // else {
        //     timeResting += Time.deltaTime;
        //     if (timeResting > restBuffer) {
        //         if (timeSprinting > 0.0f) {
        //             timeSprinting -= (Time.deltaTime * rechargeRate);
        //         }
        //         else {
        //             timeSprinting = 0.0f;
        //         }
        //     }
        // }
        // Debug.Log("Speed: " + calculatedPlayerSprintSpeed.x);
        //return calculatedPlayerSprintSpeed;
    // }







    







}