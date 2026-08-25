using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Result of giving a target's active defense the first response to a hit.
    /// Passed continues through normal impact and damage resolution.
    /// </summary>
    public enum HitDefenseOutcome
    {
        Passed = 0,
        Blocked = 1,
        Parried = 2,
        GuardBroken = 3
    }

    public readonly struct HitDefenseResult
    {
        public HitDefenseOutcome Outcome { get; }
        public DamageInfo ResolvedHit { get; }
        public float CollisionImpulseMultiplier { get; }
        public float SourceReboundSpeedMultiplier { get; }
        public bool SourceMotionHandled { get; }
        public bool CancelsHit => Outcome == HitDefenseOutcome.Parried;

        private HitDefenseResult(
            HitDefenseOutcome outcome,
            DamageInfo resolvedHit,
            float collisionImpulseMultiplier,
            float sourceReboundSpeedMultiplier,
            bool sourceMotionHandled)
        {
            Outcome = outcome;
            ResolvedHit = resolvedHit;
            CollisionImpulseMultiplier = Mathf.Clamp01(
                collisionImpulseMultiplier);
            SourceReboundSpeedMultiplier = Mathf.Max(
                0f,
                sourceReboundSpeedMultiplier);
            SourceMotionHandled = sourceMotionHandled;
        }

        public static HitDefenseResult Pass(DamageInfo incomingHit) =>
            new HitDefenseResult(
                HitDefenseOutcome.Passed,
                incomingHit,
                1f,
                0f,
                false);

        public static HitDefenseResult Block(
            DamageInfo reducedHit,
            float collisionImpulseMultiplier = 1f,
            float sourceReboundSpeedMultiplier = 0f) =>
            new HitDefenseResult(
                HitDefenseOutcome.Blocked,
                reducedHit,
                collisionImpulseMultiplier,
                sourceReboundSpeedMultiplier,
                false);

        public static HitDefenseResult Parry(
            DamageInfo incomingHit,
            float collisionImpulseMultiplier = 1f,
            float sourceReboundSpeedMultiplier = 0f,
            bool sourceMotionHandled = false) =>
            new HitDefenseResult(
                HitDefenseOutcome.Parried,
                incomingHit,
                collisionImpulseMultiplier,
                sourceReboundSpeedMultiplier,
                sourceMotionHandled);

        public static HitDefenseResult GuardBreak(DamageInfo incomingHit) =>
            new HitDefenseResult(
                HitDefenseOutcome.GuardBroken,
                incomingHit,
                1f,
                0f,
                false);
    }

    /// <summary>
    /// Implemented by an actor or object that can actively block or parry hits.
    /// Different player, human, and alien defenses can implement their own rules
    /// without requiring attack sources to know the defender type.
    /// </summary>
    public interface IHitDefense
    {
        HitDefenseResult EvaluateHit(
            Collider hitCollider,
            DamageInfo incomingHit);
    }
}
