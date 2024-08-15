// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class SMGRecoil : MonoBehaviour {

//     [Header("Recoil Settings")]
//     public float rotationSpeed = 6f;
//     public float returnSpeed = 25f;

//     [Space()]

//     [Header("Hipfire:")]
//     public Vector3 recoilRotation = new Vector3(2f, 2f, 2f);
    
//     [Header("Aiming:")]
//     public Vector3 recoilRotationAiming = new Vector3(.5f, .5f, 1.5f);

//     [Header("State:")]
//     public bool aiming;
//     public GunStateMachine gunMachine;
//     public CamStateMachine camMachine;

//     private Vector3 currentRotation;
//     private Vector3 rot;


//     void Start() {
//         gunMachine = FindObjectOfType<GunStateMachine>();
//         camMachine = FindObjectOfType<CamStateMachine>();
//     }

//     void FixedUpdate() {
//         currentRotation = Vector3.Lerp(currentRotation, Vector3.zero, returnSpeed * Time.deltaTime);
//         rot = Vector3.Slerp(rot, currentRotation, rotationSpeed * Time.fixedDeltaTime);
//         transform.localRotation = Quaternion.Euler(rot);
//     }


//     public void Fire() {
//         if(camMachine.currentState == camMachine.states.Aim()) {
//             currentRotation += new Vector3(-recoilRotationAiming.x, Random.Range(-recoilRotationAiming.y, recoilRotationAiming.y), Random.Range(-recoilRotationAiming.z, recoilRotationAiming.z));
//         }
//         else {
//             currentRotation += new Vector3(-recoilRotation.x, Random.Range(-recoilRotation.y, recoilRotation.y), Random.Range(-recoilRotation.z, recoilRotation.z));
//         }
//     }

//     void Update() {
//         if(gunMachine.CurrentState.CurrentSubstate() == gunMachine.states.Shoot()) {
//             Fire();
//         }

//     }

    
// }
