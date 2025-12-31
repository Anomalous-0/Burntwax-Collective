using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;

public class PlayerCrouchState : PlayerBaseState
{

    public PlayerCrouchState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {

    }

    public override void EnterState()
    {
        Ctx.PlayerVelocity = Ctx.CrouchVelocity;

        Ctx.LocalScale = new Vector3(Ctx.LocalScale.x, Ctx.CrouchYScale, Ctx.LocalScale.z);
        Ctx.rb.AddForce(Vector3.down * 8f, ForceMode.Impulse);
        Ctx.RideHeight = Ctx.crouchHeight;

    }

    public override void UpdateState()
    {
        CheckSwitchStates();
        
    }

    public override void ExitState()
    {
        Ctx.LocalScale = new Vector3(Ctx.LocalScale.x, Ctx.StartYScale, Ctx.LocalScale.z);
        Ctx.RideHeight = Ctx.normalHeight;
    }

    public override void CheckSwitchStates()
    {
        if (Physics.Raycast(Ctx.transform.position, Ctx.transform.TransformDirection(Vector3.up), out RaycastHit hit, 1f))
        {
            return;
        }
        else if (!Ctx.input.crouchIsPressed && Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed)
        {
            SwitchState(Factory.Walk());
        }
        else if (!Ctx.input.crouchIsPressed && !Ctx.input.moveIsPressed)
        {
            SwitchState(Factory.Idle());
        }
        else if (!Ctx.input.crouchIsPressed && Ctx.input.moveIsPressed && Ctx.input.sprintIsPressed)
        {
            SwitchState(Factory.Sprint());
        }
    }

    public override void InitializeSubState() { }




}



