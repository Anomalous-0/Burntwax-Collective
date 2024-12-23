using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSprintState : PlayerBaseState {
    

    public PlayerSprintState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base (currentContext, playerStateFactory){

    }

    public override void EnterState(){
        Ctx.PlayerVelocity = Ctx.SprintVelocity;
        // Ctx.cam.FPS();
        if(Ctx.input.aimIsPressed) {
            Ctx.input.aimIsPressed = false;
        }
        // Debug.Log("SPRINT STATE");
    }

    public override void UpdateState(){

        CheckSwitchStates();
    }

    public override void ExitState(){
        
    }
    
    public override void CheckSwitchStates(){
        // if(Ctx.input.aimIsPressed) {
        //     if(Ctx.input.moveIsPressed) {
        //         Ctx.cam.Aim();
        //         SwitchState(Factory.Walk());
        //     }
        // }
        if((Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed) || Ctx.input.shootIsPressed){ 
            
            SwitchState(Factory.Walk());
        }
        else if(Ctx.input.moveIsPressed && Ctx.input.sprintIsPressed && Ctx.gunMachine.isReloading) {
            SwitchState(Factory.Walk());
        }
        else if(!Ctx.input.moveIsPressed) {
            SwitchState(Factory.Idle());
        }
        else if(Ctx.input.crouchIsPressed) {
            SwitchState(Factory.Crouch());
        }
    }

  

    public override void InitializeSubState(){}




}



