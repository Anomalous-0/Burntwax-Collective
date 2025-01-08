using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallChargeState : PlayerBaseState {

    private RaycastHit frontWallHit;
    private RaycastHit backWallHit;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    Vector3 wallNormal;
    
    public PlayerWallChargeState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
    : base (currentContext, playerStateFactory){
        IsRootState = true;
    }


    public override void EnterState(){
        InitializeSubState(); 
        Ctx.MoveMultiplier = Ctx.WallMultiplier;
        Ctx.rb.drag = Ctx.WallDrag;
        Debug.Log("CHARGE STATE");
        
    }

    public override void UpdateState(){
        CheckSwitchStates();
        WallCheck();
        WallRun();
        
    }

    public override void ExitState(){
        Ctx.rb.velocity = new Vector3(Ctx.rb.velocity.x, 0f, Ctx.rb.velocity.z);
        Ctx.rb.useGravity = true; 
    }
    
    public override void CheckSwitchStates(){
        if(!Ctx.WallLeft && !Ctx.WallRight && !Ctx.WallFront && !Ctx.WallBack) {
            SwitchState(Factory.Fall());
        }
        else if(!Ctx.input.jumpIsPressed) {
            Vector3 force = Ctx.moveDirection * Ctx.WallJumpForce;
            Ctx.rb.AddForce(force, ForceMode.Impulse);
            Ctx.rb.AddForce(3f *wallNormal.normalized, ForceMode.Impulse);
            SwitchState(Factory.Walljump());
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
