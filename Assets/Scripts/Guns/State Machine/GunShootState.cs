using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GunShootState : GunBaseState {
   
    public GunShootState(GunStateMachine currentContext, GunStateFactory gunStateFactory): base (currentContext, gunStateFactory) {}

    int switchTime = 3;
    public override void EnterState(){
        Ctx.isShooting = true;
        Ctx.Recoil();

        // if(Ctx.camMachine.currentState == Ctx.camMachine.states.Aim()){
        //     Ctx.gunAnimator.SetTrigger(Ctx.aimShootHash);
        //     Ctx.handAnimator.SetTrigger(Ctx.aimShootHash);
        // }
        // else {
            // Ctx.gunAnimator.SetTrigger(Ctx.shootHash);
            // Ctx.handAnimator.SetTrigger(Ctx.shootHash);
        // }

        
        
        // Debug.Log("Shoot State");
    }

    public override void UpdateState(){
        switchTime--;
        if(Ctx.input.shootIsPressed) {
            Ctx.ActiveGun.Shoot();
        }
        
        
        CheckSwitchStates();
    }

    public override void ExitState(){

    }
    
    public override void CheckSwitchStates(){
        if(!Ctx.isShooting || switchTime <= 0) {
            Ctx.isShooting = false;
            SwitchState(Factory.NoShoot());
        }
        
        
        
    }


   


    
    public override void InitializeSubState(){}

    
}
