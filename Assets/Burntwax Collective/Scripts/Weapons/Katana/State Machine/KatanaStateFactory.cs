using System.Collections.Generic;
using Burntwax;
namespace Burntwax
{
    enum KatanaStates
    {
        idle,
        ready,
        heavyCharge,
        lightSlash,
        heavySlash,
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
            states[KatanaStates.heavyCharge] = new KatanaHeavyChargeState(context, this);
            states[KatanaStates.lightSlash] = new KatanaLightSlashState(context, this);
            states[KatanaStates.heavySlash] = new KatanaHeavySlashState(context, this);
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

        public KatanaBaseState LightSlash()
        {
            return states[KatanaStates.lightSlash];
        }

        public KatanaBaseState HeavyCharge()
        {
            return states[KatanaStates.heavyCharge];
        }

        public KatanaBaseState HeavySlash()
        {
            return states[KatanaStates.heavySlash];
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
