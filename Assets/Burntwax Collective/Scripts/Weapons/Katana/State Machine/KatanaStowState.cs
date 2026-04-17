using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Burntwax
{
    public class KatanaStowState : KatanaBaseState
    {

        public KatanaStowState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {
            Debug.Log("Entered Stow State");

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