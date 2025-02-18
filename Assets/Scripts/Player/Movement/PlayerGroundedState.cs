using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerGroundedState : PlayerBaseState {
    
    public PlayerGroundedState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
    : base (currentContext, playerStateFactory){
        IsRootState = true;
        
    }
    
    public override void EnterState(){

        // InitializeSubState();
        // if(!Ctx.input.moveIsPressed) {
        //     Ctx.rb.velocity = new Vector3(0, Ctx.rb.velocity.y, 0);
        // }
        Ctx.MoveMultiplier = Ctx.GroundMultiplier;
        Ctx.rb.linearDamping = Ctx.GroundDrag;
        // Debug.Log("GROUNDED STATE");
    }

    public override void UpdateState(){
        SlideDownSlope();
        CheckSwitchStates();
        


        
    }

    public override void ExitState(){
        Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0f, Ctx.rb.linearVelocity.z);
    }
    
    public override void CheckSwitchStates(){    
        if(Ctx.input.jumpIsPressed) {
            SwitchState(Factory.Jump());
        }
        else if(!Ctx.playerIsGrounded) {
            SwitchState(Factory.Fall());
        }
        else if(Ctx.playerIsSloped) {
            SwitchState(Factory.Slope());
        }
    }

    private void SlideDownSlope() {
        if(Ctx.SlopeTooSteep) {
            
            // Ctx.rb.AddForce(Ctx.groundCheckHit.normal.normalized * Ctx.DownForce);
        }

    }

    public override void InitializeSubState(){
        if(!Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed) {
            
            SetSubState(Factory.Idle());
        } else if(Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed) {
            
            SetSubState(Factory.Walk());
        }
        else { 
            SetSubState(Factory.Sprint());
        }
    }

    

}
