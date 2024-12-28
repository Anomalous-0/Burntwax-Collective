using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class DestructableObjectHealth : MonoBehaviour, Destructable {

    [SerializeField] private int maxHealth = 100;
    [SerializeField] int currentHealth;
    public int CurrentHealth {get => currentHealth; private set => currentHealth = value;}

    public int MaxHealth {get => maxHealth; private set => maxHealth = value;}


    public event Destructable.TakeDamageEvent OnTakeDamage;
    public event Destructable.Destruct OnDestruct;

    private void OnEnable() {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage) {
        int damageTaken = Mathf.Clamp(damage, 0, currentHealth);
        currentHealth -= damageTaken; 

        if(damageTaken != 0) {
            Debug.Log("Damage taken: " + damageTaken);
            OnTakeDamage?.Invoke(damageTaken);
        }

        if(currentHealth == 0 && damageTaken!= 0) {
            Debug.Log("Crate destroyed");
            OnDestruct?.Invoke(transform.position);
            
        }
    }



    
}
