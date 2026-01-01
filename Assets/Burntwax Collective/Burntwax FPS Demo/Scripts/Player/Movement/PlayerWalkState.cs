using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkState : PlayerBaseState
{

    public PlayerWalkState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
     : base(currentContext, playerStateFactory)
    {

    }

    public override void EnterState()
    {
        Ctx.PlayerVelocity = Ctx.WalkVelocity;
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }

    public override void ExitState() { }

    public override void CheckSwitchStates()
    {

        if (Ctx.input.moveIsPressed && Ctx.prioritySorter.SprintIsPriority() && !Ctx.gunMachine.isShooting && !Ctx.gunMachine.isReloading && !Ctx.gunMachine.isIncrementalReloading)
        {
            SwitchState(Factory.Sprint());
        }
        else if (!Ctx.input.moveIsPressed)
        {
            SwitchState(Factory.Idle());
        }
        else if (Ctx.input.crouchIsPressed && Ctx.currentState != Ctx.states.Fall() && Ctx.currentState != Ctx.states.Jump())
        {
            SwitchState(Factory.Crouch());
        }


    }

    public override void InitializeSubState() { }
}
