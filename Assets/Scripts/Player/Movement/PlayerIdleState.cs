using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{

    public PlayerIdleState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {

    }

    public override void EnterState()
    {
        Ctx.moveDirection.x = 0;
        Ctx.moveDirection.y = 0;
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }

    public override void ExitState() { }

    public override void CheckSwitchStates()
    {
        if (Ctx.input.moveIsPressed && Ctx.input.sprintIsPressed && Ctx.gunMachine.CurrentState.CurrentSubstate() != Ctx.gunMachine.states.Shoot() && Ctx.gunMachine.CurrentState.CurrentSubstate() != Ctx.gunMachine.states.Shoot())
        {
            SwitchState(Factory.Sprint());
        }
        else if (Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed)
        {
            SwitchState(Factory.Walk());
        }
        else if (Ctx.input.crouchIsPressed)
        {
            SwitchState(Factory.Crouch());
        }
    }

    public override void InitializeSubState() { }
}
