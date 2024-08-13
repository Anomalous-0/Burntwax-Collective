using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoPickup : MonoBehaviour {
   
    public int ammoCount;

    public int gunID;

    private GunStateMachine playerGun;


    void OnTriggerEnter(Collider other) {
        
        if(other.GetComponentInParent<PlayerHealth>() != false || other.GetComponent<ConsumableGlovePickup>()) {
            

            
            playerGun = FindObjectOfType<GunStateMachine>();

            GunScriptableObject gun = playerGun.Guns.Find(gun => gun.ID == gunID);
            if(gun.AmmoConfig.currentAmmo < gun.AmmoConfig.maxAmmo) {
                if(gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo > ammoCount) {
                    gun.AmmoConfig.currentAmmo += ammoCount;
                }
                else {
                    gun.AmmoConfig.currentAmmo += gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo;
                }
                Destroy(gameObject);
            }
            
            
        
            // Debug.Log("COllided");
        
            
            // if(playerHealth.CurrentHealth() < playerHealth.maxHealth) {
            //     playerHealth.RestoreHealth(heal);
            //     Destroy(gameObject);
            // }
            
        }
    }



}
