using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCrouchState : PlayerBaseState {
    
    public PlayerCrouchState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base (currentContext, playerStateFactory){

    }

    public override void EnterState(){
        Ctx.PlayerVelocity = Ctx.CrouchVelocity;
        Debug.Log("CROUCH STATE");
        Ctx.LocalScale = new Vector3(Ctx.LocalScale.x, Ctx.CrouchYScale, Ctx.LocalScale.z);
        Ctx.rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    public override void UpdateState(){
        // if(Ctx.input.aimIsPressed) {
        //     Ctx.cam.Aim();
    
        // }
        // else {
        //     Ctx.cam.FPS();
        // }
        CheckSwitchStates();
    }

    public override void ExitState(){
        Ctx.LocalScale = new Vector3(Ctx.LocalScale.x, Ctx.StartYScale, Ctx.LocalScale.z);
    }
    
    public override void CheckSwitchStates(){
        // if raycast upwards hits something (meaning if the player let go of crouch but is still right under stomething, then stay in crouch mode)
        
        if(!Ctx.input.crouchIsPressed && Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed) {
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



