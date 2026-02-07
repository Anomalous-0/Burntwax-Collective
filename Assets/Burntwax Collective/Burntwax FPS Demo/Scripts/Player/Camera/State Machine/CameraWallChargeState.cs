#if CINEMACHINE
namespace Burntwax
{
    public class CameraWallChargeState : CameraBaseState
    {

        public CameraWallChargeState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
        : base(currentContext, cameraStateFactory)
        {
            IsRootState = true;

        }

        public override void EnterState()
        {
            // Debug.Log("Entered Wall Charge State");
            Ctx.Charge();
            InitializeSubState();
        }

        public override void UpdateState()
        {

            CheckSwitchStates();

        }

        public override void ExitState()
        {
            // Debug.Log("Exited Wall Charge State");
            Ctx.crosshair.ResetAimCrosshair();
        }

        public override void CheckSwitchStates()
        {
            if (Ctx.player.currentState != Ctx.player.states.Wallcharge())
            {

                SwitchState(Factory.Fps());
            }
            if (InputPrioritySorter.Instance.AimIsPriority() && Ctx.gun.CurrentState != Ctx.gun.states.Reload() && Ctx.gun.CurrentState.CurrentSubstate() != Ctx.gun.states.Shoot())
            {
                SwitchState(Factory.Aim());
            }


        }


        public override void InitializeSubState() { }

    }
}

#endif