using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDeathState : MonoBehaviour
{
    public NPCDeathState(NPCStateMachine currentContext, NPCStateFactory npcStateFactory)
    : base(currentContext, npcStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        InitializeSubState();
    }

    public override void UpdateState()
    {

        // Play death animation or something

    }

    // Dead men tell no tales
    public override void ExitState(){}
    public override void CheckSwitchStates(){}
    public override void InitializeSubState() {}
}
