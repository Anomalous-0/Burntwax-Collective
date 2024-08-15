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
        if(Ctx.CurrentState.CurrentSuperstate() != Ctx.states.Reload() &&  Ctx.CurrentState.CurrentSuperstate() != Ctx.states.Stow() && Ctx.CanShoot() && Ctx.input.shootIsPressed && !Ctx.isShooting && Time.time - Ctx.ActiveGun.LastShootTime >= Ctx.ActiveGun.ShootConfig.FireRate) {
            SwitchState(Factory.Shoot());
        }
    }

    public override void InitializeSubState(){}

    
}
