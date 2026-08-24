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
        public bool CancelsHit => Outcome == HitDefenseOutcome.Parried;

        private HitDefenseResult(
            HitDefenseOutcome outcome,
            DamageInfo resolvedHit)
        {
            Outcome = outcome;
            ResolvedHit = resolvedHit;
        }

        public static HitDefenseResult Pass(DamageInfo incomingHit) =>
            new HitDefenseResult(
                HitDefenseOutcome.Passed,
                incomingHit);

        public static HitDefenseResult Block(DamageInfo reducedHit) =>
            new HitDefenseResult(
                HitDefenseOutcome.Blocked,
                reducedHit);

        public static HitDefenseResult Parry(DamageInfo incomingHit) =>
            new HitDefenseResult(
                HitDefenseOutcome.Parried,
                incomingHit);

        public static HitDefenseResult GuardBreak(DamageInfo incomingHit) =>
            new HitDefenseResult(
                HitDefenseOutcome.GuardBroken,
                incomingHit);
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
