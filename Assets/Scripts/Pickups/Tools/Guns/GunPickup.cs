
using Unity.VisualScripting;
using UnityEngine;

public class GunPickup : MonoBehaviour {


    [SerializeField] int ammo;

    [SerializeField] GunScriptableObject gun;
    private GunStateMachine gunSelector;



    void OnTriggerEnter(Collider other) {
        if(other.GetComponentInParent<PlayerHealth>() != false) {
            gunSelector = FindObjectOfType<GunStateMachine>();
            gunSelector.Pickup(gun);
            Destroy(gameObject);   
        }
    }

}
