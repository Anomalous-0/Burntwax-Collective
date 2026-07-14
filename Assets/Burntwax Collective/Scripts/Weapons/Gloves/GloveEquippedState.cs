namespace Burntwax
{
    // Root state while the glove is out. Idle is the hub substate; the two ability branches
    // (attract / charge) live under it. This root only owns equip publishing and the unequip tap.
    public class GloveEquippedState : GloveBaseState
    {
        public GloveEquippedState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            Ctx.SetEquipped(true);
            InitializeSubState();
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            // Unequip only from the resting palm-up Idle (so a tap that means "launch the held
            // object" isn't misread as "put the glove away"). Other substates handle their own taps.
            if (CurrentSubstate() == Ctx.states.Idle() && InputManager.Instance.gloveTap)
            {
                SwitchState(Factory.Unequipped());
            }
        }

        public override void InitializeSubState()
        {
            GloveBaseState idle = Factory.Idle();
            SetSubState(idle);
            idle.EnterState();
        }
    }
}
