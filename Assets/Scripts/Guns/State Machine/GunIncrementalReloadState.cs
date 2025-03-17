using UnityEngine;

public class GunIncrementalReloadState : GunBaseState {
   

    public GunIncrementalReloadState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.fullyEquipped = true;
        Ctx.ActiveGunScriptable.StartReloadAudio();
        Ctx.anim.SetBool(Ctx.incrementalReloadHash, true);
        Ctx.isIncrementalReloading = true;
        InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        // Debug.Log("Exit INCREMENTAL STATE");
        Ctx.anim.SetBool(Ctx.incrementalReloadHash, false);
        Ctx.isIncrementalReloading = false; 
        
    }
    
    public override void CheckSwitchStates(){
        
        if(Ctx.stateMachine.prioritySorter.ShootIsPriority() && Ctx.ActiveGunScriptable.AmmoConfig.currentClip > 0) {
            Ctx.anim.SetTrigger(Ctx.shotWhileIncReloadHash);
            SwitchState(Factory.Rest());
        }
        else if(Ctx.ActiveGunScriptable.AmmoConfig.currentClip == Ctx.ActiveGunScriptable.AmmoConfig.clipSize) {
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
