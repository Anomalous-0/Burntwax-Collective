using UnityEngine;

namespace Burntwax
{
    // Branch B, step 2: release pulse. Spawns the plasma projectile (charge-scaled) and returns to
    // Idle after a brief pose window.
    public class GloveBlastState : GloveBaseState
    {


        public GloveBlastState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        { }

        public override void EnterState()
        {

            Ctx.PlayGloveAnimation(Ctx.blastHash);
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
            if(Ctx.BlastEndAnim)
            {
                Ctx.BlastEndAnim = false;
                SwitchState(Factory.Idle());    
            }

        }

        public override void InitializeSubState() { }
    }
}
