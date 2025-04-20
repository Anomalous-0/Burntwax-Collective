using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFpsState : CameraBaseState {
   
    public CameraFpsState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
    : base (currentContext, cameraStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
     
       InitializeSubState();
    }

    public override void UpdateState(){
        Ctx.FPS();
        CheckSwitchStates();
       
    }

    public override void ExitState(){
       
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.player.prioritySorter.AimIsPriority() && Ctx.gun.CurrentState != Ctx.gun.states.Reload() && Ctx.gun.CurrentState.CurrentSubstate() != Ctx.gun.states.Shoot()) {
            SwitchState(Factory.Aim());
        }
        if(Ctx.pauseMenu.Paused) {
            if(Ctx.pauseMenu.pauseCounterCamThing < 2) {
                Ctx.pauseMenu.pauseCounterCamThing++;
                SwitchState(Factory.Aim());
            }
        }
        
        
    }


    public override void InitializeSubState(){}

}
