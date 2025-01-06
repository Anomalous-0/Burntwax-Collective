using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class NPCIdleState : NPCBaseState {

    public NPCIdleState(NPCStateMachine currentContext, NPCStateFactory npcStateFactory)
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
        if (Ctx.interactionHit && Ctx.input.interactIsPressed) {
            SwitchState(Factory.Interact());
        } else if (false/*danger happens*/){
            SwitchState(Factory.Danger());
        }

    }


    public override void InitializeSubState()
    {
    //     if (/*intiially works when idle*/false)
    //     {
    //         SetSubState(Factory.Work());
    //     } else if (/*initially patrols when idle*/true)
    //     {
    //         SetSubState(Factory.Patrol());
    //     }
    }

}
