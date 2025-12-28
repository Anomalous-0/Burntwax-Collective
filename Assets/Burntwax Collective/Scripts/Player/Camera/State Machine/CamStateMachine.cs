using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CamStateMachine : MonoBehaviour
{
    public InputManager input;

    public Transform cam;
    CinemachinePOV pov;

    [Header("State Machines")]
    [HideInInspector] public PlayerStateMachine player;

    public PauseMenu pauseMenu;

    public GunStateMachine gun;
    [HideInInspector] public PlayerCrosshair crosshair;

    CinemachineVirtualCamera activeCam;

    public CinemachineVirtualCamera fpsCam;
    public CinemachineVirtualCamera aimCam;

    public GameObject primaryCam;
    public GameObject secondaryCam;

    public CameraBaseState currentState;
    public CameraStateFactory states;

    public CameraBaseState CurrentState { get { return currentState; } set { currentState = value; } }
    int activeCameraPriorityModifier = 2000;

    public Vector3 mouseWorldPosition;


    private void Awake()
    {
        activeCam = fpsCam;
        primaryCam = fpsCam.gameObject;
        secondaryCam = aimCam.gameObject;
        player = GetComponent<PlayerStateMachine>();
        crosshair = GetComponent<PlayerCrosshair>();
        pov = activeCam.GetCinemachineComponent<CinemachinePOV>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        states = new CameraStateFactory(this);
        currentState = states.Fps();
        currentState.EnterState();

    }
    void Update()
    {

        currentState.UpdateState();
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f))
        {
            mouseWorldPosition = raycastHit.point;
        }

    }


    public void Aim()
    {

        if (activeCam == fpsCam)
        {
            SetCameraPriorities(fpsCam, aimCam);
        }

    }

    public void FPS()
    {

        if (activeCam == aimCam)
        {
            SetCameraPriorities(aimCam, fpsCam);
        }

    }

    private void SetCameraPriorities(CinemachineVirtualCamera CurrentCameraMode, CinemachineVirtualCamera NewCameraMode)
    {
        CurrentCameraMode.Priority -= activeCameraPriorityModifier;
        NewCameraMode.Priority += activeCameraPriorityModifier;
        activeCam = NewCameraMode;
        pov = activeCam.GetCinemachineComponent<CinemachinePOV>();

    }

    public void LoadData(CamSaveData data)
    {
        pov.m_HorizontalAxis.Value = data.yaw;
        pov.m_VerticalAxis.Value = data.pitch;
    }

    public void SaveData(ref CamSaveData data)
    {
        data.yaw = pov.m_HorizontalAxis.Value;
        data.pitch = pov.m_VerticalAxis.Value;
    }
}

[System.Serializable]
public struct CamSaveData
{
    public float yaw;
    public float pitch;
}
