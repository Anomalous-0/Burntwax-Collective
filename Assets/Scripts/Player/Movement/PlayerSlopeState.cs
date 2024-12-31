using UnityEngine;

public class PlayerSlopeState : PlayerBaseState {
    
    public PlayerSlopeState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
    : base (currentContext, playerStateFactory){
        IsRootState = true;
        
    }
    
    public override void EnterState(){
        // Debug.Log("ENTERING SLOPE STATE");
        InitializeSubState();
        Ctx.rb.useGravity = false;
        if(!Ctx.input.moveIsPressed) {
            Ctx.rb.velocity = new Vector3(0, Ctx.rb.velocity.y, 0);
        }
        Ctx.MoveMultiplier = Ctx.GroundMultiplier;
        Ctx.rb.drag = Ctx.GroundDrag;
        
    }

    public override void UpdateState(){
        
        CheckSwitchStates(); 
    }

   public override void ExitState(){

        Ctx.rb.velocity = new Vector3(Ctx.rb.velocity.x, 0f, Ctx.rb.velocity.z);
        Ctx.rb.useGravity = true;
    }
    
    public override void CheckSwitchStates(){    
        if(Ctx.input.jumpIsPressed) {
            SwitchState(Factory.Jump());
        }
        else if(!Ctx.playerIsSloped && !Ctx.playerIsGrounded) {
            SwitchState(Factory.Fall());
        }
        else if(!Ctx.playerIsSloped) {
            SwitchState(Factory.Grounded());
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
