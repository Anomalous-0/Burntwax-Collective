namespace Burntwax
{
    public class GloveEquippedState : GloveBaseState
    {

        public GloveEquippedState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
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
            if(InputManager.Instance.useGloves && Ctx.AttractObject)
            {
                SetSubState(Factory.ChargeAttract());
            }
            else if(InputManager.Instance.useGloves && !Ctx.AttractObject)
            {
                SetSubState(Factory.ChargeBlast());
            }
            else
            {
                SetSubState(Factory.Hold());
            }

        }

    }
}
