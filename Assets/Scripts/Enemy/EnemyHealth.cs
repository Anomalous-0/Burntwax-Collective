using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour {
    // Start is called before the first frame update
    public float maxHealth;
    public float currentHealth;

    Ragdoll ragdoll;

    void Start() {
        currentHealth = maxHealth;
        ragdoll = GetComponent<Ragdoll>();
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void TakeDamage(float damage) {
        currentHealth -= damage;
        if(currentHealth <= 0.0f) {
            Die();
        }
    }

    private void Die() {
        ragdoll.ActivateRagdoll();
    }
}
