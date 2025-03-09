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


    void LateUpdate() {
        currentRotation = Vector3.Lerp(currentRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        // rot = Vector
    }



    // public void Recoil(){
        // gunMachine.localRotation = gunMachine.Quaternion.Euler(0,0,0,)
    // } 
}
