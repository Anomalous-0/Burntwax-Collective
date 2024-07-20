using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {

    public int heal;

    PlayerHealth playerHealth;

    void OnTriggerStay(Collider other) {
        
        if(other.GetComponentInParent<PlayerHealth>() != false) {
            // Debug.Log("COllided");
            playerHealth = other.GetComponentInParent<PlayerHealth>();
            if(playerHealth.maxHealth  - playerHealth.CurrentHealth() > heal) {
                playerHealth.RestoreHealth(heal);
                Destroy(gameObject);
            }
            
        }
    }
   
}
