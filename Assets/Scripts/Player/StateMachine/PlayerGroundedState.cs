using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundedState : PlayerBaseState {
    
    public PlayerGroundedState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
    : base (currentContext, playerStateFactory){
        IsRootState = true;
        
    }
    
    public override void EnterState(){
        InitializeSubState();
        Ctx.MoveMultiplier = Ctx.GroundMultiplier;
        Ctx.rb.drag = Ctx.GroundDrag;
        //Debug.Log("GROUNDED STATE");
    }

    public override void UpdateState(){
        
        CheckSwitchStates();
        


        
    }

    public override void ExitState(){
        Ctx.rb.velocity = new Vector3(Ctx.rb.velocity.x, 0f, Ctx.rb.velocity.z);
    }
    
    public override void CheckSwitchStates(){    
        if(Ctx.input.jumpIsPressed) {
            SwitchState(Factory.Jump());
        }
        else if(!Ctx.playerIsGrounded) {
            SwitchState(Factory.Fall());
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
