using UnityEngine;

namespace Burntwax
{
    // Branch B, step 2: release pulse. Spawns the plasma projectile (charge-scaled) and returns to
    // Idle after a brief pose window.
    public class GloveBlastEMState : GloveBaseState
    {
        private const float FireDuration = 0.15f;
        private float enterTime;

        public GloveBlastEMState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        { }

        public override void EnterState()
        {
           
            enterTime = Time.time;
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
            if (Time.time - enterTime >= FireDuration)
            {
                SwitchState(Factory.Idle());
            }
        }

        public override void InitializeSubState() { }
    }
}
