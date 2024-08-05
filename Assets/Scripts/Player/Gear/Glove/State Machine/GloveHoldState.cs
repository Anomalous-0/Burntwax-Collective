using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveHoldState : GloveBaseState {
   
    public GloveHoldState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
    : base (currentContext, gloveStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.animator.SetBool(Ctx.holdingHash, true);

        Ctx.input.useGloves = false;
        Ctx.Force = Ctx.AttractForce;
        Ctx.Target.interpolation = RigidbodyInterpolation.None;
        Ctx.Target.velocity = Vector3.zero;
        Ctx.capsule.enabled = false;
        Ctx.BodyHierarchy = Ctx.Target.transform.parent.transform;
        Ctx.Target.transform.SetParent(Ctx.GlovePos.transform);
        Ctx.Target.excludeLayers = LayerMask.GetMask("Player");
        // Debug.Log("Hold State");
       InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
        if(Ctx.Target != null) {
            Hold();
        }
        
        
    }

    public override void ExitState(){
        Ctx.animator.SetBool(Ctx.holdingHash, false);
        if(Ctx.Target != null) {
            Ctx.Target.transform.SetParent(Ctx.BodyHierarchy);
            Ctx.Target.interpolation = RigidbodyInterpolation.Interpolate;
            Ctx.Target.excludeLayers = LayerMask.GetMask("Nothing");
        }
        
    }
    
    public override void CheckSwitchStates(){
        if(Ctx.input.useGloves || Ctx.Target == null) {
            SwitchState(Factory.Release());
        }
        else if(Ctx.input.gloveShootIsPressed) {
            SwitchState(Factory.Shoot());
        }
    }


    private void Hold() {
        
            Ctx.Target.transform.position = Vector3.Lerp(Ctx.Target.transform.position, Ctx.GlovePos.position +  .18f* Ctx.HoldDistance * Ctx.GlovePos.forward, Ctx.LerpTime * Time.deltaTime);
    
    }
    public override void InitializeSubState(){}

}
