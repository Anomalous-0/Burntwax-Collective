using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteractState : NPCBaseState {

    public NPCInteractState (NPCStateMachine currentContext, NPCStateFactory npcStateFactory)
    : base (currentContext, npcStateFactory) {
        IsRootState = true;
    }

    public override void EnterState() {
        Debug.Log("INTERACT STATE");
        InitializeSubState();
    }

    public override void UpdateState(){



        // Ctx.npc.transform.rotation = Quaternion.Slerp(Ctx.npc.transform.rotation, Quaternion.LookRotation(Ctx.player.transform.position - Ctx.npc.transform.position, Vector3.up), Time.deltaTime * 5f);

        CheckSwitchStates();
       
    }

    public override void ExitState(){
       
    }
    
    public override void CheckSwitchStates(){
        if (false/*Danger arises*/)
        {
            SwitchState(Factory.Danger());
        } else if (Ctx.interactionHit && Ctx.input.interactIsPressed)
        {
            SwitchState(Factory.Idle());
        }
    }


    public override void InitializeSubState(){}

}
