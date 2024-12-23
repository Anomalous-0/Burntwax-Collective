using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkState : PlayerBaseState {
    
    public PlayerWalkState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
     : base (currentContext, playerStateFactory){

    }
    
    public override void EnterState(){
        Ctx.PlayerVelocity = Ctx.WalkVelocity;
        if(Ctx.input.sprintIsPressed && Ctx.input.aimIsPressed) {
            Ctx.input.sprintIsPressed = false;
        }
        //Debug.Log("WALK STATE");
    }

    public override void UpdateState(){
        SwitchCam();
        CheckSwitchStates();
    }

    public override void ExitState(){}
    
    public override void CheckSwitchStates(){
        if(Ctx.input.moveIsPressed && Ctx.input.sprintIsPressed && !Ctx.gunMachine.isReloading && !Ctx.input.shootIsPressed) {
            SwitchState(Factory.Sprint());
        } else if(!Ctx.input.moveIsPressed) {
            SwitchState(Factory.Idle());
        }
        else if(Ctx.input.crouchIsPressed) {
            SwitchState(Factory.Crouch());
        }
    }

    public override void InitializeSubState(){}

    private void SwitchCam() {
        // if(Ctx.input.aimIsPressed) {
        //     Ctx.cam.Aim();
        // }   
        // else {
        //     Ctx.cam.FPS();
        // }
    }
}
