public abstract class NPCBaseState  {

    private bool isRootState = false;
    private NPCStateMachine ctx;
    private NPCStateFactory factory;
    private NPCBaseState currentSubState;
    private NPCBaseState currentSuperState;

    protected bool IsRootState { set { isRootState = value; } }
    protected NPCStateMachine Ctx { get { return ctx; } }
    protected NPCStateFactory Factory { get { return factory; } }

    
    public NPCBaseState(NPCStateMachine currentContext, NPCStateFactory npcStateFactory) {
        ctx = currentContext;
        factory = npcStateFactory;
    }

    public abstract void EnterState();

    public abstract void UpdateState();

    public abstract void ExitState();
    
    public abstract void CheckSwitchStates();

    public abstract void InitializeSubState();

    public void UpdateStates() {
        UpdateState();
        if(currentSubState != null) {
            currentSubState.UpdateStates();
        }
    }

    protected void SwitchState(NPCBaseState newState) {
        // current state exists state
        ExitState();

        // new state enters state
        newState.EnterState();

        if(isRootState) {
            // switch current state of context
            ctx.CurrentState = newState;
        } else if(currentSuperState != null) {
            // set the current super state's substate to the new state
            currentSuperState.SetSubState(newState);
        }
        
    }

    protected void SetSuperState(NPCBaseState newSuperState) {
        currentSuperState = newSuperState;
    }

    protected void SetSubState(NPCBaseState newSubState) {
        currentSubState = newSubState;
        newSubState.SetSuperState(this);
    }
}
