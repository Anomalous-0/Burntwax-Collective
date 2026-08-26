using System;
using UnityEngine;

namespace Burntwax
{
    public class PlayerHealth : MonoBehaviour,
        IDamageable,
        IPlayerResourceSource
    {
        private float health;

        [Header("Resource")]
        [SerializeField] private PlayerResourceDefinition resourceDefinition;

        [Header("Health")]
        public float maxHealth = 100;

        public PlayerResourceDefinition ResourceDefinition =>
            resourceDefinition;
        public float CurrentValue => health;
        public float MaximumValue => maxHealth;
        public bool IsResourceAvailable => isActiveAndEnabled;

        public event Action<IPlayerResourceSource> ResourceChanged;

        private void Awake()
        {
            health = maxHealth;
        }

        private void Start()
        {
            NotifyResourceChanged();
        }

        public void TakeDamage(float damage)
        {
            SetHealth(health - damage);
        }

        public void ApplyDamage(DamageInfo damage)
        {
            TakeDamage(damage.Amount);
        }

        public void RestoreHealth(float healAmount)
        {
            SetHealth(health + healAmount);
        }

        public float CurrentHealth()
        {
            return health;
        }

        public void LoadData(PlayerHealthSaveData data)
        {
            SetHealth(data.health);
        }
        public void SaveData(ref PlayerHealthSaveData data)
        {
            data.health = health;
        }

        private void SetHealth(float value)
        {
            float nextHealth = Mathf.Clamp(value, 0f, maxHealth);
            if (Mathf.Approximately(nextHealth, health))
                return;

            health = nextHealth;
            NotifyResourceChanged();
        }

        private void NotifyResourceChanged()
        {
            ResourceChanged?.Invoke(this);
        }
    }

    [System.Serializable]
    public struct PlayerHealthSaveData
    {
        public float health;
    }

}
