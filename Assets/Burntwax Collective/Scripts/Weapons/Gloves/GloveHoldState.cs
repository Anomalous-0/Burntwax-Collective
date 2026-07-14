namespace Burntwax
{
    // Branch A, step 2: the primary object orbits in the hand until a tap (or katana slash) throws
    // it out. Ignores charge input — the two ability branches never overlap.
    public class GloveHoldState : GloveBaseState
    {
        public GloveHoldState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            
            InitializeSubState();
        }

        public override void UpdateState()
        {
           
            CheckSwitchStates();
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (InputManager.Instance.gloveTap)
            {
                SwitchState(Factory.ReleaseObject());
            }
            
        }

        public override void InitializeSubState() { }
    }
}
