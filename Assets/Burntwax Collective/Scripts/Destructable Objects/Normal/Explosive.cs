using System.Collections;
using UnityEngine;

namespace Burntwax
{
    public enum ExplosiveState
    {
        Stable,
        Primed,
        Exploded
    }

    /// <summary>
    /// One-component explosive specialization. It inherits health and break
    /// rules from Breakable, then creates an explosion when it breaks.
    /// </summary>
    [DisallowMultipleComponent]
    public class Explosive : Breakable
    {
        [Header("Explosion")]
        [SerializeField] private ExplosionConfig explosionConfig;

        [Tooltip("Optional position used as the explosion center. Uses this object's position when unassigned.")]
        [SerializeField] private Transform explosionOrigin;

        [Header("Primed Fuse")]
        [Tooltip("Seconds a primed explosive burns before it explodes automatically.")]
        [SerializeField, Min(0f)] private float fuseDuration = 2f;

        [Tooltip("Optional fire effect spawned while the explosive is primed.")]
        [SerializeField] private GameObject fuseVfxPrefab;

        [Tooltip("Optional fire-effect attachment point. Uses this object's transform when unassigned.")]
        [SerializeField] private Transform fuseVfxOrigin;

        public ExplosiveState State { get; private set; } = ExplosiveState.Stable;

        private DamageInfo primingCause;
        private Coroutine fuseRoutine;
        private GameObject activeFuseVfx;

        public override void ApplyDamage(DamageInfo damage)
        {
            if (IsBroken)
                return;

            // The first explosion that reaches a stable explosive starts a delayed
            // chain reaction instead of destroying it through blast damage alone.
            if (State == ExplosiveState.Stable
                && (damage.Type & DamageType.Explosion) != 0)
            {
                Prime(damage);
                return;
            }

            float healthBeforeDamage = CurrentHealth;
            base.ApplyDamage(damage);

            if (IsBroken || State != ExplosiveState.Stable)
                return;

            bool receivedDamage = CurrentHealth < healthBeforeDamage;
            bool oneComparableHitRemaining = CurrentHealth <= damage.Amount;

            if (receivedDamage && oneComparableHitRemaining)
                Prime(damage);
        }

        private void Prime(DamageInfo cause)
        {
            if (IsBroken || State != ExplosiveState.Stable)
                return;

            State = ExplosiveState.Primed;
            primingCause = cause;
            SpawnFuseVfx();

            if (fuseDuration <= 0f)
            {
                Break(primingCause);
                return;
            }

            fuseRoutine = StartCoroutine(FuseCountdown());
        }

        private IEnumerator FuseCountdown()
        {
            yield return new WaitForSeconds(fuseDuration);

            fuseRoutine = null;
            if (!IsBroken)
                Break(primingCause);
        }

        private void SpawnFuseVfx()
        {
            if (fuseVfxPrefab == null)
                return;

            Transform origin = fuseVfxOrigin != null
                ? fuseVfxOrigin
                : transform;

            activeFuseVfx = Instantiate(
                fuseVfxPrefab,
                origin.position,
                origin.rotation,
                origin);
        }

        protected override void OnBreak(DamageInfo cause)
        {
            State = ExplosiveState.Exploded;

            if (fuseRoutine != null)
            {
                StopCoroutine(fuseRoutine);
                fuseRoutine = null;
            }

            if (activeFuseVfx != null)
            {
                Destroy(activeFuseVfx);
                activeFuseVfx = null;
            }

            Vector3 center = explosionOrigin != null
                ? explosionOrigin.position
                : transform.position;

            ExplosionResolver.Explode(
                center,
                explosionConfig,
                gameObject,
                cause.Instigator);
        }

        private void OnDrawGizmosSelected()
        {
            if (explosionConfig == null)
                return;

            Vector3 center = explosionOrigin != null
                ? explosionOrigin.position
                : transform.position;

            Color previousColor = Gizmos.color;
            Gizmos.color = new Color(1f, 0.35f, 0f, 0.8f);
            Gizmos.DrawWireSphere(center, explosionConfig.Radius);
            Gizmos.color = previousColor;
        }

        private void OnValidate()
        {
            fuseDuration = Mathf.Max(0f, fuseDuration);
        }
    }
}
