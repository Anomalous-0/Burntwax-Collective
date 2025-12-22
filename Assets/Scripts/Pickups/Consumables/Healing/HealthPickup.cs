using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(Rigidbody))]
public class HealthPickup : MonoBehaviour {
    
    public int heal;


    private PlayerHealth playerHealth;

    void Start()
    {
        ObjectManager.Instance.Register(gameObject);
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



