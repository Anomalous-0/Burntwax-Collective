using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunNoShootState : GunBaseState {
   
    public GunNoShootState(GunStateMachine currentContext, GunStateFactory gunStateFactory): base (currentContext, gunStateFactory) {}

    public override void EnterState(){

        
        
        // Debug.Log("No shoot STATE");
    }

    public override void UpdateState(){

        
        CheckSwitchStates();
    }

    public override void ExitState(){}
    
    public override void CheckSwitchStates(){
        if(Ctx.CanShoot() && Ctx.input.shootIsPressed && Time.time - Ctx.ActiveGun.LastShootTime >= Ctx.ActiveGun.ShootConfig.FireRate) {
            SwitchState(Factory.Shoot());
        }
    }

    public override void InitializeSubState(){}

    
}
