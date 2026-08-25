using System;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Owns an enemy actor's shared health. Damage sources only communicate
    /// through IDamageable; separate components can subscribe to the events to
    /// drive animation, AI reactions, ragdolls, loot, or future slicing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField, Min(0.01f)] private float maxHealth = 100f;

        [Header("Runtime (read-only)")]
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isDead;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public DamageInfo LastDamage { get; private set; }

        private void Awake()
        {
            ResetHealth();
        }

        public void ApplyDamage(DamageInfo damage)
        {
            if (isDead || damage.Amount <= 0f)
                return;

            LastDamage = damage;
            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            Damaged?.Invoke(damage);

            if (currentHealth > 0f)
                return;

            isDead = true;
            Died?.Invoke(damage);
        }

        /// <summary>
        /// Restores this actor for its initial spawn or a future object-pool reuse.
        /// </summary>
        public void ResetHealth()
        {
            maxHealth = Mathf.Max(0.01f, maxHealth);
            currentHealth = maxHealth;
            isDead = false;
            LastDamage = default;
        }

        /// <summary>
        /// Applies a resolved archetype/evolution maximum. Runtime evolution
        /// does not heal an already-living enemy unless the caller explicitly
        /// requests a refill (normally only during initial spawn).
        /// </summary>
        public void SetMaximumHealth(float value, bool refillCurrentHealth)
        {
            maxHealth = Mathf.Max(0.01f, value);
            currentHealth = refillCurrentHealth
                ? maxHealth
                : Mathf.Min(currentHealth, maxHealth);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(0.01f, maxHealth);

            if (!Application.isPlaying)
            {
                currentHealth = maxHealth;
                isDead = false;
            }
        }
    }
}
