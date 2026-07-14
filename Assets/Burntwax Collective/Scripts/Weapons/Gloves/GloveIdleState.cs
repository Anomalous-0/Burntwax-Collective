namespace Burntwax
{
    // Palm-up resting pose and the single branch point. Runs target detection (and cyan-tints the
    // primary) every frame; double-tap starts the attract branch, hold starts the charge branch.
    public class GloveIdleState : GloveBaseState
    {
        public GloveIdleState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            Ctx.PlayGloveAnimation(Ctx.idleHash);
            InitializeSubState();
        }

        public override void UpdateState()
        {
            // The Equipped root may unequip us earlier in this same frame (root UpdateState runs
            // before this substate). If that happened, don't re-detect / re-tint after unequip.
            if (InputManager.Instance == null || !InputManager.Instance.gloveEquipped)
                return;


            CheckSwitchStates();
        }

        public override void ExitState()
        {
            // Detection tint is handed off (Attract snapshots it) or cleared by the next state /
            // SetEquipped(false); nothing to undo here.
        }

        public override void CheckSwitchStates()
        {
            // Branch A: attract (needs a valid target in reach).
            if (InputManager.Instance.gloveDoubleTap && Ctx.CanAttractObject)
            {
                SwitchState(Factory.Attract());
            }
            // Branch B: charge pulse.
            else if (InputManager.Instance.gloveCharging)
            {
                SwitchState(Factory.ChargeBlast());
            }
            // (A plain tap here is consumed by the Equipped root as "unequip".)
        }

        public override void InitializeSubState() { }
    }
}
