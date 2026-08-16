using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Shared entry point for a physical hit. It coordinates presentation and
    /// gameplay while keeping those systems independent from each damage source.
    /// Future parry and defense checks can be inserted here before damage is applied.
    /// </summary>
    public static class HitResolver
    {
        public static bool ResolveHit(
            Collider hitCollider,
            DamageInfo damage,
            bool playImpactAudio = true,
            bool playSurfaceImpact = true)
        {
            if (hitCollider == null)
                return false;

            if (playSurfaceImpact)
            {
                SurfaceImpactSystem.Instance?.PlayImpact(
                    hitCollider,
                    damage,
                    playImpactAudio);
            }

            ImpactResolver.TryApplyImpact(hitCollider, damage);

            return DamageResolver.TryApplyDamage(hitCollider, damage);
        }
    }
}
