using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoPickup : MonoBehaviour {
   
    public int ammoCount;


    [SerializeField] GunScriptableObject gun;

    private GunStateMachine playerGun;


    void OnTriggerEnter(Collider other) {
        
        if(other.GetComponentInParent<PlayerHealth>() || other.GetComponent<ConsumableGlovePickup>()) {
            playerGun = FindObjectOfType<GunStateMachine>();

            if(playerGun.CurrentGuns.Contains(gun)) {
                if(gun.AmmoConfig.currentAmmo < gun.AmmoConfig.maxAmmo) {
                    if(gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo > ammoCount) {
                        gun.AmmoConfig.currentAmmo += ammoCount;
                    }
                    else {
                        gun.AmmoConfig.currentAmmo += gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo;
                    }
                    Destroy(gameObject);
                }   
            }
            
            
            
        
            // Debug.Log("COllided");
        
            
            // if(playerHealth.CurrentHealth() < playerHealth.maxHealth) {
            //     playerHealth.RestoreHealth(heal);
            //     Destroy(gameObject);
            // }
            
        }
    }



}
