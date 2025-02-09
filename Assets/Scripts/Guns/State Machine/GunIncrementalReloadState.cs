using UnityEngine;

public class GunIncrementalReloadState : GunBaseState {
   

    public GunIncrementalReloadState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Debug.Log("Enter INCREMENTAL STATE");
        Ctx.ActiveGunScriptable.StartReloadAudio();
        Ctx.anim.SetBool(Ctx.incrementalReloadHash, true);
        Ctx.isIncrementalReloading = true;
        InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        Debug.Log("Exit INCREMENTAL STATE");
        Ctx.anim.SetBool(Ctx.incrementalReloadHash, false); 
        
    }
    
    public override void CheckSwitchStates(){
        
        if(Ctx.stateMachine.prioritySorter.ShootIsPriority()) {
            SwitchState(Factory.Rest());
        }
        else if(!Ctx.isIncrementalReloading && Ctx.ActiveGunScriptable.AmmoConfig.currentClip == Ctx.ActiveGunScriptable.AmmoConfig.clipSize) {
            SwitchState(Factory.Rest());
        }

        // if (!Ctx.isIncrementalReloading && Ctx.stateMachine.currentState.CurrentSubstate() == Ctx.stateMachine.states.Sprint()){
            
        //     SwitchState(Factory.Stow());
        // }
        // else if(!Ctx.isIncrementalReloading || Ctx.input.mouseScroll != 0) {

        //     SwitchState(Factory.Rest());
        // }
        
        
    }

    public override void InitializeSubState(){
    }

}
