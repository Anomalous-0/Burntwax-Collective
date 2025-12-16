using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(Rigidbody))]
public class NewBehaviourScript : Consumable {
    
    public int heal;


    private PlayerHealth playerHealth;

    void Start()
    {
        ConsumableManager.Instance.Register(gameObject);
    }

    void OnTriggerEnter(Collider other) {
        
        if(other.GetComponentInParent<PlayerHealth>() != false || other.GetComponent<ConsumableGlovePickup>()) {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if(playerHealth.CurrentHealth() < playerHealth.maxHealth) {
                playerHealth.RestoreHealth(heal);
                gameObject.SetActive(false);
                // Destroy(gameObject);
                
            }
            
        }
    }
   
}



