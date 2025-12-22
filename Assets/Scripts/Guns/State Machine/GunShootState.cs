using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GunShootState : GunBaseState {
   
    public GunShootState(GunStateMachine currentContext, GunStateFactory gunStateFactory): base (currentContext, gunStateFactory) {}

    // int switchTime = 3;
    public override void EnterState(){
        Shoot();

        // Debug.Log("Shoot State");
    }

    public override void UpdateState(){  
        CheckSwitchStates();
        if(Ctx.anim.GetBool(Ctx.proceduralShootHash)) {  
            Ctx.recoil.Recoil();
        }
    }

    public override void ExitState(){
        Ctx.ableToShootInMiddleOfAnimation = false;
        Ctx.anim.SetBool(Ctx.proceduralShootHash, false);
    }
    
    public override void CheckSwitchStates(){
        if((Ctx.CanShoot() || Ctx.ableToShootInMiddleOfAnimation) && Ctx.input.shootIsPressed) {
            
            Shoot();
            
        }
        else if(!Ctx.isShooting) {

            SwitchState(Factory.NoShoot());
        }

        
        
        
    }


   


    
    public override void InitializeSubState(){}


    private void Shoot() {
        Ctx.ableToShootInMiddleOfAnimation = false;
        Ctx.isShooting = true;
        Ctx.ActiveGunScriptable.Shoot();
        Recoil();
        if(Ctx.CurrentState == Ctx.states.Aim()) {
            Ctx.anim.Play("recoil_aim_" + Ctx.ActiveGunScriptable.Type);
        }
        else {
            Ctx.anim.Play("recoil_" + Ctx.ActiveGunScriptable.Type);
        }
        if(Ctx.ActiveGunScriptable.proceduralRecoil) {
            Ctx.anim.SetBool(Ctx.proceduralShootHash, true);
        }
    }

    public void Recoil() {
        if(Ctx.camShake) {
            Ctx.camShake.GenerateImpulse(Camera.main.transform.forward);
        }
        
        
    }
    
}
