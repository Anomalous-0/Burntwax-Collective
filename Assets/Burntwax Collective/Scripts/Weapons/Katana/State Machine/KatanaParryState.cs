using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Burntwax
{
    public class KatanaParryState : KatanaBaseState
    {

        public KatanaParryState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {
            Debug.Log("Entered Parry State");

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