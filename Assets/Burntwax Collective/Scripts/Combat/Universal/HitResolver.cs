using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Shared entry point for a physical hit. It coordinates presentation and
    /// gameplay while keeping those systems independent from each damage source.
    /// A target defense receives the first response so a block may weaken the
    /// hit and a perfect parry may cancel it before normal hit resolution.
    /// </summary>
    public static class HitResolver
    {
        public static bool ResolveHit(
            Collider hitCollider,
            DamageInfo damage,
            bool playImpactAudio = true,
            bool playSurfaceImpact = true)
        {
            return ResolveHit(
                hitCollider,
                damage,
                out _,
                playImpactAudio,
                playSurfaceImpact);
        }

        public static bool ResolveHit(
            Collider hitCollider,
            DamageInfo damage,
            out HitDefenseResult defenseResult,
            bool playImpactAudio = true,
            bool playSurfaceImpact = true)
        {
            defenseResult = HitDefenseResult.Pass(damage);

            if (hitCollider == null)
                return false;

            IHitDefense defense =
                hitCollider.GetComponentInParent<IHitDefense>();
            if (defense != null)
            {
                defenseResult = defense.EvaluateHit(
                    hitCollider,
                    damage);

                if (defenseResult.Outcome == HitDefenseOutcome.Parried)
                    NotifyParriedAttackSource(damage, hitCollider);

                if (defenseResult.CancelsHit)
                    return false;

                damage = defenseResult.ResolvedHit;
            }

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

        private static void NotifyParriedAttackSource(
            DamageInfo parriedHit,
            Collider defenderCollider)
        {
            IParryReactionReceiver receiver =
                FindParryReactionReceiver(parriedHit.Source);

            if (receiver == null)
            {
                receiver = FindParryReactionReceiver(
                    parriedHit.Instigator);
            }

            receiver?.OnAttackParried(parriedHit, defenderCollider);
        }

        private static IParryReactionReceiver FindParryReactionReceiver(
            GameObject candidate)
        {
            return candidate != null
                ? candidate.GetComponentInParent<IParryReactionReceiver>()
                : null;
        }
    }
}
