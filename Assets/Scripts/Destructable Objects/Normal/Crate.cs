using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]

public class Crate : MonoBehaviour, Destructable {

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] int currentHealth;

    
    [Header("Vfx")]
    [SerializeField] ParticleSystem damageParticles;
    [SerializeField] ParticleSystem destructParticles;


    

    [Header("Customizables")]
    [SerializeField] bool disappear;
    public List<GameObject> resources_prefabs;
    public int CurrentHealth {get => currentHealth; private set => currentHealth = value;}

    public int MaxHealth {get => maxHealth; private set => maxHealth = value;}

    // public event Destructable.TakeDamageEvent OnTakeDamage;
    // public event Destructable.Destruct OnDestruct;

    public void TakeDamage(int damage) {
        int damageTaken = Mathf.Clamp(damage, 0, currentHealth);
        currentHealth -= damageTaken; 

        if(damageTaken != 0) {
            Debug.Log("Damage taken: " + damageTaken);
            HandleDamage(transform.position);
        }

        if(currentHealth == 0 && damageTaken!= 0) {
            Debug.Log("Crate destroyed");
            HandleDestruct(transform.position);
            
        }
    }

    private void HandleDamage(Vector3 pos) {
        if(damageParticles) {
            Instantiate(damageParticles, pos, Quaternion.identity);

        }
        
    }

    private void HandleDestruct(Vector3 pos) {
        if(destructParticles) {
            Instantiate(destructParticles, pos, Quaternion.identity);
            

        }
        if(disappear) {
            Destroy(gameObject);
        }
        
    }


    private void SpawnResources(Vector3 pos) {
        // Instantiate(resourcePrefab, pos, Quaternion.identity);
   
    }

    private void Start() {
        currentHealth = maxHealth;
    }


    
}
