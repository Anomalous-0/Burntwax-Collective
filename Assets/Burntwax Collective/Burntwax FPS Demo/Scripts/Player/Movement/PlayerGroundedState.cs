using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerGroundedState : PlayerBaseState
{

    public PlayerGroundedState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
    : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        Ctx.MoveMultiplier = Ctx.GroundMultiplier;
        Ctx.rb.linearDamping = Ctx.GroundDrag;
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }

    public override void ExitState()
    {

    }

    public override void CheckSwitchStates()
    {
        if (Ctx.input.jumpIsPressed)
        {
            SwitchState(Factory.Jump());
        }
        else if (!Ctx.playerIsGrounded && !Ctx.input.crouchIsPressed)
        {
            Ctx.CoyoteTime = Ctx.CoyoteTimer;
            SwitchState(Factory.Fall());
        }
        else if (Ctx.playerIsSloped)
        {
            SwitchState(Factory.Slope());
        }
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
