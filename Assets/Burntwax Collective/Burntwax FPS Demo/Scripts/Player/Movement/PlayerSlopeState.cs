using System;
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
        Ctx.MoveMultiplier = Ctx.GroundMultiplier;
        Ctx.rb.linearDamping = Ctx.GroundDrag;    

        Ctx.YFlat();
        InitializeSubState();
        if(Ctx.currentState.CurrentSubstate() == Ctx.states.Crouch())
        {
            Ctx.RideHeight = Ctx.crouchSlopeHeight;
            return;
        }
        Ctx.RideHeight = Ctx.slopeHeight;
        

        
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Ctx.currentAngle = 0;
        if(Ctx.currentState.CurrentSubstate() == Ctx.states.Crouch())
        {
            Ctx.RideHeight = Ctx.crouchHeight;
            return;
        }
        Ctx.RideHeight = Ctx.normalHeight;
    }

    public override void CheckSwitchStates()
    {
        if (Ctx.input.jumpIsPressed)
        {
            SwitchState(Factory.Jump());
        }
        else if (!Ctx.playerIsSloped && !Ctx.playerIsGrounded && !Ctx.input.crouchIsPressed)
        {
            SwitchState(Factory.Fall());
        }
        else if (!Ctx.input.jumpIsPressed && !Ctx.playerIsSloped && Math.Abs(Ctx.currentAngle) < 0.03f && Ctx.groundCheckHit.collider.gameObject.layer != LayerMask.NameToLayer("Variable Slope"))
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
        else if (Ctx.input.crouchIsPressed)
        {
            SwitchState(Factory.Crouch());
        }
        else
        {
            SetSubState(Factory.Sprint());
        }
        
    }
}
