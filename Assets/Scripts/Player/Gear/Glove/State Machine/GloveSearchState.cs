using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveSearchState : GloveBaseState {
   
    public GloveSearchState(Glove currentContext, GloveStateFactory gloveStateFactory)
    : base (currentContext, gloveStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.animator.SetBool(Ctx.attractingHash, true);
        // Debug.Log("Search State");
        InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
        
    }

    public override void ExitState(){
    }
    
    public override void CheckSwitchStates(){
        if(!Ctx.input.useGloves) {
            Ctx.animator.SetBool(Ctx.attractingHash, false);
            SwitchState(Factory.Rest());
        }
        else if(Ctx.InRange && Ctx.Target != null) {
            SwitchState(Factory.Attract());
        }
    }

    public override void InitializeSubState(){}

}
