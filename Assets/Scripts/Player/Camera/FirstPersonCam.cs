using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class FirstPersonCam : MonoBehaviour {
    public InputManager input;
    CinemachineVirtualCamera activeCam;
     
    public CinemachineVirtualCamera fpsCam;
    public CinemachineVirtualCamera aimCam;
    int activeCameraPriorityModifier = 2000;


    // public Quaternion rotDir;
    // public Quaternion lookDir;
    
    public Vector3 mouseWorldPosition;


    private void Awake() {
        activeCam = fpsCam;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void Update() {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        if(Physics.Raycast(ray, out RaycastHit raycastHit, 999f)) {
            mouseWorldPosition = raycastHit.point;
        }
        
    }
    
    
        
        // if(Physics.Raycast(ray, out raycastHit, 999f, bulletColliderLayerMask)) {
        //     debug.position = raycastHit.point;
        // }
        
        
                
                
                
        //         //Useful for moving in any direction for wall running
                // orientation.forward = transform.forward;


        

        // if(input.shootIsPressed) {
        //     Vector3 aimDir = (debug.position - bulletSpawn.position).normalized;
        //     Instantiate(bullet, bulletSpawn.position, Quaternion.LookRotation(aimDir, Vector3.up));
        //     input.shootIsPressed = false;
        // }
    


    public void Aim() {
    
            if(activeCam == fpsCam) {
                SetCameraPriorities(fpsCam, aimCam);
            }
    
    }

    public void FPS() {
    
            if(activeCam == aimCam) {
                SetCameraPriorities(aimCam, fpsCam);
            }
        
    }

    private void SetCameraPriorities(CinemachineVirtualCamera CurrentCameraMode, CinemachineVirtualCamera NewCameraMode) {
            CurrentCameraMode.Priority -= activeCameraPriorityModifier;
            NewCameraMode.Priority += activeCameraPriorityModifier;
            activeCam = NewCameraMode;
    }
}
