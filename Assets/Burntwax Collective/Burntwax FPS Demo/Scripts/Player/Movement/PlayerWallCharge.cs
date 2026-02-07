using UnityEngine;
namespace Burntwax
{
    public class PlayerWallChargeState : PlayerBaseState
    {
        private RaycastHit frontWallHit;
        private RaycastHit backWallHit;
        private RaycastHit leftWallHit;
        private RaycastHit rightWallHit;
        Vector3 wallNormal;

        public PlayerWallChargeState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            InitializeSubState();
            Ctx.MoveMultiplier = Ctx.CrouchVelocity;
        }

        public override void UpdateState()
        {

            CheckSwitchStates();
            WallCheck();
            WallRun();

        }

        public override void ExitState()
        {

        }

        public override void CheckSwitchStates()
        {
            if (Ctx.playerIsGrounded)
            {
                Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0f, Ctx.rb.linearVelocity.z);
                Ctx.rb.useGravity = true;
                SwitchState(Factory.Grounded());
            }
            else if (!InputManager.Instance.jumpIsPressed)
            {
                Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0f, Ctx.rb.linearVelocity.z);
                Ctx.rb.useGravity = true;
                Vector3 force = Ctx.moveDirection * Ctx.WallJumpForce;
                Ctx.rb.AddForce(force, ForceMode.Impulse);
                Ctx.rb.AddForce(3f * wallNormal.normalized, ForceMode.Impulse);
                SwitchState(Factory.Walljump());


            }
            else if (!Ctx.WallLeft && !Ctx.WallRight && !Ctx.WallFront && !Ctx.WallBack)
            {
                Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0f, Ctx.rb.linearVelocity.z);
                Ctx.rb.useGravity = true;
                SwitchState(Factory.Fall());
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

        private void WallRun()
        {
            if (Ctx.WallRight)
            {
                wallNormal = rightWallHit.normal;
            }
            else if (Ctx.WallLeft)
            {
                wallNormal = leftWallHit.normal;
            }
            else if (Ctx.WallFront)
            {
                wallNormal = frontWallHit.normal;
            }
            else if (Ctx.WallBack)
            {
                wallNormal = backWallHit.normal;
            }
            Ctx.rb.AddForce(-wallNormal * 200);


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