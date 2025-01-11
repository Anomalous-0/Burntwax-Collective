using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GunShootState : GunBaseState {
   
    public GunShootState(GunStateMachine currentContext, GunStateFactory gunStateFactory): base (currentContext, gunStateFactory) {}

    // int switchTime = 3;
    public override void EnterState(){
        Ctx.isShooting = true;
        Ctx.ActiveGunScriptable.Shoot();
        Recoil();

        if(Ctx.CurrentState == Ctx.states.Aim()) {
            Ctx.anim.Play("recoil_aim_" + Ctx.ActiveGunScriptable.Name);
        }
        else {
            Ctx.anim.Play("recoil_" + Ctx.ActiveGunScriptable.Name);
        }
        // Debug.Log("Shoot State");
    }

    public override void UpdateState(){  

        CheckSwitchStates();
    }

    public override void ExitState(){

    }
    
    public override void CheckSwitchStates(){
        if(!Ctx.isShooting) {
            SwitchState(Factory.NoShoot());
        }
        
        
        
    }


   


    
    public override void InitializeSubState(){}



    public void Recoil() {
        Ctx.camShake.GenerateImpulse(Camera.main.transform.forward);
        
    }
    
}
