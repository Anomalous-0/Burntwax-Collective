using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AmmoPickup : Consumable {
   
    public int ammoCount;


    [SerializeField] GunScriptableObject gun;

    private GunStateMachine playerGun;


    void Start()
    {
        ConsumableManager.Instance.Register(gameObject);
    }

    void OnTriggerEnter(Collider other) {
        
        if(other.GetComponentInParent<PlayerHealth>() || other.GetComponent<ConsumableGlovePickup>()) {
            playerGun = FindFirstObjectByType<GunStateMachine>();

            if(playerGun.CurrentGuns.Contains(gun)) {
                if(gun.AmmoConfig.currentAmmo < gun.AmmoConfig.maxAmmo) {
                    if(gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo > ammoCount) {
                        gun.AmmoConfig.currentAmmo += ammoCount;
                    }
                    else {
                        gun.AmmoConfig.currentAmmo += gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo;
                    }
                    gameObject.SetActive(false);
                    // Destroy(gameObject);    
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


