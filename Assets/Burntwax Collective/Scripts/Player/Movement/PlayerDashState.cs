using UnityEngine;

namespace Burntwax
{
    public class PlayerDashState : PlayerBaseState
    {
        private float dashEndTime;
        private Vector3 dashVelocity;
        private bool collisionTerminated;

        public PlayerDashState(
            PlayerStateMachine currentContext,
            PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory)
        {
        }

        public override void EnterState()
        {
            KatanaStateMachine katana = Ctx.KatanaMachine;
            KatanaScriptableObject dashConfig = katana != null
                ? katana.ActiveKatanaScriptable
                : null;

            if (dashConfig == null || !katana.TryConsumeDashCharge())
            {
                dashEndTime = Time.time;
                return;
            }

            Vector2 moveInput = InputManager.Instance.MoveInput;
            Vector3 dashDirection;

            if (Camera.main != null)
            {
                Transform cameraTransform = Camera.main.transform;
                dashDirection = moveInput.sqrMagnitude > 0f
                    ? cameraTransform.forward * moveInput.y
                        + cameraTransform.right * moveInput.x
                    : cameraTransform.forward;
            }
            else
            {
                dashDirection = moveInput.sqrMagnitude > 0f
                    ? Ctx.player.forward * moveInput.y
                        + Ctx.player.right * moveInput.x
                    : Ctx.player.forward;
            }

            dashDirection.y = 0f;

            if (dashDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                dashDirection = Ctx.player.forward;
                dashDirection.y = 0f;
            }

            dashVelocity = dashDirection.normalized * dashConfig.DashSpeed;
            collisionTerminated = false;
            Ctx.DisableMovement = true;
            Ctx.rb.useGravity = false;
            Ctx.rb.linearDamping = 0f;
            Ctx.rb.linearVelocity = dashVelocity;
            dashEndTime = Time.time + dashConfig.DashDuration;
        }

        public override void UpdateState()
        {
            if (Ctx.KatanaMachine == null || !Ctx.KatanaMachine.IsDashEquipped)
            {
                dashEndTime = Time.time;
                CheckSwitchStates();
                return;
            }

            if (collisionTerminated)
            {
                CheckSwitchStates();
                return;
            }

            Ctx.rb.useGravity = false;
            Ctx.rb.linearDamping = 0f;
            Ctx.rb.linearVelocity = dashVelocity;
            CheckSwitchStates();
        }

        public void FixedUpdateState()
        {
            if (collisionTerminated || dashVelocity.sqrMagnitude <= 0f)
                return;

            float stepDistance = dashVelocity.magnitude * Time.fixedDeltaTime;
            if (!Ctx.TryGetDashBlock(
                    dashVelocity,
                    stepDistance + Ctx.DashCollisionSkin,
                    out RaycastHit hit))
                return;

            float safeDistance = Mathf.Max(
                0f,
                hit.distance - Ctx.DashCollisionSkin);
            Ctx.rb.linearVelocity = dashVelocity.normalized
                * (safeDistance / Time.fixedDeltaTime);
            dashEndTime = Time.time;
            collisionTerminated = true;
        }

        public override void ExitState()
        {
            Ctx.DisableMovement = false;
            Ctx.rb.useGravity = true;
            Ctx.rb.linearVelocity = Vector3.zero;
            Ctx.rb.linearDamping = Ctx.currentState == Ctx.states.Grounded()
                || Ctx.currentState == Ctx.states.Slope()
                ? Ctx.GroundDrag
                : Ctx.AirDrag;
        }

        public override void CheckSwitchStates()
        {
            if (Time.time < dashEndTime)
                return;

            if (InputManager.Instance.crouchIsPressed
                && (Ctx.currentState == Ctx.states.Grounded()
                    || Ctx.currentState == Ctx.states.Slope()))
                SwitchState(Factory.Crouch());
            else if (!InputManager.Instance.moveIsPressed)
                SwitchState(Factory.Idle());
            else if (InputManager.Instance.sprintIsPressed && Ctx.CanSprint)
                SwitchState(Factory.Sprint());
            else
                SwitchState(Factory.Walk());
        }

        public override void InitializeSubState()
        {
        }
    }
}
