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

    private void OnTriggerEnter(Collider other) {

        Debug.Log("Entered");

        if (other.GetComponent<BulletTarget>() != false) {
            other.GetComponent<Rigidbody>().AddForce(bulletRb.velocity, ForceMode.Impulse);
            Debug.Log("Hit a target");

            string surfaceTag = other.tag;

            BulletImpactVFX vfxManager = FindObjectOfType<BulletImpactVFX>();
            if (vfxManager != null)
            {
                vfxManager.SpawnImpactVFX(surfaceTag, bulletRb.transform.position, Quaternion.identity);
            }

            if (other.GetComponent<BulletTarget>() != false)
            {
                other.GetComponent<Rigidbody>().AddForce(bulletRb.velocity, ForceMode.Impulse);
                Debug.Log("Hit a target");
            }

            Destroy(gameObject);

        }
        
        Destroy(gameObject);
    }
}
