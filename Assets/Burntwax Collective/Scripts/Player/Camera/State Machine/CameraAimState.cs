namespace Burntwax
{
    public class CameraAimState : CameraBaseState
    {

        public CameraAimState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
        : base(currentContext, cameraStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {
            Ctx.crosshair.SetAimCrosshair();
            Ctx.Aim();
            InitializeSubState();
        }

        public override void UpdateState()
        {

            CheckSwitchStates();

        }

        public override void ExitState()
        {
            Ctx.crosshair.ResetAimCrosshair();
        }

        public override void CheckSwitchStates()
        {
            if (InputPrioritySorter.Instance.SprintIsPriority() || !InputManager.Instance.aimIsPressed || InputManager.Instance.gloveEquipped || Ctx.gun.CurrentState == Ctx.gun.states.Reload())
            {
                // Un-zoom the camera if aim is released OR the glove is equipped mid-aim.
                SwitchState(Factory.Fps());
            }
            // else if (Ctx.player.currentState == Ctx.player.states.Wallcharge())
            // {
            //     SwitchState(Factory.Wallcharge());
            // }



        }


        public override void InitializeSubState() { }

    }

}