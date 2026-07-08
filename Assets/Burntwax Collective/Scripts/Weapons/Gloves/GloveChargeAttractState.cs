namespace Burntwax
{
    public class GloveChargeAttractState : GloveBaseState
    {

        public GloveChargeAttractState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
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

        public override void ExitState()
        {
           
        }

        public override void CheckSwitchStates()
        {
            


        }


        public override void InitializeSubState()
        {
           

        }

    }
}
