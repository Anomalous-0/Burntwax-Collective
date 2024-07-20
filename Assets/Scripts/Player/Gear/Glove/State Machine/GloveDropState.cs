using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveDropState : GloveBaseState {
   
    float switchTime;
    public GloveDropState(Glove currentContext, GloveStateFactory gloveStateFactory)
    : base (currentContext, gloveStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        switchTime = Ctx.TempStateTime;
        Ctx.Target.useGravity = true;
        Ctx.Force = Ctx.AttractForce;
        Ctx.Target.interpolation = RigidbodyInterpolation.Interpolate;
        Ctx.Target = null;
        

       InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
        
    }

    public override void ExitState(){}
    
    public override void CheckSwitchStates(){
       
    }

    public override void InitializeSubState(){}

}
