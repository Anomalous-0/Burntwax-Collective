using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunReloadState : GunBaseState {
   

    float switchTime;
    public GunReloadState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        switchTime = 1;
        Ctx.animator.SetTrigger(Ctx.reloadHash);
        Ctx.isReloading = true;
        Debug.Log("Reload State");
        InitializeSubState();
    }

    public override void UpdateState(){
        switchTime--;
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        Ctx.EndReload();
    }
    
    public override void CheckSwitchStates(){
        
        // if(switchTime == 0) {
            if(Ctx.input.aimIsPressed) {
                SwitchState(Factory.Aim());
            }
            else if (Ctx.stateMachine.currentState.CurrentSubstate() == Ctx.stateMachine.states.Sprint()){
                SwitchState(Factory.Stow());
            }
            else  {
                SwitchState(Factory.Rest());
            // }
        }
    }

    private IEnumerator Countdown() {
        yield return new WaitForSeconds(5f);
    }


    public override void InitializeSubState(){
    
       
    }

}
