using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolPickup : MonoBehaviour {
    
    [SerializeField] GameObject glove;


    void OnTriggerEnter(Collider other) {
        if(other.GetComponentInParent<PlayerHealth>() != false) {
           glove.SetActive(true);
           Destroy(gameObject);   
        }
    }
}
