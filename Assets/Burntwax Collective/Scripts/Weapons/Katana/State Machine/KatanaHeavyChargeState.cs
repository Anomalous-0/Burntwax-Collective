using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Owns a speed-qualified initial flick. Releasing now performs a light
    /// slash; continuing to hold and reversing performs a heavy slash.
    /// </summary>
    public sealed class KatanaHeavyChargeState : KatanaBaseState
    {
        private bool hasRecognizedAttack;
        private KatanaAttackIntent recognizedAttack;

        public KatanaHeavyChargeState(
            KatanaStateMachine currentContext,
            KatanaStateFactory katanaStateFactory)
            : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            hasRecognizedAttack = false;
            recognizedAttack = default;

            Ctx.camStateMachine.SlowCam(true);
            Ctx.PlayKatanaAnimation(KatanaAnimationType.HeavyCharge, restart: false);
            Debug.Log(
                $"[Katana] Heavy charge started. Initial direction: " +
                $"{Ctx.InitialSlashDirection}.");
            InitializeSubState();
        }

        public override void UpdateState()
        {
            hasRecognizedAttack = Ctx.TryRecognizeAttack(
                out recognizedAttack);

            CheckSwitchStates();
        }

        public override void ExitState()
        {
            Ctx.camStateMachine.SlowCam(false);
        }

        public override void CheckSwitchStates()
        {
            if (hasRecognizedAttack)
            {
                Ctx.QueueAttackIntent(recognizedAttack);

                if (recognizedAttack.Strength == KatanaAttackStrength.Heavy)
                    SwitchState(Factory.HeavySlash());
                else
                    SwitchState(Factory.LightSlash());
            }
            else if (!InputManager.Instance.attackIsPressed)
            {
                Ctx.ResetGestureRecognition();
                SwitchState(Factory.Idle());
            }
            else if (InputManager.Instance.aimIsPressed)
            {
                Ctx.ResetGestureRecognition();
                SwitchState(Factory.Parry());
            }
        }

        public override void InitializeSubState()
        {
        }
    }
}
