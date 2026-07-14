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
           
            InitializeSubState();
        }

        public override void UpdateState()
        {
       
          
        }

        public override void ExitState() { }

        

        public override void CheckSwitchStates() { }

        public override void InitializeSubState() { }
    }
}
