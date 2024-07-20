using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Ragdoll : MonoBehaviour {
    // Start is called before the first frame update
    [SerializeField] Animator animator;
    Rigidbody[] rigidBodies;
    

    void Start() {
        rigidBodies = GetComponentsInChildren<Rigidbody>();
        DeactivateRagdoll();
    }

    public void DeactivateRagdoll() {
        foreach(var rigidBody in rigidBodies) {
            rigidBody.isKinematic = true;
        }
        animator.enabled = true;
    }

    public void ActivateRagdoll() {
        foreach(var rigidBody in rigidBodies) {
            rigidBody.isKinematic = false;
        }
        animator.enabled = false;
    }


}
