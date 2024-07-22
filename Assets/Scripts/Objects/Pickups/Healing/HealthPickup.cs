using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {

    public int heal;

    PlayerHealth playerHealth;

    void OnTriggerEnter(Collider other) {
        
        if(other.GetComponentInParent<PlayerHealth>() != false) {
            // Debug.Log("COllided");
            playerHealth = other.GetComponentInParent<PlayerHealth>();
            if(playerHealth.CurrentHealth() < playerHealth.maxHealth) {
                playerHealth.RestoreHealth(heal);
                Destroy(gameObject);
            }
            
        }
    }
   
}
