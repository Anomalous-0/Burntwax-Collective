using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunStowState : GunBaseState {
   
    public GunStowState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.anim.SetBool(Ctx.stowHash, true);
        Ctx.fullyEquipped = true;
        // Debug.Log("Stow State");
        InitializeSubState();
    }

    public override void UpdateState(){
        // Ctx.ActiveGun.Stow();
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        Ctx.anim.SetBool(Ctx.stowHash, false);
        //  Debug.Log("Leave State");
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.stateMachine.currentState.CurrentSubstate() != Ctx.stateMachine.states.Sprint() &&  !Ctx.stateMachine.prioritySorter.AimIsPriority() || Ctx.stateMachine.prioritySorter.ShootIsPriority()) {
            
            SwitchState(Factory.Rest());
        } 
        else if(Ctx.AutoReload()  || Ctx.ManualReload()) {
            SwitchState(Factory.Reload());
        }

        else if(!Ctx.isReloading && Ctx.stateMachine.prioritySorter.AimIsPriority()) {

            //Needed this, otherwise gun goes through equip and then to aim
            
            SwitchState(Factory.Aim());
        }
       
        
    }


    public override void InitializeSubState(){
        SetSubState(Factory.NoShoot());
        
    }

}
