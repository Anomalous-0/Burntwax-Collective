using System;
using System.Collections;
using UnityEngine;

namespace Burntwax
{
    public class PlayerStateMachine : MonoBehaviour
    {

        [Header("Essentials")]
        public string currentStateText;
        public string currentSubStateText;
        public Transform player;
        public Rigidbody rb;
        public CapsuleCollider capsuleCollider;


        [Header("State Machines")]
        public GunStateMachine gunMachine;
        [HideInInspector] public KatanaStateMachine katanaMachine;

        [Header("Dash Collision")]
        [SerializeField] LayerMask dashBlockingLayers;
        [SerializeField, Min(0f)] float dashCollisionSkin = 0.05f;

        [Header("Mantle")]
        [SerializeField] PlayerMantleConfig mantleConfig;
        [SerializeField] Animator mantleAnimator;
        [SerializeField] string mantleBoolParameter = "mantle";
        [SerializeField] string mantleProgressParameter = "mantleProgress";

        private int mantleBoolHash;
        private int mantleProgressHash;
        private bool hasMantleBool;
        private bool hasMantleProgress;



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

        [Header("Falling")]
        [Tooltip("Multiplier applied to normal Physics gravity while in the Fall state. One uses normal gravity.")]
        [SerializeField, Min(1f)] float fallGravityMultiplier = 2f;

        [Tooltip("Maximum downward speed while falling. Set to 0 for no explicit limit.")]
        [SerializeField, Min(0f)] float maximumFallSpeed = 35f;

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
        [HideInInspector] public RaycastHit groundCheckHit = new RaycastHit();

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

        // External runtime speed scale (1 = normal). The glove charge-blast state lerps this down
        // so the player moves slower the more the blast is charged; it's reset to 1 on release.
        public float ChargeSpeedMultiplier { get; set; } = 1f;


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
        public KatanaStateMachine KatanaMachine { get { return katanaMachine; } }
        public bool CanSprint
        {
            get
            {
                return katanaMachine == null
                    || katanaMachine.arsenal == null
                    || katanaMachine.arsenal.currentMode != Arsenal.WeaponMode.Katana;
            }
        }
        public float DashCollisionSkin { get { return dashCollisionSkin; } }
        public PlayerMantleConfig MantleConfig { get { return mantleConfig; } }
        public bool GameplayActionsLocked { get; private set; }


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
            if (gunMachine != null)
                katanaMachine = gunMachine.GetComponent<KatanaStateMachine>();

            if (mantleAnimator == null && gunMachine != null)
                mantleAnimator = gunMachine.anim;

            CacheMantleAnimationParameters();

            states = new PlayerStateFactory(this);
            currentState = states.Grounded();
            currentState.EnterState();
            currentState.InitializeSubState();
            rideHeight = normalHeight;
            startYScale = transform.localScale.y;

        }

        public void SetMantleActive(bool active)
        {
            GameplayActionsLocked = active;

            Arsenal arsenal = gunMachine != null
                ? gunMachine.GetComponent<Arsenal>()
                : null;
            if (arsenal != null)
                arsenal.SetGameplaySuppressed(active);

            if (mantleAnimator != null && hasMantleBool)
                mantleAnimator.SetBool(mantleBoolHash, active);

            if (!active && mantleAnimator != null && hasMantleProgress)
                mantleAnimator.SetFloat(mantleProgressHash, 0f);
        }

        public void SetMantleAnimationProgress(float progress)
        {
            if (mantleAnimator != null && hasMantleProgress)
                mantleAnimator.SetFloat(mantleProgressHash, Mathf.Clamp01(progress));
        }

        private void CacheMantleAnimationParameters()
        {
            mantleBoolHash = Animator.StringToHash(mantleBoolParameter);
            mantleProgressHash = Animator.StringToHash(mantleProgressParameter);
            hasMantleBool = false;
            hasMantleProgress = false;

            if (mantleAnimator == null)
                return;

            foreach (AnimatorControllerParameter parameter in mantleAnimator.parameters)
            {
                if (parameter.nameHash == mantleBoolHash
                    && parameter.type == AnimatorControllerParameterType.Bool)
                    hasMantleBool = true;

                if (parameter.nameHash == mantleProgressHash
                    && parameter.type == AnimatorControllerParameterType.Float)
                    hasMantleProgress = true;
            }
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
            currentSubStateText = currentState.CurrentSubstate() != null
                ? currentState.CurrentSubstate().GetType().Name
                : "None";
            playerIsSloped = PlayerSlopeCheck();
            playerIsGrounded = PlayerGroundCheck();

            if (mantleConfig != null && mantleConfig.DrawDebugChecks
                && (currentState == states.Jump() || currentState == states.Fall()))
                TryGetMantleTarget(out _);
            // if (currentState == states.Walljump() && !ableToWallJump)
            // {
            //     StartCoroutine(WallJumpGracePeriod());
            // }
        }

        private void FixedUpdate()
        {
            if (currentState == states.Mantle())
                states.Mantle().FixedUpdateState();
            else if (currentState.CurrentSubstate() == states.Dash())
                ((PlayerDashState)states.Dash()).FixedUpdateState();

            ApplyFallGravity();
            ApplyFloatingForce();
            PlayerMove();
            SpeedControl();
        }

        private void ApplyFallGravity()
        {
            if (rb == null || currentState != states.Fall())
                return;

            float additionalGravityMultiplier = fallGravityMultiplier - 1f;
            if (additionalGravityMultiplier > 0f)
            {
                rb.AddForce(
                    Physics.gravity * additionalGravityMultiplier,
                    ForceMode.Acceleration);
            }

            if (maximumFallSpeed <= 0f
                || rb.linearVelocity.y >= -maximumFallSpeed)
                return;

            Vector3 velocity = rb.linearVelocity;
            velocity.y = -maximumFallSpeed;
            rb.linearVelocity = velocity;
        }



        private void GetMoveInput()
        {
            horizontalInput = InputManager.Instance.MoveInput.x;
            verticalInput = InputManager.Instance.MoveInput.y;
        }

        private void SetRotation()
        {
            player.transform.forward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z).normalized;
        }

        private void ApplyFloatingForce()
        {
            if (currentState == states.Mantle()
                || currentState.CurrentSubstate() == states.Dash())
                return;

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
                moveDirection = player.forward * verticalInput
                    + player.right * horizontalInput;

                if (PlayerSlopeCheck())
                {
                    moveDirection = GetSlopeMoveDirection();

                }

                rb.AddForce(moveDirection.normalized * 20f * playerVelocity * moveMultiplier * ChargeSpeedMultiplier);
            }
        }

        public void YFlat()
        {

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }

        public bool TryGetDashBlock(
            Vector3 direction,
            float distance,
            out RaycastHit hit)
        {
            hit = default;

            if (capsuleCollider == null || direction.sqrMagnitude <= 0f
                || distance <= 0f || dashBlockingLayers.value == 0)
                return false;

            if (!TryGetWorldCapsule(
                    out Vector3 pointOne,
                    out Vector3 pointTwo,
                    out float radius))
                return false;

            return Physics.CapsuleCast(
                pointOne,
                pointTwo,
                radius,
                direction.normalized,
                out hit,
                distance,
                dashBlockingLayers,
                QueryTriggerInteraction.Ignore);
        }

        public bool TryGetMantleTarget(out PlayerMantleTarget target)
        {
            target = default;

            if (mantleConfig == null || player == null
                || !TryGetWorldCapsule(
                    out Vector3 pointOne,
                    out Vector3 pointTwo,
                    out float capsuleRadius))
                return false;

            Bounds bounds = capsuleCollider.bounds;
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
                return false;

            Vector3 wallOrigin = new Vector3(
                bounds.center.x,
                Mathf.Lerp(
                    bounds.min.y,
                    bounds.max.y,
                    mantleConfig.ChestHeightNormalized),
                bounds.center.z);
            float wallProbeRadius = capsuleRadius
                * mantleConfig.WallProbeRadiusScale;

            bool foundWall = Physics.SphereCast(
                wallOrigin,
                wallProbeRadius,
                forward,
                out RaycastHit wallHit,
                mantleConfig.ForwardCheckDistance,
                mantleConfig.WallLayers,
                QueryTriggerInteraction.Ignore);

            if (mantleConfig.DrawDebugChecks)
            {
                Debug.DrawLine(
                    wallOrigin,
                    wallOrigin + forward * mantleConfig.ForwardCheckDistance,
                    foundWall ? Color.green : Color.red,
                    0.1f);
            }

            if (!foundWall)
                return false;

            Vector3 intoWall = Vector3.ProjectOnPlane(
                -wallHit.normal,
                Vector3.up).normalized;
            if (intoWall.sqrMagnitude <= Mathf.Epsilon)
                return false;

            float minimumHeight = Mathf.Min(
                mantleConfig.MinimumMantleHeight,
                mantleConfig.MaximumMantleHeight);
            float maximumHeight = Mathf.Max(
                mantleConfig.MinimumMantleHeight,
                mantleConfig.MaximumMantleHeight);
            Vector3 topProbeOrigin = wallHit.point
                + intoWall * mantleConfig.LandingDepth;
            topProbeOrigin.y = bounds.min.y
                + maximumHeight;
            float topProbeDistance = maximumHeight - minimumHeight;

            bool foundTop = Physics.Raycast(
                topProbeOrigin,
                Vector3.down,
                out RaycastHit topHit,
                topProbeDistance,
                mantleConfig.TopSurfaceLayers,
                QueryTriggerInteraction.Ignore);

            if (mantleConfig.DrawDebugChecks)
            {
                Debug.DrawLine(
                    topProbeOrigin,
                    topProbeOrigin + Vector3.down * topProbeDistance,
                    foundTop ? Color.cyan : Color.yellow,
                    0.1f);
            }

            if (!foundTop
                || Vector3.Dot(topHit.normal, Vector3.up)
                    < mantleConfig.MinimumTopUpDot)
                return false;

            float mantleHeight = topHit.point.y - bounds.min.y;
            if (mantleHeight < minimumHeight
                || mantleHeight > maximumHeight)
                return false;

            Vector3 targetCapsuleCenter = topHit.point
                + Vector3.up * (bounds.extents.y + mantleConfig.ClearanceSkin);
            Vector3 capsuleOffset = targetCapsuleCenter - bounds.center;
            float clearanceRadius = Mathf.Max(
                0.01f,
                capsuleRadius - mantleConfig.ClearanceSkin);
            bool landingBlocked = Physics.CheckCapsule(
                pointOne + capsuleOffset,
                pointTwo + capsuleOffset,
                clearanceRadius,
                mantleConfig.ClearanceBlockingLayers,
                QueryTriggerInteraction.Ignore);

            if (landingBlocked)
                return false;

            target = new PlayerMantleTarget(
                rb.position + capsuleOffset,
                wallHit,
                topHit);
            return true;
        }

        private bool TryGetWorldCapsule(
            out Vector3 pointOne,
            out Vector3 pointTwo,
            out float radius)
        {
            pointOne = default;
            pointTwo = default;
            radius = 0f;

            if (capsuleCollider == null)
                return false;

            Transform capsuleTransform = capsuleCollider.transform;
            Vector3 scale = capsuleTransform.lossyScale;
            Vector3 absoluteScale = new Vector3(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z));

            Vector3 localAxis;
            float axisScale;
            float radiusScale;

            switch (capsuleCollider.direction)
            {
                case 0:
                    localAxis = Vector3.right;
                    axisScale = absoluteScale.x;
                    radiusScale = Mathf.Max(absoluteScale.y, absoluteScale.z);
                    break;
                case 2:
                    localAxis = Vector3.forward;
                    axisScale = absoluteScale.z;
                    radiusScale = Mathf.Max(absoluteScale.x, absoluteScale.y);
                    break;
                default:
                    localAxis = Vector3.up;
                    axisScale = absoluteScale.y;
                    radiusScale = Mathf.Max(absoluteScale.x, absoluteScale.z);
                    break;
            }

            Vector3 axis = capsuleTransform.TransformDirection(localAxis).normalized;
            Vector3 center = capsuleTransform.TransformPoint(capsuleCollider.center);
            radius = capsuleCollider.radius * radiusScale;
            float height = Mathf.Max(capsuleCollider.height * axisScale, radius * 2f);
            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            pointOne = center + axis * halfSegment;
            pointTwo = center - axis * halfSegment;
            return true;
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
            if (currentState == states.Mantle()
                || currentState.CurrentSubstate() == states.Dash())
                return;

            float speedCap = playerVelocity * ChargeSpeedMultiplier;

            if (currentState == states.Slope())
            {
                if (rb.linearVelocity.magnitude > speedCap)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * speedCap;
                }
            }

            else
            {
                Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                if (flatVel.magnitude > speedCap)
                {
                    Vector3 limitedVel = flatVel.normalized * speedCap;
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


}
