using UnityEngine;

public class PlayerJumpState : PlayerBaseState {

    private RaycastHit frontWallHit;
    private RaycastHit backWallHit;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    
    public PlayerJumpState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
    : base (currentContext, playerStateFactory){
        IsRootState = true;
    }
    
    public override void EnterState(){
        InitializeSubState(); 
        Ctx.MoveMultiplier = Ctx.AirMultiplier;
        Ctx.rb.drag = Ctx.AirDrag;
        
        
        Jump();
        //Debug.Log("JUMP STATE");
        
    }

    public override void UpdateState(){

        WallCheck();
        
        CheckSwitchStates();


    }

    public override void ExitState(){
        
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.playerIsGrounded) {
            SwitchState(Factory.Grounded());
        }
        else if(Ctx.WallLeft || Ctx.WallRight || Ctx.WallFront || Ctx.WallBack) {
            SwitchState(Factory.Wallrun());
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

    private void Jump() {
        // reset y velocity
        
        // Ctx.rb.velocity = new Vector3(Ctx.rb.velocity.x, 0f, Ctx.rb.velocity.z);
        Ctx.rb.AddForce(Ctx.player.up * Ctx.JumpForce, ForceMode.Impulse);
        
    }


    private void WallCheck() {
        Ctx.WallRight = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.right, out rightWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallLeft = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.right, out leftWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallFront = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.forward, out frontWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallBack = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.forward, out backWallHit, Ctx.WallCheckDistance, Ctx.Wall);
    }

}