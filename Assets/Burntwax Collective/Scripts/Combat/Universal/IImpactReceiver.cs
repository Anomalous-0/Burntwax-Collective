namespace Burntwax
{
    /// <summary>
    /// Optional override for targets that need a custom physical hit reaction,
    /// such as an animated enemy, stagger system, or ragdoll controller.
    /// </summary>
    public interface IImpactReceiver
    {
        void ReceiveImpact(DamageInfo impact);
    }
}
