using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunNoShootState : GunBaseState {
   
    public GunNoShootState(GunStateMachine currentContext, GunStateFactory gunStateFactory): base (currentContext, gunStateFactory) {}

    public override void EnterState(){

        
        
        // Debug.Log("No Shoot State");
    }

    public override void UpdateState(){

        
        CheckSwitchStates();
    }

    public override void ExitState(){}
    
    public override void CheckSwitchStates(){
        if(Ctx.currentState.CurrentSuperstate() != Ctx.states.Reload() &&  Ctx.currentState.CurrentSuperstate() != Ctx.states.Stow() && Ctx.CanShoot() && Ctx.input.shootIsPressed && !Ctx.isShooting) {
            SwitchState(Factory.Shoot());
        }
    }

    public override void InitializeSubState(){}

    
}
