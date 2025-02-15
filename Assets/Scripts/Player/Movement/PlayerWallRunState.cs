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
        Ctx.rb.linearDamping = Ctx.WallDrag;
        Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0, Ctx.rb.linearVelocity.z);
        Ctx.rb.useGravity = false;
        //Debug.Log("WALLRUN STATE");
        
    }

    public override void UpdateState(){

        CheckSwitchStates();
        WallCheck();
        WallRun();
        
    }

    public override void ExitState(){
         
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.playerIsGrounded) {
            Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0f, Ctx.rb.linearVelocity.z);
            Ctx.rb.useGravity = true;
            SwitchState(Factory.Grounded());
        }
        else if(Ctx.input.jumpIsPressed) {
            Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0f, Ctx.rb.linearVelocity.z);
            Ctx.rb.useGravity = true;
            Vector3 force = Ctx.moveDirection * Ctx.WallJumpForce;
            Ctx.rb.AddForce(force, ForceMode.Impulse);
            Ctx.rb.AddForce(3f *wallNormal.normalized, ForceMode.Impulse);
            SwitchState(Factory.Walljump());
            
            
        }
        else if(!Ctx.WallLeft && !Ctx.WallRight && !Ctx.WallFront && !Ctx.WallBack) {
            Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0f, Ctx.rb.linearVelocity.z);
            Ctx.rb.useGravity = true;
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
        // Vector3 wallForward = Vector3.Cross(wallNormal, Ctx.player.up);
       
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
