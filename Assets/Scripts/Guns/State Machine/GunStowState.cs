using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunStowState : GunBaseState {
   
    public GunStowState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.input.shootIsPressed = false;
        Ctx.anim.SetBool(Ctx.stowHash, true);
        // Debug.Log("Stow State");
        InitializeSubState();
    }

    public override void UpdateState(){
        // Ctx.ActiveGun.Stow();
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        Ctx.anim.SetBool(Ctx.stowHash, false);
        
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.stateMachine.currentState.CurrentSubstate() != Ctx.stateMachine.states.Sprint() || Ctx.input.shootIsPressed){
            SwitchState(Factory.Rest());
        } 
        else if(Ctx.AutoReload()  || Ctx.ManualReload()) {
            SwitchState(Factory.Reload());
        }

        else if(!Ctx.isReloading && Ctx.input.aimIsPressed) {
            Ctx.input.sprintIsPressed = false; 
            SwitchState(Factory.Aim());
        }
       
        
    }


    public override void InitializeSubState(){
        SetSubState(Factory.NoShoot());
        
    }

}
