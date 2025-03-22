using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ProceduralRecoil : MonoBehaviour {
    
    [Header("Resting Position")]


    [SerializeField] Vector3 restingPosition;
    Vector3 restingRotation; 

    [Header("Speed Settings")]

    public float positionalRecoilSpeed = 8f;
    public float rotationalRecoilSpeed = 8f;
    [Space(10)]

    public float positionalReturnSpeed = 18f;
    public float rotationalReturnSpeed = 38f;

    [Space()]

    [Header("Recoil Settings")]
    public Transform recoilPosition;
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

    public void SetRestingPosition() {
        restingPosition.x = transform.localPosition.x;
        restingPosition.y = transform.localPosition.y;
        restingPosition.z = transform.localPosition.z;
        // restingRotation = transform.localRotation.eulerAngles;
    }

    void FixedUpdate() {
        // Debug.Log(transform.localPosition.x + " " + transform.localPosition.y + " " + transform.localPosition.z);
        if(gunMachine.ActiveGunScriptable) {
            if(gunMachine.ActiveGunScriptable.proceduralRecoil) {
                rotationalRecoil = Vector3.Lerp(rotationalRecoil, restingRotation, rotationalReturnSpeed*Time.fixedDeltaTime);
                positionalRecoil = Vector3.Lerp(positionalRecoil, restingPosition, positionalReturnSpeed * Time.fixedDeltaTime);

                recoilPosition.localPosition = Vector3.Slerp(recoilPosition.localPosition, positionalRecoil, positionalRecoilSpeed * Time.fixedDeltaTime);
                rot = Vector3.Slerp(rot, rotationalRecoil, rotationalRecoilSpeed * Time.fixedDeltaTime);
                transform.localRotation = Quaternion.Euler(rot);
              
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
