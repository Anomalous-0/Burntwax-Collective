using UnityEngine;

namespace Burntwax
{
    // Branch A, step 3: throw the held/incoming object out along the camera forward, hold the throw
    // pose briefly, then return to Idle.
    public class GloveReleaseObjectState : GloveBaseState
    {
        private const float ReleaseDuration = 0.18f;
        private float enterTime;

        public GloveReleaseObjectState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            Ctx.RestoreHeldObjectPlayerCollisions();
            enterTime = Time.time;
            InitializeSubState();
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (Time.time - enterTime >= ReleaseDuration)
            {
                SwitchState(Factory.Idle());
            }
        }

        public override void InitializeSubState() { }
    }
}
