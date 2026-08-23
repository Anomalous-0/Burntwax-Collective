namespace Burntwax
{
    // Glove pulse ADS: while the plasma blast is charging, ride the charge camera and narrow its FOV
    // in step with the charge, stabilising once it is full. Mirrors CameraAimState — same root-state
    // shape and vcam swap — but driven by the glove charge input flags instead of aim, and it drives
    // the FOV every frame so the zoom tracks the charge. Returns to Fps the moment charging ends.
    public class CameraGloveChargeState : CameraBaseState
    {

        public CameraGloveChargeState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
        : base(currentContext, cameraStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {
            // Take over the charge camera at the normal FOV; UpdateState eases the zoom in from there.
            Ctx.BeginChargeZoom();
            InitializeSubState();
        }

        public override void UpdateState()
        {
            Ctx.SetChargeZoom(InputManager.Instance.gloveChargeNormalized);
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            Ctx.ResetChargeZoom();
        }

        public override void CheckSwitchStates()
        {
            if (!InputManager.Instance.gloveCharging)
            {
                SwitchState(Factory.Fps());
            }
        }

        public override void InitializeSubState() { }

    }
}
