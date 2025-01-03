using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCHobbleState : NPCBaseState
{
    public NPCHobbleState(NPCStateMachine currentContext, NPCStateFactory npcStateFactory)
    : base(currentContext, npcStateFactory)
    {
        
    }

    public override void EnterState()
    {
        // Reduce speed and change animation
    }


    // NPCs either be hobbling or be dead no in between
    public override void UpdateState() {}
    public override void ExitState(){}
    public override void CheckSwitchStates() {}
    public override void InitializeSubState() { }
}
