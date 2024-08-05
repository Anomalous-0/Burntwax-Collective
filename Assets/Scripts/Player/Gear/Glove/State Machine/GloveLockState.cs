using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveLockState : GloveBaseState {
   
    public GloveLockState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
    : base (currentContext, gloveStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        // Debug.Log("Locked State");
       InitializeSubState();
    }

    public override void UpdateState(){
        CheckSwitchStates();
        Attract(Ctx.Target);
    }

    public override void ExitState(){
        Ctx.animator.SetBool(Ctx.attractingHash, false);
    }
    
    public override void CheckSwitchStates(){
        if(CheckHold(Ctx.Target)) {
            SwitchState(Factory.Hold());
        }
        else if(Ctx.Target == null) {
            Ctx.animator.SetBool(Ctx.attractingHash, false);
            SwitchState(Factory.Release());
        }
    }

    private void Attract(Rigidbody target) {
        Ctx.Force += Ctx.Acceleration;
        Vector3 dir = (Ctx.GlovePos.position - target.position).normalized;
        target.AddForce(dir * Ctx.Force );
    }

    private bool CheckHold(Rigidbody target) {
        
        RaycastHit hit;
        if(Physics.SphereCast(Ctx.GlovePos.position, Ctx.SphereCastRadius, Ctx.GlovePos.forward, out hit, Ctx.HoldDistance)) {
            if(hit.collider.GetComponent<Rigidbody>() != false && hit.collider.GetComponent<Pickupable>() != false) {
                //target = hit.collider.GetComponent<Rigidbody>();
                //target.interpolation = RigidbodyInterpolation.None;

                //bodyHierarchy = target.transform.parent.transform;
                //target.transform.SetParent(glovePos.transform);

                //target.velocity = Vector3.zero;
               
                
                
               
                //capsule.enabled = false;
                return true;
            }
        }
        return false;
       
    }

    public override void InitializeSubState(){}

}
