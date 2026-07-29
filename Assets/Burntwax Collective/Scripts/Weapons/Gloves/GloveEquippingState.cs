namespace Burntwax
{
    // The equip action: the glove is raising into view. Runs while the arm-override layer weight
    // climbs, then hands off to Idle once the glove is fully up. Short-lived by design, but it owns
    // the raise so "equipping" is never folded into the Idle hub.
    public class GloveEquippingState : GloveBaseState
    {
        public GloveEquippingState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
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
            CheckSwitchStates();
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (Ctx.GloveRaised)
                SwitchState(Factory.Idle());
        }

        public override void InitializeSubState() { }
    }
}
