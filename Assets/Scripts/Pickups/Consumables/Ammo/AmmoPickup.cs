using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AmmoPickup : MonoBehaviour
{

    public int ammoCount;


    [SerializeField] GunScriptableObject gun;

    private GunStateMachine playerGun;


    void Start()
    {
        ObjectManager.Instance.Register(gameObject);
    }


    //Collect ammo
    void OnTriggerEnter(Collider other)
    {

        if (other.GetComponentInParent<PlayerHealth>())
        {
            playerGun = FindFirstObjectByType<GunStateMachine>();

            if (playerGun.CurrentGuns.Contains(gun))
            {
                if (gun.AmmoConfig.currentAmmo < gun.AmmoConfig.maxAmmo)
                {
                    if (gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo > ammoCount)
                    {
                        gun.AmmoConfig.currentAmmo += ammoCount;
                    }
                    else
                    {
                        gun.AmmoConfig.currentAmmo += gun.AmmoConfig.maxAmmo - gun.AmmoConfig.currentAmmo;
                    }
                    gameObject.SetActive(false);  
                }
            }
        }
    }
}


