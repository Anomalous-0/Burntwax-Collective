using System.Collections.Generic;
using Burntwax;
namespace Burntwax
{
    enum KatanaStates
    {
        idle,
        ready,
        slash,
        parry,
        stow


    }

    public class KatanaStateFactory
    {

        KatanaStateMachine context;
        Dictionary<KatanaStates, KatanaBaseState> states = new Dictionary<KatanaStates, KatanaBaseState>();

        public KatanaStateFactory(KatanaStateMachine currentContext)
        {
            context = currentContext;
            states[KatanaStates.idle] = new KatanaIdleState(context, this);
            states[KatanaStates.slash] = new KatanaSlashState(context, this);
            states[KatanaStates.parry] = new KatanaParryState(context, this);
            states[KatanaStates.stow] = new KatanaStowState(context, this);
            states[KatanaStates.ready] = new KatanaReadyState(context, this); 
        }

        public KatanaBaseState Idle()
        {
            return states[KatanaStates.idle];
        }

        public KatanaBaseState Ready()
        {
            return states[KatanaStates.ready];
        }

        public KatanaBaseState Slash()
        {
            return states[KatanaStates.slash];
        }

        public KatanaBaseState Parry()
        {
            return states[KatanaStates.parry];
        }

        public KatanaBaseState Stow()
        {
            return states[KatanaStates.stow];
        }
    }

}