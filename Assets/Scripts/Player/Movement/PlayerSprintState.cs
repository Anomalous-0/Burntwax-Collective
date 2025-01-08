using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSprintState : PlayerBaseState {
    

    public PlayerSprintState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base (currentContext, playerStateFactory){

    }

    public override void EnterState(){
        Ctx.PlayerVelocity = Ctx.SprintVelocity;
        // Ctx.cam.FPS();
        
        // Debug.Log("SPRINT STATE");
    }

    public override void UpdateState(){

        CheckSwitchStates();
    }

    public override void ExitState(){
        
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.currentState != Ctx.states.Wallcharge()) {
            if(Ctx.input.moveIsPressed) {
                if(!Ctx.prioritySorter.SprintIsPriority() || Ctx.gunMachine.isReloading) {
                    SwitchState(Factory.Walk());
                }    
            }
            else if(!Ctx.input.moveIsPressed) {
                SwitchState(Factory.Idle());
            } 
            else if(Ctx.input.crouchIsPressed) {
                SwitchState(Factory.Crouch());
            }
        }   
        
    }

  

    public override void InitializeSubState(){}




}



