using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFpsState : CameraBaseState {
   
    public CameraFpsState(CameraStateMachine currentContext, CameraStateFactory cameraStateFactory)
    : base (currentContext, cameraStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        // Debug.Log("Locked State");
       InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
       
    }

    public override void ExitState(){
       
    }
    
    public override void CheckSwitchStates(){
        
    }


    public override void InitializeSubState(){}

}
