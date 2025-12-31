using UnityEngine;

public class PlayerSlopeState : PlayerBaseState
{

    public PlayerSlopeState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
    : base(currentContext, playerStateFactory)
    {
        IsRootState = true;

    }

    public override void EnterState()
    {
        // Debug.Log("SLOPE STATE");
        InitializeSubState();

        // Ctx.YFlat();
        Ctx.RideHeight = 1.4f;
        Ctx.MoveMultiplier = Ctx.GroundMultiplier;
        Ctx.rb.linearDamping = Ctx.GroundDrag;

    }

    public override void UpdateState()
    {
        SlideDownSlope();
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Ctx.RideHeight = Ctx.normalHeight;
    }

    public override void CheckSwitchStates()
    {
        if (Ctx.input.jumpIsPressed && !Ctx.SlopeTooSteep)
        {
            SwitchState(Factory.Jump());
        }
        else if (!Ctx.playerIsSloped && !Ctx.playerIsGrounded)
        {
            SwitchState(Factory.Fall());
        }
        else if (!Ctx.input.jumpIsPressed && !Ctx.playerIsSloped)
        {
            SwitchState(Factory.Grounded());
        }
    }


    private void SlideDownSlope()
    {
        

    }

    public override void InitializeSubState()
    {
        if (!Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed)
        {

            SetSubState(Factory.Idle());
        }
        else if (Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed)
        {

            SetSubState(Factory.Walk());
        }
        else
        {
            SetSubState(Factory.Sprint());
        }
    }
}
