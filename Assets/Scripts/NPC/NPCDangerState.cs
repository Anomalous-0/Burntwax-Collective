using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDangerState : NPCBaseState {
   
    public NPCDangerState(NPCStateMachine currentContext, NPCStateFactory npcStateFactory)
    : base (currentContext, npcStateFactory) {
        IsRootState = true;
    }

    public override void EnterState(){
       InitializeSubState();
    }

    public override void UpdateState(){
        
        CheckSwitchStates();
       
    }

    public override void ExitState(){
       
    }
    
    public override void CheckSwitchStates(){
       
        if (false/*Danger stops*/)
        {
            SwitchState(Factory.Idle());
        }
        // There is no switch to interact because there is no scenario in which you should be able to interact with an NPC in danger state
        
    }


    public override void InitializeSubState(){}

}
