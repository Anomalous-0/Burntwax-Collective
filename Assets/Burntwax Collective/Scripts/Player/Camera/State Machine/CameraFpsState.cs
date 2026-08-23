namespace Burntwax
{
    public class CameraFpsState : CameraBaseState
    {

        public CameraFpsState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
        : base(currentContext, cameraStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {
            Ctx.FPS();
            InitializeSubState();
        }

        public override void UpdateState()
        {

            CheckSwitchStates();

        }

        public override void ExitState()
        {

        }

        public override void CheckSwitchStates()
        {
            if (InputPrioritySorter.Instance.AimIsPriority()
            && Ctx.gun.CurrentState != Ctx.gun.states.Reload()
            && Ctx.gun.CurrentState.CurrentSubstate() != Ctx.gun.states.Shoot()
            && Ctx.arsenal.currentMode == Arsenal.WeaponMode.Guns)
            {
                SwitchState(Factory.Aim());
            }
            else if (InputManager.Instance.gloveCharging)
            {
                // Glove pulse charge zooms in like ADS, using the charge camera.
                SwitchState(Factory.GloveCharge());
            }

            // else if (Ctx.player.currentState == Ctx.player.states.Wallcharge() )
            // {
            //     SwitchState(Factory.Wallcharge());
            // }
        }

        public override void InitializeSubState() { }

    }
}