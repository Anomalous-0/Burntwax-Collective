namespace Burntwax
{
    /// <summary>
    /// Implemented by anything that can receive damage.
    /// </summary>
    public interface IDamageable
    {
        void ApplyDamage(DamageInfo damage);
    }
}
