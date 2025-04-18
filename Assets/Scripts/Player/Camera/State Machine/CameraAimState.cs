using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAimState : CameraBaseState {
   
    public CameraAimState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
    : base (currentContext, cameraStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.crosshair.SetAimCrosshair();
        Debug.Log("Aim State");
        InitializeSubState();
    }

    public override void UpdateState(){
        Ctx.Aim();
        CheckSwitchStates();
       
    }

    public override void ExitState(){
        Ctx.crosshair.ResetAimCrosshair();
    }
    

    // MUST CONSOLIDATE AIM AND SHOOT PRIORITY INTO ONE QUEABLE
    public override void CheckSwitchStates(){
        if(Ctx.player.prioritySorter.SprintIsPriority() || !Ctx.input.aimIsPressed || Ctx.gun.CurrentState == Ctx.gun.states.Reload() ) {
            SwitchState(Factory.Fps());
        }
        if(Ctx.pauseMenu.Paused) {
            if(Ctx.pauseMenu.pauseCounterCamThing < 2) {
                Ctx.pauseMenu.pauseCounterCamThing++;
                SwitchState(Factory.Fps());
            }
        }

         
        // if(Ctx.input.sprintIsPressed && Ctx.input.aimIsPressed  || !Ctx.input.aimIsPressed || Ctx.gun.CurrentState == Ctx.gun.states.Reload()) {

        //     Ctx.input.aimIsPressed = false;
        //     SwitchState(Factory.Fps());
        // }
       
        
    }


    public override void InitializeSubState(){}

}
