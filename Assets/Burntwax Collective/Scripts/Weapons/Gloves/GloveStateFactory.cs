using System.Collections.Generic;
using Burntwax;
namespace Burntwax
{
    public enum GloveState
    {
        Unequipped,
        Unequipping,
        Equipped,
        Equipping,
        Idle,
        ReadyAttract,
        Attract,
        Hold,
        ReleaseObject,
        ChargeBlast,
        Blast,

        BlastProjectile
    }

    public class GloveStateFactory
    {

        GloveStateMachine context;
        Dictionary<GloveState, GloveBaseState> states = new Dictionary<GloveState, GloveBaseState>();

        public GloveStateFactory(GloveStateMachine currentContext)
        {
            context = currentContext;
            states[GloveState.Unequipped] = new GloveUnequippedState(context, this);
            states[GloveState.Unequipping] = new GloveUnequippingState(context, this);
            states[GloveState.Equipped] = new GloveEquippedState(context, this);
            states[GloveState.Equipping] = new GloveEquippingState(context, this);
            states[GloveState.ReadyAttract] = new GloveReadyAttractState(context, this);
            states[GloveState.Attract] = new GloveAttractState(context, this);
            states[GloveState.Idle] = new GloveIdleState(context, this);
            states[GloveState.Hold] = new GloveHoldState(context, this);
            states[GloveState.ReleaseObject] = new GloveReleaseObjectState(context, this);
            states[GloveState.ChargeBlast] = new GloveChargeBlastState(context, this);
            states[GloveState.Blast] = new GloveBlastState(context, this);
            states[GloveState.BlastProjectile] = new GloveBlastProjectileState(context, this);


        }

        public GloveBaseState Unequipped()
        {
            return states[GloveState.Unequipped];
        }

        public GloveBaseState Unequipping()
        {
            return states[GloveState.Unequipping];
        }

        public GloveBaseState Equipped()
        {
            return states[GloveState.Equipped];
        }

        public GloveBaseState Equipping()
        {
            return states[GloveState.Equipping];
        }

        public GloveBaseState Idle()
        {
            return states[GloveState.Idle];
        }
        public GloveBaseState ReadyAttract()
        {
            return states[GloveState.ReadyAttract];
        }

        public GloveBaseState Attract()
        {
            return states[GloveState.Attract];
        }

        public GloveBaseState Hold()
        {
            return states[GloveState.Hold];
        }

        public GloveBaseState ReleaseObject()
        {
            return states[GloveState.ReleaseObject];
        }

        public GloveBaseState ChargeBlast()
        {
            return states[GloveState.ChargeBlast];
        }

        public GloveBaseState Blast()
        {
            return states[GloveState.Blast];
        }

        public GloveBaseState BlastProjectile()
        {
            return states[GloveState.BlastProjectile];
        }


    }

}