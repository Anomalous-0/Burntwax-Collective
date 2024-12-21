using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAimState : CameraBaseState {
   
    public CameraAimState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
    : base (currentContext, cameraStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        // Debug.Log("Locked State");
       InitializeSubState();
    }

    public override void UpdateState(){
        Ctx.Aim();
        CheckSwitchStates();
       
    }

    public override void ExitState(){
       
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.input.sprintIsPressed && Ctx.input.aimIsPressed  || !Ctx.input.aimIsPressed || Ctx.gun.CurrentState == Ctx.gun.states.Reload()) {

            Ctx.input.aimIsPressed = false;
            SwitchState(Factory.Fps());
        }
       
        
    }


    public override void InitializeSubState(){}

}
