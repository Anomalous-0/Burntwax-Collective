using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class DestructableObjectHealth : MonoBehaviour, Damageable {

    [SerializeField] private int maxHealth = 100;
    [SerializeField] int currentHealth;
    public int CurrentHealth {get => currentHealth; private set => currentHealth = value;}

    public int MaxHealth {get => maxHealth; private set => maxHealth = value;}


    public event Damageable.TakeDamageEvent OnTakeDamage;
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
            Destroy(gameObject);
        }
    }



    
}
