using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ProceduralRecoil : MonoBehaviour {
    
    [Header("Speed Settings")]

    public float positionalRecoilSpeed = 8f;
    public float rotationalRecoilSpeed = 8f;
    [Space(10)]

    public float positionalReturnSpeed = 18f;
    public float rotationalReturnSpeed = 38f;

    [Space()]

    [Header("Recoil Settings")]
    public Vector3 recoilRotation = new Vector3(10,5,7);
    public Vector3 recoilKickBack = new Vector3(0.015f, 0f, 0.2f);
    [Space()]
    public Vector3 recoilRotationAim = new Vector3(10,4,0);
    public Vector3 recoilKickBackAim = new Vector3(0.015f, 0f, -0.2f);
    Vector3 rotationalRecoil;
    Vector3 positionalRecoil;

    [Header("State Machine References")]
    public GunStateMachine gunMachine;
    
    private Vector3 rot;


    void FixedUpdate() {
        if(gunMachine.ActiveGunScriptable) {
            if(gunMachine.ActiveGunScriptable.proceduralRecoil) {
                rotationalRecoil = Vector3.Lerp(rotationalRecoil, Vector3.zero, rotationalReturnSpeed*Time.fixedDeltaTime);
                positionalRecoil = Vector3.Lerp(positionalRecoil, Vector3.zero, positionalReturnSpeed * Time.fixedDeltaTime);

                transform.localPosition = Vector3.Slerp(transform.localPosition, positionalRecoil, positionalRecoilSpeed * Time.fixedDeltaTime);
                rot = Vector3.Slerp(rot, rotationalRecoil, rotationalRecoilSpeed * Time.fixedDeltaTime);
                transform.localRotation = Quaternion.Euler(rot);
                // currentRotation = Vector3.Lerp(currentRotation, Vector3.zero, returnSpeed * Time.deltaTime);
                // rot = Vector3.Slerp(rot, currentRotation, rotationSpeed * Time.deltaTime);
                // transform.localRotation = Quaternion.Euler(rot);
            }
            
        }
        
    }

    public void Recoil( ) {
        if(gunMachine.CurrentState == gunMachine.states.Aim()) {
            rotationalRecoil += new Vector3(-recoilRotationAim.x, 
            Random.Range(-recoilRotationAim.y, recoilRotationAim.y), 
            Random.Range(0, -recoilRotationAim.z));

            positionalRecoil += new Vector3(Random.Range(-recoilKickBackAim.x, recoilKickBackAim.x), 
            Random.Range(-recoilKickBackAim.y, recoilKickBackAim.y), 
            recoilKickBackAim.z);
        }
        else {
            rotationalRecoil += new Vector3(-recoilRotation.x, 
            Random.Range(-recoilRotation.y, recoilRotation.y), 
            Random.Range(0, -recoilRotation.z));

            positionalRecoil += new Vector3(Random.Range(-recoilKickBack.x, recoilKickBack.x), 
            Random.Range(-recoilKickBack.y, recoilKickBack.y), 
            recoilKickBack.z);
        }
    }





}
