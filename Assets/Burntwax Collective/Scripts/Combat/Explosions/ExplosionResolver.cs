using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Resolves one explosion through the shared hit pipeline. The explosion
    /// source only supplies a location and config; targets decide how they react.
    /// </summary>
    public static class ExplosionResolver
    {
        private const float DirectionEpsilon = 0.0001f;
        private const float LineOfSightPadding = 0.05f;

        public static int Explode(
            Vector3 center,
            ExplosionConfig config,
            GameObject source,
            GameObject instigator = null)
        {
            if (config == null)
            {
                Debug.LogWarning("An explosion was requested without an ExplosionConfig.");
                return 0;
            }

            PlayPresentation(center, config);

            Collider[] colliders = Physics.OverlapSphere(
                center,
                config.Radius,
                config.AffectedLayers,
                QueryTriggerInteraction.Ignore);

            HashSet<EntityId> resolvedTargets = new HashSet<EntityId>();
            int resolvedTargetCount = 0;

            foreach (Collider hitCollider in colliders)
            {
                if (hitCollider == null || IsPartOfSource(hitCollider, source))
                    continue;

                GameObject target = GetLogicalTarget(hitCollider);
                if (target == null || resolvedTargets.Contains(target.GetEntityId()))
                    continue;

                Vector3 hitPoint = hitCollider.ClosestPoint(center);
                Vector3 fromCenter = hitPoint - center;

                if (fromCenter.sqrMagnitude <= DirectionEpsilon)
                {
                    hitPoint = hitCollider.bounds.center;
                    fromCenter = hitPoint - center;
                }

                float distance = fromCenter.magnitude;
                Vector3 outwardDirection = distance > DirectionEpsilon
                    ? fromCenter / distance
                    : Vector3.up;

                if (config.RequireLineOfSight
                    && !HasLineOfSight(
                        center,
                        outwardDirection,
                        distance,
                        target,
                        source,
                        config.ObstructionLayers))
                {
                    continue;
                }

                resolvedTargets.Add(target.GetEntityId());

                float normalizedDistance = Mathf.Clamp01(
                    distance / config.Radius);
                float damageAmount = config.EvaluateDamage(normalizedDistance);
                float impulseAmount = config.EvaluateImpulse(normalizedDistance);

                Vector3 impulseDirection = (
                    outwardDirection + Vector3.up * config.UpwardBias).normalized;

                DamageInfo damage = new DamageInfo(
                    damageAmount,
                    DamageType.Explosion,
                    hitPoint,
                    -outwardDirection,
                    outwardDirection,
                    source,
                    instigator,
                    impulseDirection * impulseAmount,
                    DamageType.Explosion);

                // The explosion has one central presentation. Per-target surface
                // effects are intentionally disabled to avoid spawning bullet-like
                // impacts on every collider inside the blast.
                HitResolver.ResolveHit(
                    hitCollider,
                    damage,
                    playImpactAudio: false,
                    playSurfaceImpact: false);

                resolvedTargetCount++;
            }

            return resolvedTargetCount;
        }

        private static void PlayPresentation(
            Vector3 center,
            ExplosionConfig config)
        {
            if (config.VfxPrefab != null)
            {
                GameObject vfx = UnityEngine.Object.Instantiate(
                    config.VfxPrefab,
                    center,
                    Quaternion.identity);

                if (config.VfxLifetime > 0f)
                    UnityEngine.Object.Destroy(vfx, config.VfxLifetime);
            }

            AudioClip clip = GetRandomClip(config.ExplosionSounds);
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, center, config.Volume);
        }

        private static AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            int startIndex = UnityEngine.Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                AudioClip clip = clips[(startIndex + offset) % clips.Length];
                if (clip != null)
                    return clip;
            }

            return null;
        }

        private static bool HasLineOfSight(
            Vector3 center,
            Vector3 direction,
            float distance,
            GameObject target,
            GameObject source,
            LayerMask obstructionLayers)
        {
            if (distance <= DirectionEpsilon)
                return true;

            RaycastHit[] hits = Physics.RaycastAll(
                center,
                direction,
                distance + LineOfSightPadding,
                obstructionLayers,
                QueryTriggerInteraction.Ignore);

            Array.Sort(hits, (left, right) =>
                left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || IsPartOfSource(hit.collider, source))
                    continue;

                return GetLogicalTarget(hit.collider) == target;
            }

            return true;
        }

        private static GameObject GetLogicalTarget(Collider hitCollider)
        {
            IDamageable damageable =
                hitCollider.GetComponentInParent<IDamageable>();
            if (damageable is Component damageableComponent)
                return damageableComponent.gameObject;

            Rigidbody body = hitCollider.attachedRigidbody;
            return body != null ? body.gameObject : null;
        }

        private static bool IsPartOfSource(
            Collider hitCollider,
            GameObject source)
        {
            return source != null
                && (hitCollider.gameObject == source
                    || hitCollider.transform.IsChildOf(source.transform));
        }
    }
}
