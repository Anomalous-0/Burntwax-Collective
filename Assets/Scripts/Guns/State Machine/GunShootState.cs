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
            Ctx.animator.SetTrigger(Ctx.aimShootHash);
        }
        else {
            Ctx.animator.SetTrigger(Ctx.shootHash);
        }

        
        
        Debug.Log("Shoot State");
    }

    public override void UpdateState(){

        switchTime--;
        Ctx.ActiveGun.Shoot();
        
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
