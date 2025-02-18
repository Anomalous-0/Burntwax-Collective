using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBullet : MonoBehaviour {
    private Rigidbody bulletRb;

    [SerializeField] private Transform vfxHitGreen;
    [SerializeField] private Transform vfxHitRed;
    [SerializeField] private int damage;
    [SerializeField] private float speed = 120.0f;

    private void Awake() {
        bulletRb = GetComponent<Rigidbody>();
    }

    private void Start() {
        bulletRb.velocity = transform.forward * speed;
    }

    private void OnTriggerStay(Collider other) {
        if(other.GetComponent<BulletTarget>() != false) {
            other.GetComponent<Rigidbody>().AddForce(bulletRb.velocity, ForceMode.Impulse);
            Debug.Log("Hit a target");
            //var rb2d = hitinfo.collider.GetComponent<Rigidbody>();
            

            // if(other.GetComponentInParent<EnemyTarget>() != false) {
                // Debug.Log("Hit an enemy");
            //}
            
            
        
            
            //Instantiate(vfxHitRed, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
