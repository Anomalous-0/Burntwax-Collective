using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GloveReleaseState : GloveBaseState {
    float switchTime;
    
    public GloveReleaseState(Glove currentContext, GloveStateFactory gloveStateFactory)
    : base (currentContext, gloveStateFactory) {
        IsRootState = true;

    }


    public override void EnterState(){
        Ctx.animator.SetTrigger(Ctx.droppingHash);
        switchTime = Ctx.TempStateTime;
        Ctx.Target.useGravity = true;
        Ctx.Force = Ctx.AttractForce;
        Ctx.Target = null;
        // Debug.Log("Release State");
        InitializeSubState();
    }

    public override void UpdateState(){
        switchTime--;
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        
        Ctx.capsule.enabled = true;
    }
    
    public override void CheckSwitchStates(){
        if(switchTime == 0) {
            SwitchState(Factory.Rest());
        }
        
        
    }

    public override void InitializeSubState(){}


}
