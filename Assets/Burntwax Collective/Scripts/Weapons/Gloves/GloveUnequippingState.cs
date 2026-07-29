namespace Burntwax
{
    // The unequip action: the glove is lowering out of view. A root state (it swaps the whole mode
    // out), it flags the glove down so the arm-override layer weight falls, then hands off to the
    // fully-stowed Unequipped state once the glove is all the way down.
    public class GloveUnequippingState : GloveBaseState
    {
        public GloveUnequippingState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            Ctx.SetEquipped(false);
            InitializeSubState();
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (Ctx.GloveLowered)
                SwitchState(Factory.Unequipped());
        }

        public override void InitializeSubState() { }
    }
}
