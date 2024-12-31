using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteractState : NPCBaseState {
   
    public NPCInteractState (NPCStateMachine currentContext, NPCStateFactory npcStateFactory)
    : base (currentContext, npcStateFactory) {

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
        if (false/*Danger arises*/)
        {
            SwitchState(Factory.Danger());
        } else if (false/*Interaction stopped*/)
        {
            SwitchState(Factory.Idle());
        }
    }


    public override void InitializeSubState(){}

}
