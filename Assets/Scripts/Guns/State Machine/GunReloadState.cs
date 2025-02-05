using Unity.VisualScripting;

public class GunReloadState : GunBaseState {
   

    public GunReloadState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        // Debug.Log("Enter RELOAD");
        Ctx.ActiveGunScriptable.StartReloadAudio();
        Ctx.anim.SetTrigger(Ctx.reloadHash);
        Ctx.isReloading = true;
        InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
        
    }

    public override void ExitState(){
       
        
    }
    
    public override void CheckSwitchStates(){
        
        
        if (!Ctx.isReloading && Ctx.stateMachine.currentState.CurrentSubstate() == Ctx.stateMachine.states.Sprint()){
            
            SwitchState(Factory.Stow());
        }

        else if(!Ctx.isReloading && Ctx.ActiveGunScriptable.Name == "Shotgun") {
            SwitchState(Factory.IncrementalReload());
        }
        
        else if(!Ctx.isReloading || Ctx.input.mouseScroll != 0) {

            SwitchState(Factory.Rest());
        }

        
        
        
    }

    public override void InitializeSubState(){
    }

}
