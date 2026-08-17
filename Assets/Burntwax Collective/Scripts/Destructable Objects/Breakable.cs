using System;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Reusable health and break-rule owner for damageable environment objects.
    /// It keeps the legacy Destructable contract so existing saving remains compatible.
    /// </summary>
    [DisallowMultipleComponent]
    public class Breakable : MonoBehaviour, Destructable, IDamageable
    {
        [Header("Health")]
        [SerializeField, Min(0f)] private float maxHealth = 15f;
        [SerializeField, Min(0f)] private float currentHealth = 15f;

        [Header("Break Rules")]
        [Tooltip("Damage categories allowed to affect this object. Combined incoming flags match when any allowed flag overlaps.")]
        [SerializeField] private DamageType acceptedDamageTypes = (DamageType)(-1);

        [Tooltip("Allowed damage categories that break this object immediately, regardless of remaining health.")]
        [SerializeField] private DamageType instantBreakDamageTypes = DamageType.None;

        [Tooltip("Individual hits below this amount are ignored.")]
        [SerializeField, Min(0f)] private float minimumDamagePerHit;

        [Header("After Breaking")]
        [Tooltip("Destroys the intact root after all attached break actions have run. Disable this for mesh-swap behaviors.")]
        [SerializeField] private bool destroyObjectOnBreak = true;

        public event Action<DamageInfo> Broken;

        public float MaxHealth => maxHealth;
        public bool IsBroken { get; private set; }

        public float CurrentHealth
        {
            get => currentHealth;
            set => currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        }

        public virtual void ApplyDamage(DamageInfo damage)
        {
            if (IsBroken
                || damage.Amount <= 0f
                || damage.Amount < minimumDamagePerHit
                || !Accepts(damage.Type))
            {
                return;
            }

            bool breaksImmediately = damage.Type != DamageType.None
                && (instantBreakDamageTypes & damage.Type) != 0;

            if (breaksImmediately)
            {
                currentHealth = 0f;
                Break(damage);
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            if (currentHealth <= 0f)
                Break(damage);
        }

        public void TakeDamage(float damage)
        {
            if (IsBroken || damage <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            if (currentHealth <= 0f)
            {
                Break(new DamageInfo(
                    damage,
                    DamageType.None,
                    transform.position,
                    Vector3.zero,
                    Vector3.zero,
                    null,
                    null));
            }
        }

        public void Destruct(Vector3 position)
        {
            if (IsBroken)
                return;

            currentHealth = 0f;
            Break(new DamageInfo(
                0f,
                DamageType.None,
                position,
                Vector3.zero,
                Vector3.zero,
                null,
                null));
        }

        public void Break(DamageInfo cause)
        {
            if (IsBroken)
                return;

            IsBroken = true;
            currentHealth = 0f;

            OnBreak(cause);

            Broken?.Invoke(cause);

            if (destroyObjectOnBreak)
                Destroy(gameObject);
        }

        /// <summary>
        /// Specialized breakables override this to perform their unique behavior
        /// before the intact root is optionally destroyed.
        /// </summary>
        protected virtual void OnBreak(DamageInfo cause)
        {
        }

        private bool Accepts(DamageType damageType)
        {
            return damageType != DamageType.None
                && (acceptedDamageTypes & damageType) != 0;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(0f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            minimumDamagePerHit = Mathf.Max(0f, minimumDamagePerHit);
        }
    }
}
