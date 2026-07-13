namespace Burntwax
{
    public class GloveEMState : GloveBaseState
    {

        public GloveEMState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        { }

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
