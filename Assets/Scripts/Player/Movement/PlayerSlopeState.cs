using UnityEngine;

public class PlayerSlopeState : PlayerBaseState {
    
    public PlayerSlopeState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
    : base (currentContext, playerStateFactory){
        IsRootState = true;
        
    }
    
    public override void EnterState(){
        // Debug.Log("ENTERING SLOPE STATE");
        InitializeSubState();
        //Ctx.rb.useGravity = false;
        if(!Ctx.input.moveIsPressed) {
            Ctx.rb.linearVelocity = new Vector3(0, Ctx.rb.linearVelocity.y, 0);
        }
        Ctx.MoveMultiplier = Ctx.GroundMultiplier;
        Ctx.rb.linearDamping = Ctx.GroundDrag;
        
    }

    public override void UpdateState(){
        // if(!Ctx.input.moveIsPressed) {
        //     Ctx.rb.linearVelocity = new Vector3(0, 0, 0);
        // }
        SlideDownSlope();
        CheckSwitchStates(); 
    }

   public override void ExitState(){

        Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0f, Ctx.rb.linearVelocity.z);
        Ctx.rb.useGravity = true;
    }
    
    public override void CheckSwitchStates(){    
        if(Ctx.input.jumpIsPressed && !Ctx.SlopeTooSteep) {
            SwitchState(Factory.Jump());
        }
        else if(!Ctx.playerIsSloped && !Ctx.playerIsGrounded) {
            SwitchState(Factory.Fall());
        }
        else if(!Ctx.playerIsSloped) {
            SwitchState(Factory.Grounded());
        } 
    }


    private void SlideDownSlope() {
        if(Ctx.SlopeTooSteep) {
            Ctx.rb.AddForce(Vector3.down * Ctx.DownForce);
            // Ctx.DisableMovement = true;
        }
        else {
            Ctx.DisableMovement = false;
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
