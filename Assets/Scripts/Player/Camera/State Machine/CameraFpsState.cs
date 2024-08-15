using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFpsState : CameraBaseState {
   
    public CameraFpsState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
    : base (currentContext, cameraStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        // Debug.Log("Locked State");
       InitializeSubState();
    }

    public override void UpdateState(){
        Ctx.FPS();
        CheckSwitchStates();
       
    }

    public override void ExitState(){
       
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.player.currentState.CurrentSubstate() == Ctx.player.states.Sprint() && Ctx.input.aimIsPressed) {
            Ctx.input.sprintIsPressed = false;
            SwitchState(Factory.Aim());
        }
        else if(Ctx.input.aimIsPressed && Ctx.gun.CurrentState != Ctx.gun.states.Reload()) {
            SwitchState(Factory.Aim());
        }
    }


    public override void InitializeSubState(){}

}
