using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunReloadState : GunBaseState {
   

    public GunReloadState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.ActiveGun.StartReloadAudio();
        Ctx.anim.SetTrigger(Ctx.reloadHash);
        Ctx.isReloading = true;
        Ctx.isShooting = false;
        // Debug.Log("Reload State");
        InitializeSubState();
    }

    public override void UpdateState(){
   
        // Ctx.ActiveGun.Rest();
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        Ctx.isReloading = false;
        
    }
    
    public override void CheckSwitchStates(){
        
        if(Ctx.input.aimIsPressed) {
            SwitchState(Factory.Aim());
        }
        else if (!Ctx.isReloading && Ctx.stateMachine.currentState.CurrentSubstate() == Ctx.stateMachine.states.Sprint()){
            SwitchState(Factory.Stow());
        }
        else if(!Ctx.isReloading || Ctx.input.mouseScroll != 0) {

            SwitchState(Factory.Rest());
        }
        
        
    }

    private IEnumerator Countdown() {
        yield return new WaitForSeconds(5f);
    }


    public override void InitializeSubState(){
        // SetSubState(Factory.NoShoot());
       
    }

}
