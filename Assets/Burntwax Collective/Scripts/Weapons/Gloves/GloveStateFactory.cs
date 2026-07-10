using System.Collections.Generic;
using Burntwax;
namespace Burntwax
{
    public enum GloveState
    {
        Unequipped,
        Equipped,
        Idle,
        ChargeAttract,
        Attract,
        Hold,
        ChargeBlast,
        Blast,
    }

    public class GloveStateFactory
    {

        GloveStateMachine context;
        Dictionary<GloveState, GloveBaseState> states = new Dictionary<GloveState, GloveBaseState>();

        public GloveStateFactory(GloveStateMachine currentContext)
        {
            context = currentContext;
            states[GloveState.Unequipped] = new GloveUnequippedState(context, this);
            states[GloveState.Equipped] = new GloveEquippedState(context, this);
            states[GloveState.ChargeAttract] = new GloveChargeAttractState(context, this);
            states[GloveState.Attract] = new GloveAttractState(context, this);
            states[GloveState.Idle] = new GloveIdleState(context, this);
            states[GloveState.Hold] = new GloveHoldState(context, this);
            states[GloveState.ChargeBlast] = new GloveChargeBlastState(context, this);
            states[GloveState.Blast] = new GloveBlastState(context, this);


        }

        public GloveBaseState Unequipped()
        {
            return states[GloveState.Unequipped];
        }

        public GloveBaseState Equipped()
        {
            return states[GloveState.Equipped];
        }

        public GloveBaseState Idle()
        {
            return states[GloveState.Idle];
        }
        public GloveBaseState ChargeAttract()
        {
            return states[GloveState.ChargeAttract];
        }

        public GloveBaseState Attract()
        {
            return states[GloveState.Attract];
        }

        public GloveBaseState Hold()
        {
            return states[GloveState.Hold];
        }

        public GloveBaseState ChargeBlast()
        {
            return states[GloveState.ChargeBlast];
        }

        public GloveBaseState Blast()
        {
            return states[GloveState.Blast];
        }

    }

}