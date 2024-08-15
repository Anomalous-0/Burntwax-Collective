using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallRunState : PlayerBaseState {

    private RaycastHit frontWallHit;
    private RaycastHit backWallHit;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    Vector3 wallNormal;
    
    public PlayerWallRunState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
    : base (currentContext, playerStateFactory){
        IsRootState = true;
    }


    public override void EnterState(){
        InitializeSubState(); 
        Ctx.MoveMultiplier = Ctx.WallMultiplier;
        Ctx.rb.drag = Ctx.WallDrag;
        Ctx.rb.velocity = new Vector3(Ctx.rb.velocity.x, 0, Ctx.rb.velocity.z);
        Ctx.WallRunSwitch = 1;
        Ctx.rb.useGravity = false;
        //Debug.Log("WALLRUN STATE");

    }

    public override void UpdateState(){
        CheckSwitchStates();
        WallCheck();
        WallRun();
        
    }

    public override void ExitState(){
        Ctx.WallRunSwitch = 0;
        Ctx.rb.useGravity = true; 
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.playerIsGrounded) {
            SwitchState(Factory.Grounded());
        }
        else if(Ctx.input.jumpIsPressed) {
            Ctx.rb.velocity = new Vector3(Ctx.rb.velocity.x, 0, Ctx.rb.velocity.z);
            Vector3 force =   4f * wallNormal * Ctx.WallJumpForce;
            Ctx.rb.AddForce(force, ForceMode.Impulse);
            SwitchState(Factory.Walljump());
        }
        else if((!Ctx.WallLeft && !Ctx.WallRight && !Ctx.WallFront && !Ctx.WallBack)) {
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

    private void WallRun() {
        if(Ctx.WallRight) {
            wallNormal = rightWallHit.normal;
        } 
        else if(Ctx.WallLeft) {
            wallNormal = leftWallHit.normal;
        }
        else if(Ctx.WallFront) {
            wallNormal = frontWallHit.normal;
        }
        else if(Ctx.WallBack) {
            wallNormal = backWallHit.normal;
        }
        Vector3 wallForward = Vector3.Cross(wallNormal, Ctx.player.up);
       
        //Ctx.rb.velocity = new Vector3((Ctx.orientation.forward - wallForward).magnitude, 0, 0);


        
        
        Ctx.rb.AddForce(-wallNormal * 200);
        

    }

    private void WallCheck() {
        Ctx.WallRight = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.right, out rightWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallLeft = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.right, out leftWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallFront = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.forward, out frontWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallBack = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.forward, out backWallHit, Ctx.WallCheckDistance, Ctx.Wall);
    }

}
