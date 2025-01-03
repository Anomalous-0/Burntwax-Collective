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
        } else if (false/*if the opps knock bro out*/) {
            SwitchState(Factory.Death());
        }
        
    }


    public override void InitializeSubState()
    {
        SetSubState(Factory.Run()); // NPC will always start with running
    }

}
