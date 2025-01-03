using System.Collections.Generic;

enum NPCStates {
    idle,
    interact, 
    danger,
    death,
    patrol,
    work,
    run,
    hobble
}

public class NPCStateFactory{

    NPCStateMachine context;
    Dictionary<NPCStates, NPCBaseState> states = new Dictionary<NPCStates, NPCBaseState>();

    public NPCStateFactory(NPCStateMachine currentContext) {
        context = currentContext;
        states[NPCStates.idle] = new NPCIdleState(context, this);
        states[NPCStates.interact] = new NPCInteractState(context, this);
        states[NPCStates.danger] = new NPCDangerState(context, this);
        states[NPCStates.death] = new NPCDeathState(context, this);
        states[NPCStates.patrol] = new NPCPatrolState(context, this);
        states[NPCStates.work] = new NPCWorkState(context, this);
        states[NPCStates.run] = new NPCRunState(context, this);
        states[NPCStates.hobble] = new NPCHobbleState(context, this);


    }

    public NPCBaseState Idle() {
        return states[NPCStates.idle];
    }

    public NPCBaseState Interact() {
        return states[NPCStates.interact];
    }

    public NPCBaseState Danger()
    {
        return states[NPCStates.danger];
    }

    public NPCBaseState Death()
    {
        return states[NPCStates.death];
    }

    public NPCBaseState Patrol()
    {
        return states[NPCStates.patrol];
    }

    public NPCBaseState Work()
    {
        return states[NPCStates.work];
    }

    public NPCBaseState Run()
    {
        return states[NPCStates.run];
    }

    public NPCBaseState Hobble()
    {
        return states[NPCStates.hobble];
    }



}
