// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class SMGRecoil : MonoBehaviour {

//     [Header("Reference Points:")]
//     public Transform recoilPosition;
//     public Transform rotationPoint;

//     [Header("Speed Settings:")]
//     public float positionalRecoilSpeed = 8f;
//     public float rotationalRecoilSpeed = 8f;
//     public float positionalReturnSpeed = 18f;
//     public float rotationalReturnSpeed = 38f;

//     [Header("Amount Settings:")]
//     public Vector3 recoilRotation = new Vector3(10f, 5f, 7f);
//     public Vector3 recoilKickBack = new Vector3(.015f, 0f, -0.2f);

//     public Vector3 recoilRotationAim = new Vector3(10f, 4f, 6f);
//     public Vector3 recoilKickBackAim = new Vector3(.015f, 0f, -0.2f);

//     [Space(10)]

//     Vector3 rotationalRecoil;
//     Vector3 positionalRecoil;
//     Vector3 rot;


//     [Header("States:")]

//     public GunStateMachine gunMachine;
//     public CamStateMachine camMachine;
//     public InputManager input;


    

//     void Start() {
//         gunMachine = FindObjectOfType<GunStateMachine>();
//         camMachine = FindObjectOfType<CamStateMachine>();
//         input = FindObjectOfType<InputManager>();
//         recoilPosition = GetComponent<Transform>();
//         // Very sketchy
//         rotationPoint = FindObjectOfType<RotationPoint>().GetComponent<Transform>();
//     }

//     void FixedUpdate() {
//         rotationalRecoil = Vector3.Lerp(rotationalRecoil, Vector3.zero, rotationalReturnSpeed * Time.deltaTime);
//         positionalRecoil = Vector3.Lerp(positionalRecoil, Vector3.zero, positionalReturnSpeed * Time.deltaTime);

//         recoilPosition.localPosition = Vector3.Slerp(recoilPosition.localPosition, positionalRecoil, positionalRecoilSpeed * Time.fixedDeltaTime);
//         rot = Vector3.Slerp(rot, rotationalRecoil, rotationalRecoilSpeed * Time.fixedDeltaTime);
//         rotationPoint.localRotation = Quaternion.Euler(rot);
//     }


//     public void Fire() {
//         if(camMachine.CurrentState == camMachine.states.Aim()) {
//             rotationalRecoil += new Vector3(-recoilRotationAim.x, Random.Range(-recoilRotationAim.y, recoilRotationAim.y), Random.Range(-recoilRotationAim.z, recoilRotationAim.z));
//             positionalRecoil += new Vector3(Random.Range(-recoilKickBackAim.x, recoilKickBackAim.x), Random.Range(-recoilKickBackAim.y, recoilKickBackAim.y), recoilKickBackAim.z);
//         }
//         else {
//             rotationalRecoil += new Vector3(-recoilRotation.x, Random.Range(-recoilRotation.y, recoilRotation.y), Random.Range(recoilRotation.z, recoilRotation.z));
//             positionalRecoil += new Vector3(Random.Range(-recoilKickBack.x, recoilKickBack.x), Random.Range(-recoilKickBack.y, recoilKickBack.y), recoilKickBack.z);
//         }
//     }


//     void Update() {
//         if(gunMachine.CurrentState.CurrentSubstate() == gunMachine.states.Shoot() && input.shootIsPressed) {
//             Fire();
//         }

//     }

    
// }
