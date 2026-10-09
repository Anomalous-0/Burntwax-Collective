using Debug = UnityEngine.Debug;

namespace Burntwax
{
    public class KatanaReadyState : KatanaBaseState
    {
        public KatanaReadyState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }


        public override void EnterState()
        {
            Debug.Log("Ready state");

            Ctx.ResetGestureRecognition();

            Ctx.camStateMachine.SlowCam(true);
            Ctx.PlayKatanaAnimation(KatanaAnimationType.Ready);


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
            if (Ctx.TryRecognizeAttack(out KatanaAttackIntent intent))
            {
                Ctx.QueueAttackIntent(intent);

                if (intent.Strength == KatanaAttackStrength.Heavy)
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
