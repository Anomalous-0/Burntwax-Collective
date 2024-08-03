public abstract class GloveBaseState  {

    private bool isRootState = false;
    private GloveStateMachine ctx;
    private GloveStateFactory factory;
    private GloveBaseState currentSubState;
    private GloveBaseState currentSuperState;

    protected bool IsRootState { set { isRootState = value; } }
    protected GloveStateMachine Ctx { get { return ctx; } }
    protected GloveStateFactory Factory { get { return factory; } }

    
    public GloveBaseState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory) {
        ctx = currentContext;
        factory = gloveStateFactory;
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

    protected void SwitchState(GloveBaseState newState) {
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

    protected void SetSuperState(GloveBaseState newSuperState) {
        currentSuperState = newSuperState;
    }

    protected void SetSubState(GloveBaseState newSubState) {
        currentSubState = newSubState;
        newSubState.SetSuperState(this);
    }
}
