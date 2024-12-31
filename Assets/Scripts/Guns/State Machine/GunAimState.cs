using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAimState : GunBaseState {
   
    public GunAimState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.anim.SetBool(Ctx.aimHash, true);
        // Debug.Log("Aim State");
        InitializeSubState();
    }

    public override void UpdateState(){
        // Ctx.ActiveGun.ADS();
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        Ctx.anim.SetBool(Ctx.aimHash, false);
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.stateMachine.prioritySorter.SprintIsPriority()) {
            Debug.Log("AIM TO STOW");
            //FIX THIS 
            //Ctx.input.aimIsPressed = false;
            SwitchState(Factory.Stow());
        }
        else if(!Ctx.input.aimIsPressed) {
            SwitchState(Factory.Rest());
        }
        else if(Ctx.AutoReload()  || Ctx.ManualReload()) {
            SwitchState(Factory.Reload());
        }
    }


    public override void InitializeSubState(){
        SetSubState(Factory.NoShoot());
    }

}
