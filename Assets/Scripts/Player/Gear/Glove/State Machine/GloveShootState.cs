using UnityEngine;

public class GloveShootState : GloveBaseState {
    float switchTime;
    public GloveShootState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
    : base (currentContext, gloveStateFactory) {
        IsRootState = true;

    }

    public override void EnterState(){
        Ctx.animator.SetTrigger(Ctx.shootingHash);
        switchTime = 45;
        // Ctx.looking = false;
        Ctx.Target.useGravity = true;
        Shoot(Ctx.Target);
        Ctx.Target = null;
        // Debug.Log("Shoot State");
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

    private void Shoot(Rigidbody target) {
        Vector3 aimDir = (Ctx.cam.mouseWorldPosition - target.transform.position).normalized;
        target.AddForce(Ctx.ShootForce * aimDir, ForceMode.Impulse);
    }

    public override void InitializeSubState(){}

}
