namespace Burntwax
{
    // Branch A, step 1: stop-sign pose, pulling object(s) toward the hand. The primary latches into
    // Hold; a tap or (with katana equipped) a slash launches the incoming object early.
    public class GloveAttractState : GloveBaseState
    {
        public GloveAttractState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            Ctx.SetStopSign(true);
            Ctx.BeginAttract();
            InitializeSubState();
        }

        public override void UpdateState()
        {
            bool latched = Ctx.TickAttract();
            CheckSwitchStatesInternal(latched);
        }

        public override void ExitState() { }

        // Attract needs the latch result, so the real decision lives here.
        private void CheckSwitchStatesInternal(bool latched)
        {
            if (latched)
            {
                SwitchState(Factory.Hold());
            }
            else if (InputManager.Instance.gloveTap)
            {
                // Cancel: throw the incoming object out early.
                SwitchState(Factory.ReleaseObject());
            }
            else if (Ctx.KatanaEquipped && InputManager.Instance.attackIsPressed && Ctx.KatanaLaunchInRange())
            {
                // Katana synergy: slash launches the object early — but only once it's pulled in close,
                // so you can't spam attract-from-afar + katana to fling objects across the map.
                SwitchState(Factory.ReleaseObject());
            }
            else if (!Ctx.HasAttractedObject)
            {
                // Nothing was actually caught — go back to idle.
                SwitchState(Factory.Idle());
            }
        }

        public override void CheckSwitchStates() { }

        public override void InitializeSubState() { }
    }
}
