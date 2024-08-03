using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAimState : GunBaseState {
   
    public GunAimState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
       
        Debug.Log("Aim State");
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
