using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerStateMachine : MonoBehaviour
{

    [Header("Essentials")]
    public string currentStateText;
    public string currentSubStateText;
    public Transform player;
    public InputManager input;
    public Rigidbody rb;
    public CapsuleCollider capsuleCollider;


    [Header("State Machines")]
    public GunStateMachine gunMachine;
    public CamStateMachine cam;
    public InputPrioritySorter prioritySorter;

    

    [Header("Movement")]

    [SerializeField] bool disableMovement;
    [SerializeField] float playerVelocity;
    float moveMultiplier;
    [SerializeField] float groundMultiplier = 1f;
    [SerializeField] float airMultiplier = .85f;
    [SerializeField] float wallMultiplier = 0.9f;

    [SerializeField] float walkVelocity = 4f;
    [SerializeField] float sprintVelocity = 6f;
    [SerializeField] float crouchVelocity = 2f;
    [SerializeField] float groundDrag = 10f;
    [SerializeField] float airDrag = 0.5f;
    [SerializeField] float wallDrag = 6f;
    float horizontalInput;
    float verticalInput;
    [HideInInspector] public Vector3 moveDirection;

    [Header("Crouching")]
    [SerializeField] float crouchYScale = 0.5f;
    float startYScale;

    [Header("Jumping")]
    [SerializeField] float jumpForce;
    [SerializeField] int coyoteTimer;
    [SerializeField] int coyoteTime;


    [Header("Ground Checking")]
    public bool playerIsGrounded = true;
    [HideInInspector]public RaycastHit groundCheckHit = new RaycastHit();

    [Header("Spring Capsule Settings")]
    [SerializeField] float rideHeight = 0f;
    [SerializeField] float normalHeight = 1.2f;
    [SerializeField] float crouchHeight = 0.73f;
    [SerializeField] float slopeHeight = 1.4f;
    [SerializeField] float crouchSlopeHeight = 0.93f;
    [SerializeField] float rideSpringStrength = 50f; // How hard it pushes up
    [SerializeField] float rideSpringDampener = 5f;  // Prevents infinite bouncing
    [SerializeField] float rayDistance = 2.0f;       // Length of the check ray





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
    [SerializeField] bool wallLeft;
    [SerializeField] bool wallRight;
    [SerializeField] bool wallFront;
    [SerializeField] bool wallBack;


    [Header("Slope Handling")]
    public bool playerIsSloped;
    public int currentAngle;

    public PlayerBaseState currentState;
    public PlayerStateFactory states;
    // For states
    public PlayerBaseState CurrentState { get { return currentState; } set { currentState = value; } }


    // Movement

    public bool DisableMovement { get { return disableMovement; } set { disableMovement = value; } }
    public float PlayerVelocity { get { return playerVelocity; } set { playerVelocity = value; } }
    public float MoveMultiplier { get { return moveMultiplier; } set { moveMultiplier = value; } }
    public float VerticalInput { get { return verticalInput; } set { verticalInput = value; } }
    public float HorizontalInput { get { return horizontalInput; } set { horizontalInput = value; } }

    public float GroundMultiplier { get { return groundMultiplier; } }
    public float AirMultiplier { get { return airMultiplier; } }

    public float WallMultiplier { get { return wallMultiplier; } }
    public float SprintVelocity { get { return sprintVelocity; } }
    public float WalkVelocity { get { return walkVelocity; } }
    public float GroundDrag { get { return groundDrag; } }
    public float AirDrag { get { return airDrag; } }
    public float WallDrag { get { return wallDrag; } }


    // Spring Capsule
    public float NormalHeight { get { return normalHeight; } }
    public float CrouchHeight { get { return crouchHeight; } }
    public float SlopeHeight { get { return slopeHeight; } }
    public float CrouchSlopeHeight { get { return crouchSlopeHeight; } }
    public float RideHeight { get { return rideHeight; } set { rideHeight = value; } }
    public float RideSpringStrength { get { return rideSpringStrength; } set { rideSpringStrength = value; } }
    public float RideSpringDampener { get { return rideSpringDampener; } set { rideSpringDampener = value; } }
    public float RayDistance { get { return rayDistance; } set { rayDistance = value; } }


    // Crouching

    public Vector3 LocalScale { get { return transform.localScale; } set { transform.localScale = value; } }
    public float CrouchVelocity { get { return crouchVelocity; } }
    public float StartYScale { get { return startYScale; } }
    public float CrouchYScale { get { return crouchYScale; } }


    // Jumping State
    public float JumpForce { get { return jumpForce; } }
    public int CoyoteTime { get { return coyoteTime; } set { coyoteTime = value; } }
    public int CoyoteTimer { get { return coyoteTimer; } }




    // Wall Running State
    public LayerMask Wall { get { return wall; } }
    public float WallRunForce { get { return wallRunForce; } }

    public float WallJumpForce { get { return wallJumpForce; } set { wallJumpForce = value; } }
    public float MaxWallJumpForce { get { return maxWallJumpForce; } }
    public bool AbleToWallJump { get { return ableToWallJump; } set { ableToWallJump = value; } }
    public float WallCheckDistance { get { return wallCheckDistance; } }
    public float SphereCastRadius { get { return sphereCastRadius; } }
    public float MinHeight { get { return minHeight; } }
    public bool WallLeft { get { return wallLeft; } set { wallLeft = value; } }
    public bool WallRight { get { return wallRight; } set { wallRight = value; } }
    public bool WallFront { get { return wallFront; } set { wallFront = value; } }
    public bool WallBack { get { return wallBack; } set { wallBack = value; } }

    private void Awake()
    {
        states = new PlayerStateFactory(this);
        currentState = states.Grounded();
        currentState.EnterState();
        currentState.InitializeSubState();
        rideHeight = normalHeight;
        startYScale = transform.localScale.y;

    }



    private void LateUpdate()
    {
        SetRotation();
    }

    private void Update()
    {
        GetMoveInput();
        currentState.UpdateStates();
        currentStateText = currentState.GetType().Name;
        currentSubStateText = currentState.CurrentSubstate().GetType().Name;
        playerIsSloped = PlayerSlopeCheck();
        playerIsGrounded = PlayerGroundCheck();
        if (currentState == states.Walljump() && !ableToWallJump)
        {
            StartCoroutine(WallJumpGracePeriod());
        }
    }

    private void FixedUpdate()
    {
        
        ApplyFloatingForce();
        PlayerMove();
        SpeedControl();
    }



    private void GetMoveInput()
    {
        horizontalInput = input.MoveInput.x;
        verticalInput = input.MoveInput.y;
    }

    private void SetRotation()
    {
        player.transform.forward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z).normalized;
    }

    private void ApplyFloatingForce()
    {
        if ((playerIsGrounded || playerIsSloped) && Physics.Raycast(rb.position, Vector3.down, out groundCheckHit, rayDistance))
        {
            Vector3 vel = rb.linearVelocity;
            Vector3 rayDir = Vector3.down;

            Vector3 otherVel = Vector3.zero;
            Rigidbody hitBody = groundCheckHit.rigidbody;
            if (hitBody != null)
            {
                otherVel = hitBody.linearVelocity;
            }
            float rayDirVel = Vector3.Dot(Vector3.down, vel);
            float otherDirVel = Vector3.Dot(Vector3.down, otherVel);

            float relVel = rayDirVel - otherDirVel;
            float x = groundCheckHit.distance - rideHeight;
            float springForce = (x * rideSpringStrength) - (relVel * rideSpringDampener);
            rb.AddForce(rayDir * springForce);

            if (hitBody != null)
            {
                hitBody.AddForceAtPosition(rayDir * -springForce, groundCheckHit.point);
            }

        }
    }

    private void PlayerMove()
    {
        if (!disableMovement)
        {
            if (currentState != states.Wallrun())
            {
                moveDirection = player.forward * verticalInput + player.right * horizontalInput;
            }
            else
            {
                moveDirection = Camera.main.transform.forward * verticalInput + Camera.main.transform.right * horizontalInput;
            }

            if (PlayerSlopeCheck())
            {
                moveDirection = GetSlopeMoveDirection();

            }

            rb.AddForce(moveDirection.normalized * 20f * playerVelocity * moveMultiplier);
        }
        else
        {
            Debug.Log("Locked");
        }
    }

    public void YFlat()
    {

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    private bool PlayerGroundCheck()
    {
        Debug.DrawRay(rb.position, Vector3.down * rideHeight, Color.red);
        if (Physics.Raycast(rb.position, Vector3.down, out groundCheckHit, rideHeight))
        {
            return true;
        }
        return false;
    }



    private void SpeedControl()
    {

        if (currentState == states.Slope())
        {
            if (rb.linearVelocity.magnitude > playerVelocity)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * playerVelocity;
            }
        }

        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVel.magnitude > playerVelocity)
            {
                Vector3 limitedVel = flatVel.normalized * playerVelocity;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }

    }

    private bool PlayerSlopeCheck()
    {
        if (Physics.Raycast(rb.position, Vector3.down, out groundCheckHit, rideHeight))
        {
            Vector3 localGroundCheckHitNormal = rb.transform.InverseTransformDirection(groundCheckHit.normal);
            currentAngle = (int)Math.Round(Vector3.Angle(localGroundCheckHitNormal, rb.transform.up));
            if (groundCheckHit.collider == null)
            {
                currentAngle = 0;
                return false;
            }

            return Math.Abs(currentAngle) > 0.03f;
        }
        return false;

    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, groundCheckHit.normal).normalized;
    }



    public IEnumerator WallJumpGracePeriod()
    {
        yield return new WaitForSeconds(wallJumpGracePeriod);
        ableToWallJump = true;
    }

    public void LoadData(PlayerSaveData data)
    {
        rb.position = data.playerPos;
        rb.linearVelocity = data.playerVelocity;
    }

    public void SaveData(ref PlayerSaveData data)
    {
        data.playerPos = player.position;
        data.playerVelocity = rb.linearVelocity;
    }
}

[System.Serializable]
public struct PlayerSaveData
{
    public Vector3 playerPos;
    public Vector3 playerVelocity;
}


