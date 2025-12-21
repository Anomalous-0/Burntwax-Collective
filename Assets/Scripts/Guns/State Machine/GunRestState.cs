using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunRestState : GunBaseState {
   
    public GunRestState(GunStateMachine currentContext, GunStateFactory gunStateFactory)
    : base (currentContext, gunStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        // Ctx.anim.SetBool(Ctx.equipHash, true);  
        // Ctx.recoil.SetRestingPosition();     
        Ctx.isReloading = false;
        Ctx.isIncrementalReloading = false;
        Ctx.isShooting = false;
        // Debug.Log("Rest State");
        InitializeSubState();
    }

    public override void UpdateState(){
        // Ctx.ActiveGun.Rest();
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        
        // Ctx.anim.SetBool(Ctx.equipHash, false);       
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.stateMachine.prioritySorter.AimIsPriority() && Ctx.CurrentState.CurrentSubstate() != Ctx.states.Shoot()) {
            SwitchState(Factory.Aim());
        }
        else if(Ctx.stateMachine.prioritySorter.SprintIsPriority() && Ctx.stateMachine.currentState.CurrentSubstate() != Ctx.stateMachine.states.Idle() && Ctx.CurrentState.CurrentSubstate() != Ctx.states.Shoot()) {
            Debug.Log("Switching to Stow from Rest");
            SwitchState(Factory.Stow());
        }
        else if((Ctx.AutoReload()  || Ctx.ManualReload()) && Ctx.fullyEquipped) {
            Ctx.fullyEquipped = false;
            Ctx.anim.SetBool(Ctx.proceduralShootHash, false);
            SwitchState(Factory.Reload());
        }
       
    }


    public override void InitializeSubState(){
        SetSubState(Factory.NoShoot());
        

    }

}
