using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;

public class PlayerCrouchState : PlayerBaseState {
    
    public PlayerCrouchState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base (currentContext, playerStateFactory){

    }

    public override void EnterState(){
        Ctx.PlayerVelocity = Ctx.CrouchVelocity;

        Ctx.LocalScale = new Vector3(Ctx.LocalScale.x, Ctx.CrouchYScale, Ctx.LocalScale.z);
        Ctx.rb.AddForce(Vector3.down * 15f, ForceMode.Impulse);
    }

    public override void UpdateState(){
        CheckSwitchStates();
    }

    public override void ExitState(){
        Ctx.LocalScale = new Vector3(Ctx.LocalScale.x, Ctx.StartYScale, Ctx.LocalScale.z);
    }
    
    public override void CheckSwitchStates(){
        
        //Burner variable
        RaycastHit hit;


        if (Physics.Raycast(Ctx.transform.position, Ctx.transform.TransformDirection(Vector3.up), out hit, 1f))
        {
            // This is to filter everything out before the other checks
        }
        /*
        //Redefine front back and sides
        Vector3 back = new Vector3(Ctx.transform.position.x - Ctx.capsuleCollider.radius, Ctx.transform.position.y, Ctx.transform.position.z);
        Vector3 front = new Vector3(Ctx.transform.position.x + Ctx.capsuleCollider.radius, Ctx.transform.position.y, Ctx.transform.position.z);
        Vector3 left = new Vector3(Ctx.transform.position.x, Ctx.transform.position.y, Ctx.transform.position.z - Ctx.capsuleCollider.radius);
        Vector3 right = new Vector3(Ctx.transform.position.x, Ctx.transform.position.y, Ctx.transform.position.z + Ctx.capsuleCollider.radius);

        if (Physics.Raycast(back, Ctx.transform.TransformDirection(Vector3.up), out hit, 1f)
            && Physics.Raycast(front, Ctx.transform.TransformDirection(Vector3.up), out hit, 1f)
            && Physics.Raycast(left, Ctx.transform.TransformDirection(Vector3.up), out hit, 1f)
            && Physics.Raycast(right, Ctx.transform.TransformDirection(Vector3.up), out hit, 1f)) {
            // This is to filter everything out before the other checks
        }*/
        else if(!Ctx.input.crouchIsPressed && Ctx.input.moveIsPressed && !Ctx.input.sprintIsPressed) {
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



