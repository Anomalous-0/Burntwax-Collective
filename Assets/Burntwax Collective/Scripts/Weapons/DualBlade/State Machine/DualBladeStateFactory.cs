using System.Collections.Generic;
using Burntwax;
namespace Burntwax
{
    enum DualBladeStates
    {
        idle,
        ready,
        heavyCharge,
        lightSlash,
        heavySlash,
        parry,
        parriedRecoil,
        stow


    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaStateFactory")]
    public class DualBladeStateFactory
    {

        DualBladeStateMachine context;
        Dictionary<DualBladeStates, DualBladeBaseState> states = new Dictionary<DualBladeStates, DualBladeBaseState>();

        public DualBladeStateFactory(DualBladeStateMachine currentContext)
        {
            context = currentContext;
            states[DualBladeStates.idle] = new DualBladeIdleState(context, this);
            states[DualBladeStates.heavyCharge] = new DualBladeHeavyChargeState(context, this);
            states[DualBladeStates.lightSlash] = new DualBladeLightSlashState(context, this);
            states[DualBladeStates.heavySlash] = new DualBladeHeavySlashState(context, this);
            states[DualBladeStates.parry] = new DualBladeParryState(context, this);
            states[DualBladeStates.parriedRecoil] =
                new DualBladeParriedRecoilState(context, this);
            states[DualBladeStates.stow] = new DualBladeStowState(context, this);
            states[DualBladeStates.ready] = new DualBladeReadyState(context, this); 
        }

        public DualBladeBaseState Idle()
        {
            return states[DualBladeStates.idle];
        }

        public DualBladeBaseState Ready()
        {
            return states[DualBladeStates.ready];
        }

        public DualBladeBaseState LightSlash()
        {
            return states[DualBladeStates.lightSlash];
        }

        public DualBladeBaseState HeavyCharge()
        {
            return states[DualBladeStates.heavyCharge];
        }

        public DualBladeBaseState HeavySlash()
        {
            return states[DualBladeStates.heavySlash];
        }

        public DualBladeBaseState Parry()
        {
            return states[DualBladeStates.parry];
        }

        public DualBladeBaseState ParriedRecoil()
        {
            return states[DualBladeStates.parriedRecoil];
        }

        public DualBladeBaseState Stow()
        {
            return states[DualBladeStates.stow];
        }
    }

}
