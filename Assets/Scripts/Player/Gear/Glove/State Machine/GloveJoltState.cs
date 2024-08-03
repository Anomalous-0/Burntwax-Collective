using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveJoltState : GloveBaseState {
   
    public GloveJoltState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
    : base (currentContext, gloveStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
       InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
        
    }

    public override void ExitState(){}
    
    public override void CheckSwitchStates(){
       
    }

    public override void InitializeSubState(){}

}
