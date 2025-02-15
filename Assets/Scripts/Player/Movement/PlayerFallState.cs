using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallState : PlayerBaseState {

    private RaycastHit frontWallHit;
    private RaycastHit backWallHit;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    
    public PlayerFallState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
    : base (currentContext, playerStateFactory){
        IsRootState = true;
        
    }


    public override void EnterState(){
        Ctx.MoveMultiplier = Ctx.AirMultiplier;
        Ctx.rb.linearDamping = Ctx.AirDrag;
        Ctx.CoyoteTime = Ctx.CoyoteTimer;
        InitializeSubState();

        //Debug.Log("FALL STATE"); 
    }

    public override void UpdateState(){
        Ctx.CoyoteTime--;
        if(Ctx.CoyoteTime <= 0){
            Ctx.CoyoteTime = 0;
        }
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
        else if(Ctx.CoyoteTime >= 0 && Ctx.input.jumpIsPressed) {
            Ctx.CoyoteTime = Ctx.CoyoteTimer;
            Ctx.rb.linearVelocity = new Vector3(Ctx.rb.linearVelocity.x, 0f, Ctx.rb.linearVelocity.z);
            SwitchState(Factory.Jump());
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


    private void WallCheck() {
        Ctx.WallRight = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.right, out rightWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallLeft = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.right, out leftWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallFront = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, Ctx.player.forward, out frontWallHit, Ctx.WallCheckDistance, Ctx.Wall);
        Ctx.WallBack = Physics.SphereCast(Ctx.player.position, Ctx.SphereCastRadius, -Ctx.player.forward, out backWallHit, Ctx.WallCheckDistance, Ctx.Wall);
    }


}