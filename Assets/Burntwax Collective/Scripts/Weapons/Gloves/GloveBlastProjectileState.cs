using UnityEngine;

namespace Burntwax
{
    // Hold → charge: fling the currently held object out as the projectile, then return to Idle after
    // a brief pose window. Launching also clears heldObject, which is what frees detection back up so
    // objects can be attracted again afterward.
    public class GloveBlastProjectileState : GloveBaseState
    {


        public GloveBlastProjectileState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        { }

        public override void EnterState()
        {

            Ctx.PlayGloveAnimation(Ctx.blastProjectileHash);
            Ctx.LaunchHeldObjectForward();
            InitializeSubState();
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void ExitState()
        {

        }

        public override void CheckSwitchStates()
        {
            if (!Ctx.blastWindow)
                SwitchState(Factory.Idle());
        }



        public override void InitializeSubState() { }
    }
}
