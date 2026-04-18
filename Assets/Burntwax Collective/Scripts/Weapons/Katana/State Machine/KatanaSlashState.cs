namespace Burntwax
{
    public class KatanaSlashState : KatanaBaseState
    {

        public KatanaSlashState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;

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