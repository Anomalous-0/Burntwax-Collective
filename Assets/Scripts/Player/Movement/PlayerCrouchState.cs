using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCrouchState : PlayerBaseState {
    
    public PlayerCrouchState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base (currentContext, playerStateFactory){

    }

    public override void EnterState(){
        Ctx.PlayerVelocity = Ctx.CrouchVelocity;

        Ctx.LocalScale = new Vector3(Ctx.LocalScale.x, Ctx.CrouchYScale, Ctx.LocalScale.z);
        Ctx.rb.AddForce(Vector3.down * 15f, ForceMode.Impulse);
    }

    public override void UpdateState(){
        CheckSwitchStates();
    }

    public override void ExitState(){
        Ctx.LocalScale = new Vector3(Ctx.LocalScale.x, Ctx.StartYScale, Ctx.LocalScale.z);
    }
    
    public override void CheckSwitchStates(){
        
        RaycastHit hit;
        if (Physics.Raycast(Ctx.transform.position, Ctx.transform.TransformDirection(Vector3.forward), out hit, 1f)) {
            // This is to filter everything out before the other checks
        }
        else if(!Ctx.input.crouchIsPressed && Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed) {
            SwitchState(Factory.Walk());
        }
        else if(!Ctx.input.crouchIsPressed && !Ctx.input.moveIsPressed) {
            SwitchState(Factory.Idle());
        }
        else if(!Ctx.input.crouchIsPressed && Ctx.input.moveIsPressed && Ctx.input.sprintIsPressed) {
            SwitchState(Factory.Sprint());    
        }
    }

    public override void InitializeSubState(){}




}



