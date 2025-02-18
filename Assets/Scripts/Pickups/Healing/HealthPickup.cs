using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {

    public int heal;

    private PlayerHealth playerHealth;

    void OnTriggerEnter(Collider other) {
        
        if(other.GetComponentInParent<PlayerHealth>() != false || other.GetComponent<ConsumableGlovePickup>()) {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if(playerHealth.CurrentHealth() < playerHealth.maxHealth) {
                playerHealth.RestoreHealth(heal);
                Destroy(gameObject);
            }
            
        }
    }
   
}
