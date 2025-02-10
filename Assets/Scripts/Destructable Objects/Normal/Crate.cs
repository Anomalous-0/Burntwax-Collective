using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]

public class Crate : MonoBehaviour, Destructable {

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] int currentHealth;

    
    [Header("Vfx")]
    [SerializeField] GameObject damageParticles;
    [SerializeField] GameObject destructParticles;


    

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
            
            HandleDamage(transform.position);
        }

        if(currentHealth == 0 && damageTaken!= 0) {
           
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
            SpawnResources(pos);
            Destroy(gameObject);
            
            
        }
        
    }


    private void SpawnResources(Vector3 pos) {
        // Instantiate(resourcePrefab, pos, Quaternion.identity);
        Instantiate(resources_prefabs[Random.Range(0,resources_prefabs.Count)], pos, Quaternion.identity);
        Instantiate(resources_prefabs[Random.Range(0,resources_prefabs.Count)], pos, Quaternion.identity);
        Instantiate(resources_prefabs[Random.Range(0,resources_prefabs.Count)], pos, Quaternion.identity);
    }

    private void Start() {
        currentHealth = maxHealth;
    }


    
}
