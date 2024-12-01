using System.Collections.Generic;

enum NPCStates {
    idle,
    interact, 
    danger
}

public class NPCStateFactory{

    NPCStateMachine context;
    Dictionary<NPCStates, NPCBaseState> states = new Dictionary<NPCStates, NPCBaseState>();

    public NPCStateFactory(NPCStateMachine currentContext) {
        context = currentContext;
        states[NPCStates.idle] = new NPCIdleState(context, this);
        states[NPCStates.interact] = new NPCInteractState(context, this);
        states[NPCStates.danger] = new NPCDangerState(context, this);
        

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



}
