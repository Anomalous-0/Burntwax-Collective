using UnityEngine;

namespace Burntwax
{
    public class PlayerFallState : PlayerBaseState
    {

        private RaycastHit frontWallHit;
        private RaycastHit backWallHit;
        private RaycastHit leftWallHit;
        private RaycastHit rightWallHit;

        public PlayerFallState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
        {
            IsRootState = true;

        }


        public override void EnterState()
        {
            Ctx.MoveMultiplier = Ctx.AirMultiplier;
            Ctx.rb.linearDamping = Ctx.AirDrag;

            InitializeSubState();
            Ctx.currentAngle = 0;
        }

        public override void UpdateState()
        {

            Ctx.CoyoteTime--;
            if (Ctx.CoyoteTime <= 0)
            {
                Ctx.CoyoteTime = 0;
            }
            WallCheck();
            CheckSwitchStates();

        }

        public override void ExitState()
        {
            Ctx.CoyoteTime = 0;
        }

        public override void CheckSwitchStates()
        {
            if (Ctx.playerIsSloped)
            {
                // Debug.Log("FALL TO SLOPE");
                SwitchState(Factory.Slope());
                return;
            }
            else if (Ctx.playerIsGrounded && !Ctx.playerIsSloped)
            {
                // Debug.Log("FALL TO GROUNDED");
                SwitchState(Factory.Grounded());
            }
            else if (Ctx.WallLeft || Ctx.WallRight || Ctx.WallFront || Ctx.WallBack)
            {
                SwitchState(Factory.Wallrun());
            }
            else if (Ctx.CoyoteTime > 0 && InputManager.Instance.jumpIsPressed)
            {
                Ctx.CoyoteTime = Ctx.CoyoteTimer;
                Ctx.YFlat();
                SwitchState(Factory.Jump());
            }

        }

        public override void InitializeSubState()
        {
            if (!InputManager.Instance.moveIsPressed && !InputManager.Instance.sprintIsPressed)
            {
                SetSubState(Factory.Idle());
            }
            else if (InputManager.Instance.moveIsPressed && !InputManager.Instance.sprintIsPressed)
            {
                SetSubState(Factory.Walk());
            }
            else
            {
                SetSubState(Factory.Sprint());
            }
        }


        private void WallCheck()
        {
            Ctx.WallRight = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.right, out rightWallHit, Ctx.WallCheckDistance, Ctx.Wall);
            Ctx.WallLeft = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.right, out leftWallHit, Ctx.WallCheckDistance, Ctx.Wall);
            Ctx.WallFront = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.forward, out frontWallHit, Ctx.WallCheckDistance, Ctx.Wall);
            Ctx.WallBack = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.forward, out backWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        }


    }

}