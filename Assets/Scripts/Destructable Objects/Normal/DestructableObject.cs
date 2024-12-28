using UnityEngine;

[DisallowMultipleComponent]

public class DestructableObject : MonoBehaviour, Destructable {

    [SerializeField] private int maxHealth = 100;
    [SerializeField] int currentHealth;
    public int CurrentHealth {get => currentHealth; private set => currentHealth = value;}

    public int MaxHealth {get => maxHealth; private set => maxHealth = value;}

    public event Destructable.TakeDamageEvent OnTakeDamage;
    public event Destructable.Destruct OnDestruct;

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

    private void Start() {
        currentHealth = maxHealth;
    }


    
}
