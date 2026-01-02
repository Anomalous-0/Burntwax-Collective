using UnityEngine;

public class PlayerJumpState : PlayerBaseState
{

    private RaycastHit frontWallHit;
    private RaycastHit backWallHit;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    public PlayerJumpState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
    : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        Ctx.MoveMultiplier = Ctx.AirMultiplier;
        Ctx.rb.linearDamping = Ctx.AirDrag;
        Jump();
        InitializeSubState();
        Ctx.currentAngle = 0;
    }

    public override void UpdateState()
    {
        WallCheck();
        CheckSwitchStates();
    }

    public override void ExitState()
    {

    }

    public override void CheckSwitchStates()
    {
        if (Ctx.playerIsSloped && !Ctx.playerIsGrounded)
        {
            SwitchState(Factory.Slope());
            return;
        }
        else if (Ctx.playerIsGrounded )
        {
            SwitchState(Factory.Grounded());
        }
        else if (Ctx.rb.linearVelocity.y < 0)
        {
            SwitchState(Factory.Fall());
        }
        else if (Ctx.WallLeft || Ctx.WallRight || Ctx.WallFront || Ctx.WallBack)
        {
            SwitchState(Factory.Wallrun());
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

    private void Jump()
    {
        Ctx.rb.AddForce(Ctx.player.up * Ctx.JumpForce, ForceMode.Impulse);
        Ctx.YFlat();
    }

    private void WallCheck()
    {
        Ctx.WallRight = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.right, out rightWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallLeft = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.right, out leftWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallFront = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.forward, out frontWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallBack = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.forward, out backWallHit, Ctx.WallCheckDistance, Ctx.Wall);
    }

}