using System.Collections.Generic;

namespace Burntwax
{
    internal enum EnemyStates
    {
        Alive,
        Dead,
        Grounded,
        Stationary,
        Unaware,
        Idle,
        Combat,
        FaceTarget,
        Melee,
        Throw,
        Defense,
        Staggered
    }

    /// <summary>
    /// Creates and caches the enemy's state instances exactly once, following
    /// the same pattern as PlayerStateFactory. State Enter methods must reset any
    /// per-entry data because a cached state can be entered repeatedly.
    /// </summary>
    public sealed class EnemyStateFactory
    {
        private readonly Dictionary<EnemyStates, EnemyBaseState> states = new();

        public EnemyStateFactory(EnemyStateMachine context)
        {
            states[EnemyStates.Alive] = new EnemyAliveState(context, this);
            states[EnemyStates.Dead] = new EnemyDeadState(context, this);
            states[EnemyStates.Grounded] = new EnemyGroundedState(context, this);
            states[EnemyStates.Stationary] = new EnemyStationaryState(context, this);
            states[EnemyStates.Unaware] = new EnemyUnawareState(context, this);
            states[EnemyStates.Idle] = new EnemyIdleState(context, this);
            states[EnemyStates.Combat] = new EnemyCombatState(context, this);
            states[EnemyStates.FaceTarget] = new EnemyFaceTargetState(context, this);
            states[EnemyStates.Melee] = new EnemyMeleeState(context, this);
            states[EnemyStates.Throw] = new EnemyThrowState(context, this);
            states[EnemyStates.Defense] = new EnemyDefenseState(context, this);
            states[EnemyStates.Staggered] = new EnemyStaggeredState(context, this);
        }

        public EnemyBaseState Alive() => states[EnemyStates.Alive];
        public EnemyBaseState Dead() => states[EnemyStates.Dead];
        public EnemyBaseState Grounded() => states[EnemyStates.Grounded];
        public EnemyBaseState Stationary() => states[EnemyStates.Stationary];
        public EnemyBaseState Unaware() => states[EnemyStates.Unaware];
        public EnemyBaseState Idle() => states[EnemyStates.Idle];
        public EnemyBaseState Combat() => states[EnemyStates.Combat];
        public EnemyBaseState FaceTarget() => states[EnemyStates.FaceTarget];
        public EnemyMeleeState Melee() =>
            (EnemyMeleeState)states[EnemyStates.Melee];
        public EnemyThrowState Throw() =>
            (EnemyThrowState)states[EnemyStates.Throw];
        public EnemyDefenseState Defense() =>
            (EnemyDefenseState)states[EnemyStates.Defense];
        public EnemyBaseState Staggered() => states[EnemyStates.Staggered];
    }
}
