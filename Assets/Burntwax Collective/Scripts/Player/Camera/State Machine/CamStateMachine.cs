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

        [Header("Movement FOV")]
        [Tooltip("Degrees added to the normal camera FOV while sprinting.")]
        [SerializeField, Min(0f)] private float sprintFovOffset = 5f;

        [Tooltip("Degrees added to the normal camera FOV while dashing. Dash takes priority over mantle and sprint.")]
        [SerializeField, Min(0f)] private float dashFovOffset = 10f;

        [Tooltip("Degrees added to the normal camera FOV while mantling.")]
        [SerializeField, Min(0f)] private float mantleFovOffset = 7f;

        [Tooltip("How quickly movement FOV eases between normal, sprint, dash, and mantle values.")]
        [SerializeField, Min(0.01f)] private float movementFovSmoothTime = 0.1f;

        private bool sprintFovActive;
        private bool dashFovActive;
        private bool mantleFovActive;
        private float currentMovementFovOffset;
        private float movementFovVelocity;
        private float fpsBaseFov;
        private float aimBaseFov;
        private float chargeBaseFov;

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
        private float currentObjectCatchFovOffset;

        [Header("Combat FOV Kicks")]
        [Tooltip("Degrees briefly added when the katana baseball mechanic successfully launches an object.")]
        [SerializeField, Min(0f)] private float baseballFovKick = 8f;

        [Tooltip("Duration of the baseball zoom-out pulse.")]
        [SerializeField, Min(0.01f)] private float baseballFovDuration = 0.22f;

        [Tooltip("Degrees briefly added when the player confirms a perfect parry.")]
        [SerializeField, Min(0f)] private float perfectParryFovKick = 10f;

        [Tooltip("Duration of the perfect-parry zoom-out pulse.")]
        [SerializeField, Min(0.01f)] private float perfectParryFovDuration = 0.18f;

        [Tooltip("Normalized FOV offset used by baseball and perfect-parry pulses: 0 = normal, 1 = full kick.")]
        [SerializeField] private AnimationCurve combatFovKickCurve =
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.12f, 1f),
                new Keyframe(1f, 0f));

        private Coroutine combatFovKickRoutine;
        private float currentCombatFovOffset;

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
            fpsBaseFov = fpsCam != null ? fpsCam.m_Lens.FieldOfView : 0f;
            aimBaseFov = aimCam != null ? aimCam.m_Lens.FieldOfView : 0f;
            chargeBaseFov = chargeCam != null ? chargeCam.m_Lens.FieldOfView : 0f;
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

        private void LateUpdate()
        {
            UpdateMovementFov();
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
            chargeCam.m_Lens.FieldOfView = fpsBaseFov;
        }

        // Progressive ADS-style zoom for the glove pulse: narrow the charge camera's FOV in step with
        // the charge, easing toward the target (rather than snapping) with a small push past Charge Fov
        // near full charge that settles back exactly at max.
        public void SetChargeZoom(float chargeNormalized)
        {
            if (activeCam != chargeCam)
                Charge();

            chargeNormalized = Mathf.Clamp01(chargeNormalized);

            float targetFov = Mathf.Lerp(fpsBaseFov, chargeFov, chargeNormalized);
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
            chargeCam.m_Lens.FieldOfView = chargeBaseFov;
        }

        public void SetSprintFovActive(bool active)
        {
            sprintFovActive = active;
        }

        public void SetDashFovActive(bool active)
        {
            dashFovActive = active;
        }

        public void SetMantleFovActive(bool active)
        {
            mantleFovActive = active;
        }

        public void PlayBaseballFovKick()
        {
            PlayCombatFovKick(baseballFovKick, baseballFovDuration);
        }

        public void PlayPerfectParryFovKick()
        {
            PlayCombatFovKick(
                perfectParryFovKick,
                perfectParryFovDuration);
        }

        public void PlayObjectCatchFovKick()
        {
            // Glove charging owns the charge camera's FOV.
            if (activeCam == null || activeCam == chargeCam)
                return;

            RestoreObjectCatchFov();
            objectCatchFovRoutine = StartCoroutine(AnimateObjectCatchFov());
        }

        private IEnumerator AnimateObjectCatchFov()
        {
            float elapsed = 0f;

            while (elapsed < objectCatchFovDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime =
                    Mathf.Clamp01(elapsed / objectCatchFovDuration);
                currentObjectCatchFovOffset =
                    objectCatchFovCurve.Evaluate(normalizedTime) *
                    objectCatchFovKick;

                yield return null;
            }

            objectCatchFovRoutine = null;
            currentObjectCatchFovOffset = 0f;
        }

        private void RestoreObjectCatchFov()
        {
            if (objectCatchFovRoutine != null)
            {
                StopCoroutine(objectCatchFovRoutine);
                objectCatchFovRoutine = null;
            }

            currentObjectCatchFovOffset = 0f;
        }

        private void PlayCombatFovKick(float fovKick, float duration)
        {
            // Glove charge owns its camera's intentional zoom-in.
            if (activeCam == null || activeCam == chargeCam
                || fovKick <= 0f || duration <= 0f)
            {
                return;
            }

            RestoreCombatFovKick();
            combatFovKickRoutine = StartCoroutine(
                AnimateCombatFovKick(fovKick, duration));
        }

        private IEnumerator AnimateCombatFovKick(
            float fovKick,
            float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                currentCombatFovOffset =
                    combatFovKickCurve.Evaluate(normalizedTime) * fovKick;

                yield return null;
            }

            combatFovKickRoutine = null;
            currentCombatFovOffset = 0f;
        }

        private void RestoreCombatFovKick()
        {
            if (combatFovKickRoutine != null)
            {
                StopCoroutine(combatFovKickRoutine);
                combatFovKickRoutine = null;
            }

            currentCombatFovOffset = 0f;
        }

        private void OnDisable()
        {
            RestoreObjectCatchFov();
            RestoreCombatFovKick();
            sprintFovActive = false;
            dashFovActive = false;
            mantleFovActive = false;
            currentMovementFovOffset = 0f;
            movementFovVelocity = 0f;
            RestoreBaseFovs();
        }

        private void SetCameraPriorities(CinemachineVirtualCamera NewCameraMode)
        {
            RestoreBaseFov(activeCam);
            activeCam.Priority -= activeCameraPriorityModifier;
            NewCameraMode.Priority += activeCameraPriorityModifier;
            activeCam = NewCameraMode;
            pov = activeCam.GetCinemachineComponent<CinemachinePOV>();

            ApplySharedFov();

        }

        private void UpdateMovementFov()
        {
            float targetOffset = dashFovActive
                ? dashFovOffset
                : mantleFovActive
                    ? mantleFovOffset
                    : sprintFovActive
                        ? sprintFovOffset
                        : 0f;

            currentMovementFovOffset = Mathf.SmoothDamp(
                currentMovementFovOffset,
                targetOffset,
                ref movementFovVelocity,
                movementFovSmoothTime);

            ApplySharedFov();
        }

        private void ApplySharedFov()
        {
            // Glove charge owns its camera and performs an intentional zoom-in.
            if (activeCam == null || activeCam == chargeCam)
                return;

            activeCam.m_Lens.FieldOfView =
                GetBaseFov(activeCam) +
                currentMovementFovOffset +
                currentObjectCatchFovOffset +
                currentCombatFovOffset;
        }

        private float GetBaseFov(CinemachineVirtualCamera camera)
        {
            if (camera == fpsCam)
                return fpsBaseFov;

            if (camera == aimCam)
                return aimBaseFov;

            if (camera == chargeCam)
                return chargeBaseFov;

            return camera != null ? camera.m_Lens.FieldOfView : 0f;
        }

        private void RestoreBaseFov(CinemachineVirtualCamera camera)
        {
            if (camera != null)
                camera.m_Lens.FieldOfView = GetBaseFov(camera);
        }

        private void RestoreBaseFovs()
        {
            RestoreBaseFov(fpsCam);
            RestoreBaseFov(aimCam);
            RestoreBaseFov(chargeCam);
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
