
using Unity.VisualScripting;
using UnityEngine;

public class GunPickup : MonoBehaviour {


    [SerializeField] int ammoCount;

    [SerializeField] GunScriptableObject gun;
    private GunStateMachine gunSelector;



    void OnTriggerEnter(Collider other) {
        if(other.GetComponentInParent<PlayerHealth>() != false) {
            gunSelector = FindObjectOfType<GunStateMachine>();
            if(!gunSelector.CurrentGuns.Contains(gun)) {
                gunSelector.Pickup(gun);
            }
            else {
                if(gun.AmmoConfig.currentAmmo < gun.AmmoConfig.maxAmmo) {
                    if(gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo > ammoCount) {
                        gun.AmmoConfig.currentAmmo += ammoCount;
                    }
                    else {
                        gun.AmmoConfig.currentAmmo += gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo;
                    }
                   
                } 
            }
            
            Destroy(gameObject);   
        }
    }

}
