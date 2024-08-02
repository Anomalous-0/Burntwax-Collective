
using UnityEngine;

public class NPCHealth : MonoBehaviour, Damageable {

    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;

    public int CurrentHealth { get => currentHealth; private set =>currentHealth = value;}

    public int MaxHealth { get => maxHealth; private set => maxHealth = value;}

    public event Damageable.DamageEvent OnTakeDamage;
    public event Damageable.DeathEvent OnDeath;


    private void OnEnable() {
        currentHealth = maxHealth;
    }
    public void TakeDamage(int damage) {
        int damageTaken = Mathf.Clamp(damage, 0, currentHealth);
        currentHealth -= damageTaken;
        if(damageTaken != 0) {
            OnTakeDamage?.Invoke(damageTaken);
        }
        if(currentHealth == 0 && damageTaken!= 0) {
            OnDeath?.Invoke(transform.position);
        }
    }
}
