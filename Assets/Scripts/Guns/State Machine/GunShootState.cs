using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunShootState : GunBaseState {
   
    public GunShootState(GunStateMachine currentContext, GunStateFactory gunStateFactory): base (currentContext, gunStateFactory) {}

    public override void EnterState(){
        
        
        Debug.Log("SHOOT STATE");
    }

    public override void UpdateState(){

        
        CheckSwitchStates();
    }

    public override void ExitState(){}
    
    public override void CheckSwitchStates(){
       
    }

    public override void InitializeSubState(){}

    
}
