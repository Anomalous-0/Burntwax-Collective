using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CamStateMachine : MonoBehaviour {
    public InputManager input;

    public Transform cam;


    [Header("State Machines")]
    [HideInInspector] public PlayerStateMachine player;

    public PauseMenu pauseMenu;
    
    public GunStateMachine gun;
    [HideInInspector] public PlayerCrosshair crosshair;

    CinemachineVirtualCamera activeCam;
     
    public CinemachineVirtualCamera fpsCam;
    public CinemachineVirtualCamera aimCam;

    /*[HideInInspector]*/ public GameObject primaryCam;
    private GameObject temp;
    /*[HideInInspector]*/ public GameObject secondaryCam;

    public CameraBaseState currentState;
    public CameraStateFactory states;

    //Attracting
    public CameraBaseState CurrentState {get { return currentState; } set { currentState = value; } }
    int activeCameraPriorityModifier = 2000;


    // public Quaternion rotDir;
    // public Quaternion lookDir;
    
    public Vector3 mouseWorldPosition;


    private void Awake() {
        activeCam = fpsCam;
        primaryCam = fpsCam.gameObject;
        secondaryCam = aimCam.gameObject;
        player = GetComponent<PlayerStateMachine>();
        crosshair = GetComponent<PlayerCrosshair>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        states = new CameraStateFactory(this);
        currentState = states.Fps();
        currentState.EnterState();
        
    }

    // private void Start() {
    //     cam.GetComponent<Rigidbody>().freezeRotation = false;
    // }


    void Update() {

        currentState.UpdateState();
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        if(Physics.Raycast(ray, out RaycastHit raycastHit, 999f)) {
            mouseWorldPosition = raycastHit.point;
        }
        
    }


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

    private void SetCameraPriorities(CinemachineVirtualCamera CurrentCameraMode, CinemachineVirtualCamera NewCameraMode)
    {
        CurrentCameraMode.Priority -= activeCameraPriorityModifier;
        NewCameraMode.Priority += activeCameraPriorityModifier;
        activeCam = NewCameraMode;
        
    }
}
