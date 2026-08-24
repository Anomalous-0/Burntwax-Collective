using System.Collections;
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

        [Header("Charge Zoom")]
        [Tooltip("FOV the charge camera reaches at full glove charge. Lower = more zoom-in. Match it to the aim camera for an identical ADS feel.")]
        public float chargeFov = 40f;
        [Tooltip("How quickly the FOV eases toward the charge target. Lower = smoother/laggier, higher = snappier.")]
        public float chargeZoomSmoothTime = 0.08f;
        [Tooltip("Degrees the FOV briefly pushes past Charge Fov near full charge before settling back at max. 0 = no overshoot.")]
        public float chargeZoomOvershoot = 4f;
        private float chargeZoomVelocity;

        [Header("Object Catch FOV")]
        [Tooltip("How many degrees the view widens when an attracted object reaches the glove.")]
        [SerializeField, Min(0f)] private float objectCatchFovKick = 8f;

        [Tooltip("Total duration of the widen-and-return animation.")]
        [SerializeField, Min(0.01f)] private float objectCatchFovDuration = 0.25f;

        [Tooltip("Normalized FOV offset over the animation: 0 = normal, 1 = full kick.")]
        [SerializeField] private AnimationCurve objectCatchFovCurve =
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, 0f));

        private Coroutine objectCatchFovRoutine;
        private CinemachineVirtualCamera objectCatchCamera;
        private float objectCatchBaseFov;

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

        // Switch to the charge camera and snap it wide, so the glove pulse zoom always eases in from
        // the normal FOV. Called on enter, mirroring how Aim() takes over the aim camera.
        public void BeginChargeZoom()
        {
            if (activeCam != chargeCam)
                Charge();

            chargeZoomVelocity = 0f;
            chargeCam.m_Lens.FieldOfView = fpsCam.m_Lens.FieldOfView;
        }

        // Progressive ADS-style zoom for the glove pulse: narrow the charge camera's FOV in step with
        // the charge, easing toward the target (rather than snapping) with a small push past Charge Fov
        // near full charge that settles back exactly at max.
        public void SetChargeZoom(float chargeNormalized)
        {
            if (activeCam != chargeCam)
                Charge();

            chargeNormalized = Mathf.Clamp01(chargeNormalized);

            float targetFov = Mathf.Lerp(fpsCam.m_Lens.FieldOfView, chargeFov, chargeNormalized);
            // Peaks near full charge (~0.7) and returns to zero at max, so hitting full charge reads as
            // a small punch-in that settles rather than a hard stop.
            targetFov -= chargeZoomOvershoot *
                Mathf.Sin(chargeNormalized * chargeNormalized * Mathf.PI);

            chargeCam.m_Lens.FieldOfView = Mathf.SmoothDamp(
                chargeCam.m_Lens.FieldOfView, targetFov, ref chargeZoomVelocity, chargeZoomSmoothTime);
        }

        // Restore the charge camera's FOV so the next pulse starts wide.
        public void ResetChargeZoom()
        {
            chargeZoomVelocity = 0f;
            chargeCam.m_Lens.FieldOfView = fpsCam.m_Lens.FieldOfView;
        }

        public void PlayObjectCatchFovKick()
        {
            // Glove charging owns the charge camera's FOV.
            if (activeCam == null || activeCam == chargeCam)
                return;

            RestoreObjectCatchFov();

            objectCatchCamera = activeCam;
            objectCatchBaseFov = objectCatchCamera.m_Lens.FieldOfView;
            objectCatchFovRoutine = StartCoroutine(AnimateObjectCatchFov());
        }

        private IEnumerator AnimateObjectCatchFov()
        {
            float elapsed = 0f;

            while (elapsed < objectCatchFovDuration &&
                   objectCatchCamera != null &&
                   objectCatchCamera == activeCam)
            {
                elapsed += Time.deltaTime;
                float normalizedTime =
                    Mathf.Clamp01(elapsed / objectCatchFovDuration);
                float offset =
                    objectCatchFovCurve.Evaluate(normalizedTime) *
                    objectCatchFovKick;

                objectCatchCamera.m_Lens.FieldOfView =
                    objectCatchBaseFov + offset;

                yield return null;
            }

            RestoreObjectCatchFov();
        }

        private void RestoreObjectCatchFov()
        {
            if (objectCatchFovRoutine != null)
            {
                StopCoroutine(objectCatchFovRoutine);
                objectCatchFovRoutine = null;
            }

            if (objectCatchCamera != null)
                objectCatchCamera.m_Lens.FieldOfView = objectCatchBaseFov;

            objectCatchCamera = null;
        }

        private void OnDisable()
        {
            RestoreObjectCatchFov();
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
