using UnityEngine;

namespace Burntwax
{
    // Branch A, step 0: the attract wind-up. Plays the reach-out pose for a brief beat before the
    // pull actually begins, so "getting ready to attract" is its own action rather than folded into
    // the pull. Detection keeps running in the context, so Attract still captures a fresh target.
    public class GloveReadyAttractState : GloveBaseState
    {
        private const float ReadyDuration = 0.06f;
        private float enterTime;

        public GloveReadyAttractState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            Ctx.PlayGloveAnimation(Ctx.attractHash);
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
            // Target slipped out of reach during the wind-up — abort back to Idle.
            if (!Ctx.CanAttractObject && Ctx.detectedObject == null)
            {
                SwitchState(Factory.Idle());
                return;
            }

            if (Time.time - enterTime >= ReadyDuration)
            {
                SwitchState(Factory.Attract());
            }
        }

        public override void InitializeSubState() { }
    }
}
