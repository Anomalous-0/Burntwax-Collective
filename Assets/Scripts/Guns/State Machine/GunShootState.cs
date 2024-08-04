using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GunShootState : GunBaseState {
   
    public GunShootState(GunStateMachine currentContext, GunStateFactory gunStateFactory): base (currentContext, gunStateFactory) {}

    float switchTime;
    public override void EnterState(){
        Ctx.isShooting = true;
        switchTime = 35;

        if(CurrentSuperstate() == Factory.Aim()){
            Ctx.GunSelector.animator.SetTrigger(Ctx.GunSelector.aimShootHash);
        }
        else {
            Ctx.GunSelector.animator.SetTrigger(Ctx.GunSelector.shootHash);
        }

        
        
        Debug.Log("Shoot State");
    }

    public override void UpdateState(){

        switchTime--;
        Ctx.GunSelector.ActiveGun.Shoot();
        
        CheckSwitchStates();
    }

    public override void ExitState(){}
    
    public override void CheckSwitchStates(){
        // if(switchTime == 0) {
            SwitchState(Factory.NoShoot());
        // }
        
    }


    
    public override void InitializeSubState(){}

    
}
