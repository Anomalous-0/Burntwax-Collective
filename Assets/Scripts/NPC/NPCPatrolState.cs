using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCPatrolState : NPCBaseState
{
    public NPCPatrolState(NPCStateMachine currentContext, NPCStateFactory npcStateFactory)
    : base(currentContext, npcStateFactory)
    {

    }

    public override void EnterState()
    {

    }

    public override void UpdateState()
    {

        CheckSwitchStates();

    }

    public override void ExitState()
    {

    }

    public override void CheckSwitchStates()
    {

        if (false/*Unknown trigger. Do we even want an NPC to be able to switch between patrolling and working?*/)
        {
            SwitchState(Factory.Work());
        }

    }


    public override void InitializeSubState() { }
}
