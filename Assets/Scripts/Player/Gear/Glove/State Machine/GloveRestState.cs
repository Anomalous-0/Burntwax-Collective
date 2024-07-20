using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveRestState : GloveBaseState {
   
    public GloveRestState(Glove currentContext, GloveStateFactory gloveStateFactory)
    : base (currentContext, gloveStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.Target = null;
        Ctx.animator.SetBool(Ctx.restingHash, true);
        // Debug.Log("Rest State");
        InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
        
    }

    public override void ExitState(){
        Ctx.animator.SetBool(Ctx.restingHash, false);
    }
    
    public override void CheckSwitchStates(){
        // if(Ctx.input.useGloves && Ctx.InRange) {
            // SwitchState(Factory.Attract());
        // }
        if(Ctx.input.useGloves) {
            SwitchState(Factory.Search());
        }

    }

    public override void InitializeSubState(){}

}
