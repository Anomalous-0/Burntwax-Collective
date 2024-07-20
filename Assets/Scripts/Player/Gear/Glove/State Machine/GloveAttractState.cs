using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveAttractState : GloveBaseState {
   
    float timeToLock;
    public GloveAttractState(Glove currentContext, GloveStateFactory gloveStateFactory)
    : base (currentContext, gloveStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.Target.useGravity = false;
        Ctx.Force = Ctx.AttractForce;
        timeToLock = Ctx.AttractTime;
        // Debug.Log("Attract State");
        InitializeSubState();
    }

    public override void UpdateState(){
        Attract(Ctx.Target);
        CheckSwitchStates();
        
        timeToLock--;
        
    }

    public void FixedUpdate() {
        
    }

    public override void ExitState(){}
    
    public override void CheckSwitchStates(){
        if(!Ctx.input.useGloves || !Ctx.InRange) {
            Ctx.animator.SetBool(Ctx.attractingHash, false);
            SwitchState(Factory.Release());
        }
        else if(timeToLock == 0) {
            SwitchState(Factory.Lock());
        }
        else if(CheckHold(Ctx.Target)) {
            Ctx.animator.SetBool(Ctx.attractingHash, false);
            SwitchState(Factory.Hold());
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
