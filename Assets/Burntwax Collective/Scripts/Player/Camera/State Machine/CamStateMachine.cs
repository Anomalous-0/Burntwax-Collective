using UnityEngine;
using Cinemachine;

namespace Burntwax
{
    public class CamStateMachine : MonoBehaviour
    {
        public Transform cam;
        CinemachinePOV pov;

        [Header("State Machines")]
        [HideInInspector] public PlayerStateMachine player;

        public GameObject Arms;

        [HideInInspector] public Arsenal arsenal;
        [HideInInspector] public GunStateMachine gun;
        [HideInInspector] public KatanaStateMachine katana;

        public PauseMenu pauseMenu;


        [HideInInspector] public PlayerCrosshair crosshair;

        CinemachineVirtualCamera activeCam;

        public CinemachineVirtualCamera fpsCam;
        public CinemachineVirtualCamera aimCam;

        public CinemachineVirtualCamera chargeCam;
        public CameraBaseState currentState;
        public CameraStateFactory states;

        public CameraBaseState CurrentState { get { return currentState; } set { currentState = value; } }
        int activeCameraPriorityModifier = 2000;

        public Vector3 mouseWorldPosition;

        private float cachedHorizontalSpeed;
        private float cachedVerticalSpeed;




        private void Awake()
        {
            activeCam = fpsCam;
            player = GetComponent<PlayerStateMachine>();
            crosshair = GetComponent<PlayerCrosshair>();

            arsenal = Arms.GetComponent<Arsenal>();
            gun = Arms.GetComponent<GunStateMachine>();
            katana = Arms.GetComponent<KatanaStateMachine>();

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
            SetCameraPriorities(aimCam);
        }

        public void FPS()
        {
            SetCameraPriorities(fpsCam);
        }

        public void Charge()
        {
            SetCameraPriorities(chargeCam);
        }

        private void SetCameraPriorities(CinemachineVirtualCamera NewCameraMode)
        {
            activeCam.Priority -= activeCameraPriorityModifier;
            NewCameraMode.Priority += activeCameraPriorityModifier;
            activeCam = NewCameraMode;
            pov = activeCam.GetCinemachineComponent<CinemachinePOV>();

        }

        public void SlowCam(bool active)
        {
            if (pov == null) return;

            if (active)
            {
                cachedHorizontalSpeed = pov.m_HorizontalAxis.m_MaxSpeed;
                cachedVerticalSpeed = pov.m_VerticalAxis.m_MaxSpeed;
                pov.m_HorizontalAxis.m_MaxSpeed = katana.camSlowdown;
                pov.m_VerticalAxis.m_MaxSpeed = katana.camSlowdown;
            }
            else if (pov.m_HorizontalAxis.m_MaxSpeed == katana.camSlowdown && pov.m_VerticalAxis.m_MaxSpeed == katana.camSlowdown)
            {
                pov.m_HorizontalAxis.m_MaxSpeed = cachedHorizontalSpeed;
                pov.m_VerticalAxis.m_MaxSpeed = cachedVerticalSpeed;
            }
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
}
