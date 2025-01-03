using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCRunState : NPCBaseState
{
    public NPCRunState(NPCStateMachine currentContext, NPCStateFactory npcStateFactory)
    : base(currentContext, npcStateFactory)
    {

    }

    public override void EnterState()
    {
        // Change speed to increase it
        // Set a destination
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

        if (false/*Health is in between 0 and certain limit, maybe 65/100? Maybe it's easier to just have a timer? After first hit?*/)
        {
            SwitchState(Factory.Hobble());
        }

    }


    public override void InitializeSubState() { }
}
