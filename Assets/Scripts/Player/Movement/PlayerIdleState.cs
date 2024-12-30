using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerBaseState {
   
    public PlayerIdleState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory): base (currentContext, playerStateFactory) {

    }

    public override void EnterState(){
        
        Ctx.moveDirection.x = 0;
        Ctx.moveDirection.y = 0;
       
        //Debug.Log("IDLE STATE");
    }

    public override void UpdateState(){

        SwitchCam();
        CheckSwitchStates();
    }

    public override void ExitState(){}
    
    public override void CheckSwitchStates(){
        if(Ctx.input.moveIsPressed && Ctx.input.sprintIsPressed) {
            SwitchState(Factory.Sprint());
        } else if(Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed) {
            SwitchState(Factory.Walk());
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
