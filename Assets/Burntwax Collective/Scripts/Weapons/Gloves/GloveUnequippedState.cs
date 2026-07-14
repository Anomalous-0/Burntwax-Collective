using UnityEngine;

namespace Burntwax
{
    public class GloveUnequippedState : GloveBaseState
    {

        public GloveUnequippedState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
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
            
            if(InputManager.Instance.useGloves)
            {
                
                SwitchState(Factory.Equipped());
            }


        }


        public override void InitializeSubState()
        {
            

        }

    }
}
