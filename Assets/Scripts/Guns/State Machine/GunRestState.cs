using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunRestState : GunBaseState {
   
    public GunRestState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
       
        // Debug.Log("Rest State");
        InitializeSubState();
    }

    public override void UpdateState(){
        Ctx.ActiveGun.Rest();
        CheckSwitchStates();
        
    }

    public override void ExitState(){
                
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.input.aimIsPressed) {
            SwitchState(Factory.Aim());
        }
        else if(Ctx.stateMachine.currentState.CurrentSubstate() == Ctx.stateMachine.states.Sprint()){
            SwitchState(Factory.Stow());
        }
        else if(Ctx.AutoReload()  || Ctx.ManualReload()) {
            SwitchState(Factory.Reload());
        }
    }


    public override void InitializeSubState(){
        SetSubState(Factory.NoShoot());
        

    }

}
