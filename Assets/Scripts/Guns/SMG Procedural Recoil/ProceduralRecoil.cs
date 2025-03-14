using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ProceduralRecoil : MonoBehaviour {
    
    [Header("Recoil Settings")]
    public float rotationSpeed = 6f;
    public float returnSpeed = 25f;

    [Space()]

    [Header("Hipfire")]
    public Vector3 recoilRotation = new Vector3(2f,2f,2f);
    [Space()]

    [Header("Aiming")]
    public Vector3 recoilRotationAim = new Vector3(0.5f,0.5f,1.5f);
    [Space()]

    [Header("State")]
    public GunStateMachine gunMachine;
    private Vector3 currentRotation;
    private Vector3 rot;


    void FixedUpdate() {
        if(gunMachine.ActiveGunScriptable) {
            if(gunMachine.ActiveGunScriptable.proceduralRecoil) {
                currentRotation = Vector3.Lerp(currentRotation, Vector3.zero, returnSpeed * Time.deltaTime);
                rot = Vector3.Slerp(rot, currentRotation, rotationSpeed * Time.fixedDeltaTime);
                transform.localRotation = Quaternion.Euler(rot);
            }
            
        }
        
    }
    void Update() {
        // if(gunMachine.input.shootIsPressed) {
        //     Recoil();
        // }
    }

    public void Recoil( ) {
        if(gunMachine.CurrentState == gunMachine.states.Aim()) {
            currentRotation += new Vector3(-recoilRotationAim.x, 
            Random.Range(-recoilRotationAim.y, recoilRotationAim.y), 
            Random.Range(-recoilRotationAim.z, recoilRotationAim.z));
        }
        else {
            currentRotation += new Vector3(-recoilRotation.x, 
            Random.Range(-recoilRotation.y, recoilRotation.y), 
            Random.Range(-recoilRotation.z, recoilRotation.z));
        }
    }





}
