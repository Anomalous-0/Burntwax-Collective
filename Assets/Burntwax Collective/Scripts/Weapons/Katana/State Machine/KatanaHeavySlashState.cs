namespace Burntwax
{
    /// <summary>
    /// Executes an already-recognized heavy release. It currently shares the
    /// directional slash animation with light attacks while applying heavy combat data.
    /// </summary>
    public sealed class KatanaHeavySlashState : KatanaSlashState
    {
        protected override KatanaAttackStrength AttackStrength =>
            KatanaAttackStrength.Heavy;

        public KatanaHeavySlashState(
            KatanaStateMachine currentContext,
            KatanaStateFactory katanaStateFactory)
            : base(currentContext, katanaStateFactory)
        {
        }
    }
}
