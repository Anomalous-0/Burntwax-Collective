using Debug = UnityEngine.Debug;

namespace Burntwax
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaReadyState")]
    public class DualBladeReadyState : DualBladeBaseState
    {
        public DualBladeReadyState(DualBladeStateMachine currentContext, DualBladeStateFactory dualBladeStateFactory)
        : base(currentContext, dualBladeStateFactory)
        {
            IsRootState = true;
        }


        public override void EnterState()
        {
            Debug.Log("Ready state");

            Ctx.ResetGestureRecognition();

            Ctx.camStateMachine.SlowCam(true);
            Ctx.PlayDualBladeAnimation(DualBladeAnimationType.Ready);


            InitializeSubState();
        }

        public override void UpdateState()
        {





            CheckSwitchStates();
        }

        public override void ExitState()
        {

            Ctx.camStateMachine.SlowCam(false);


        }

        public override void CheckSwitchStates()
        {
            if (Ctx.TryRecognizeAttack(out DualBladeAttackIntent intent))
            {
                Ctx.QueueAttackIntent(intent);

                if (intent.Strength == DualBladeAttackStrength.Heavy)
                    SwitchState(Factory.HeavySlash());
                else
                    SwitchState(Factory.LightSlash());
            }
            else if (Ctx.IsInitialSlashQualified)
            {
                SwitchState(Factory.HeavyCharge());
            }
            else if (InputManager.Instance.aimIsPressed)
            {
                SwitchState(Factory.Parry());
            }
            else if (!InputManager.Instance.aimIsPressed && !InputManager.Instance.attackIsPressed)
            {
                SwitchState(Factory.Idle());
            }

        }

        public override void InitializeSubState() { }
    }
}
