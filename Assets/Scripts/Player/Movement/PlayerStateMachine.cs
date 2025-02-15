using System;
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


    [Header("State Machines")]
    public GunStateMachine gunMachine;
    public InputPrioritySorter prioritySorter;

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
    [SerializeField] int coyoteTimer;
    [SerializeField] int coyoteTime;


    // [SerializeField] float jumpCooldown;
    // float cooldownTime;
    

    [Header("Ground Checking")]
    [SerializeField] public bool playerIsGrounded = true;
    [SerializeField] [Range(0.0f, 1.8f)] float groundCheckRadiusMultiplier = 0.9f;
    [SerializeField] [Range(-0.95f, 1.05f)] float groundCheckDistance = 0.01f;
        
    public LayerMask heldRigidbody;
    public RaycastHit groundCheckHit = new RaycastHit();
    

    
    [Header("Wall Running")]
    [SerializeField] LayerMask wall;

    [SerializeField] float wallRunForce;

    [SerializeField] float wallJumpForce;
    [SerializeField] float maxWallJumpForce;
    [SerializeField] float wallJumpGracePeriod;
    [SerializeField] bool ableToWallJump;
    [SerializeField] float wallCheckDistance;
    [SerializeField] float sphereCastRadius; 
    [SerializeField] float minHeight;
    [SerializeField] RaycastHit leftWallHit;
    [SerializeField] RaycastHit rightWallHit;
    [SerializeField] RaycastHit frontWallHit;
    [SerializeField] RaycastHit backWallHit;
    [SerializeField] bool wallLeft;
    [SerializeField] bool wallRight;
    [SerializeField] bool wallFront;
    [SerializeField] bool wallBack;
    

    [Header("Slope Handling")]
    [SerializeField] public bool playerIsSloped;
    [SerializeField] float maxSlopeAngle;
    [SerializeField] bool slopeToSteep;
    [SerializeField] int downForce;

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
    public int CoyoteTime {get { return coyoteTime;} set {coyoteTime = value; } }
    public int CoyoteTimer {get { return coyoteTimer;} }
    



    // Wall Running State
    public LayerMask Wall {get {return wall;} }
    // public bool PlayerIsWallRunning{ get {return playerIsWallRunning;} set {playerIsWallRunning = value; } }
    public float WallRunForce {get {return wallRunForce;} }

    public float WallJumpForce {get {return wallJumpForce;} set {wallJumpForce = value;} }
    public float MaxWallJumpForce {get {return maxWallJumpForce;} }
    // public float MaxWallRunTime{ get {return maxWallRunTime;} }

    public bool AbleToWallJump {get {return ableToWallJump;} set { ableToWallJump = value; } }
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



    public bool SlopeTooSteep {get {return slopeToSteep;}}
    public int DownForce {get{return downForce;}}

    void Start() {
        //rb = GetComponent<Rigidbody>();
        startYScale = transform.localScale.y;
    }
    


    private void Awake(){


        // playerFullHeight = capsuleCollider.height;
        // playerCrouchedHeight = playerFullHeight - crouchAmount;

        states = new PlayerStateFactory(this);
        prioritySorter = GetComponent<InputPrioritySorter>();
        currentState = states.Grounded();
        currentState.EnterState();
        currentState.InitializeSubState();
        
    }

    private void LateUpdate() {
        SetRotation();
    }

    private void Update() {
        currentState.UpdateStates();
        
        playerIsGrounded = PlayerGroundCheck();
        

        GetMoveInput();
        
       
       
        if(currentState == states.Walljump() && !ableToWallJump) {
            StartCoroutine(WallJumpGracePeriod());
        }
    }

    private void FixedUpdate(){
        
        SetOrientation();
        
        PlayerMove();
        playerIsSloped = PlayerSlopeCheck();
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
        if (currentState != states.Wallrun() ) {
            moveDirection = player.forward * verticalInput + player.right * horizontalInput;
        }
        else {
            moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        }

        if (PlayerSlopeCheck()) {
            moveDirection = GetSlopeMoveDirection();
        
        }
        
        rb.AddForce(moveDirection.normalized * 20f * playerVelocity * moveMultiplier);
    }

    private bool PlayerGroundCheck(){
        float sphereCastRadius = capsuleCollider.radius * groundCheckRadiusMultiplier;
        //Physics.SphereCast(playerCenterPoint, sphereCastRadius, Vector3.down, out groundCheckHit);

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

        if (currentState == states.Slope()) {
            if (rb.linearVelocity.magnitude > playerVelocity) {
                rb.linearVelocity = rb.linearVelocity.normalized * playerVelocity;
            }
        }

        else {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > playerVelocity) {
                Vector3 limitedVel = flatVel.normalized * playerVelocity;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
        
    }

    private bool PlayerSlopeCheck() {
        float sphereCastRadius = capsuleCollider.radius * groundCheckRadiusMultiplier;
        float sphereCastTravelDistance = capsuleCollider.bounds.extents.y - sphereCastRadius + groundCheckDistance;
        // Transform playerPos = gameObject.transform.GetChild(1);
        Vector3 localGroundCheckHitNormal = rb.transform.InverseTransformDirection(groundCheckHit.normal); 
        //Physics.SphereCast(rb.position, sphereCastRadius, Vector3.down, out slopeHit, sphereCastTravelDistance)
            int angle = (int) Math.Round(Vector3.Angle(localGroundCheckHitNormal, rb.transform.up));
        
        if (Physics.SphereCast(rb.position, sphereCastRadius, Vector3.down, out slopeHit, sphereCastTravelDistance)) {
            
            if(slopeHit.collider.GetComponent<Pickupable>()) {
                Debug.Log("Shouldn't be any slope");
                return false;
            }

        }
        
            // float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            // Debug.Log("Angle: " + Math.Round(angle) + ", " + (Math.Abs(angle) < maxSlopeAngle && Math.Abs(angle) > 0.03f));
            if(angle > maxSlopeAngle) {
                slopeToSteep = true;
            }
            else {
                slopeToSteep = false;
            }

            
            return Math.Abs(angle) < maxSlopeAngle && Math.Abs(angle) > 0.03f;
            
        // }

        


        /*
             Vector3 localGroundCheckHitNormal = rb.transform.InverseTransformDirection(groundCheckHit.normal); 
        int angle = (int) Math.Round(Vector3.Angle(localGroundCheckHitNormal, rb.transform.up));
        if(angle == 0) {
            if(input.moveIsPressed) {
                RaycastHit rayHit;
                float rayHeightFromGround = 0.1f;
                float rayCalculatedRayHeight = rb.position.y - capsuleCollider.bounds.extents.y + rayHeightFromGround;
                Vector3 rayOrigin = new Vector3(rb.transform.position.x, rayCalculatedRayHeight, rb.position.z);

                if(Physics.Raycast(rayOrigin, rb.transform.TransformDirection(moveDirection), out rayHit, 0.75f)){
                    if(Vector3.Angle(rayHit.normal, rb.transform.up) > maxSlopeAngle) { 
                        moveDirection.y = -moveMultiplier;
                    }
                }
                Debug.DrawRay(rayOrigin, rb.transform.TransformDirection(moveDirection));
            }
            return false;
        }

        else if(angle < maxSlopeAngle){ 
            return true;
        }

        */
    }

    private Vector3 GetSlopeMoveDirection() {
        return Vector3.ProjectOnPlane(moveDirection, groundCheckHit.normal).normalized;
    }


    public IEnumerator WallJumpGracePeriod() {
        yield return new WaitForSeconds(wallJumpGracePeriod);
        ableToWallJump = true;
    }



    //private bool CheckSlope() { 
    //    if(playerIsGrounded) {
    //        float angle = Vector3.Angle(Vector3.up, groundCheckHit.normal);
    //        playerIsSlopeStable = angle < maxSlopeAngle;
    //        return playerIsSlopeStable && angle > 1;
    //    }
    //    return false;
    //}

    //private Vector3 SlopeHandle()
    //{
    //    return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    //}



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