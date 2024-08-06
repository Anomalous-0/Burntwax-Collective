using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoPickup : MonoBehaviour {
   
    public int ammoCount;

    public int gunID;
    [SerializeField] GunScriptableObject gun;


    void OnTriggerEnter(Collider other) {
        
        if(other.GetComponentInParent<GunStateMachine>() != false || other.GetComponent<ConsumableGlovePickup>()) {


            gun.AmmoConfig.currentAmmo += ammoCount;
        
            // Debug.Log("COllided");
            playerHealth = FindObjectOfType<PlayerHealth>();
            if(playerHealth.CurrentHealth() < playerHealth.maxHealth) {
                playerHealth.RestoreHealth(heal);
                Destroy(gameObject);
            }
            
        }
    }



}
