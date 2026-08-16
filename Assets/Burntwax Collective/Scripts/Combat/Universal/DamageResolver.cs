using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Shared entry point for sending a hit to a damageable target.
    /// Future defense, difficulty, and resistance rules can be added here
    /// without changing every damage source.
    /// </summary>
    public static class DamageResolver
    {
        public static bool TryApplyDamage(Collider hitCollider, DamageInfo damage)
        {
            if (hitCollider == null)
                return false;

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null)
                return false;

            damageable.ApplyDamage(damage);
            return true;
        }
    }
}
