using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Burntwax
{
    /// <summary>
    /// Shared execution mechanics for concrete katana slash states. Light and
    /// heavy states own their lifecycle and explicitly choose attack strength.
    /// </summary>
    public abstract class KatanaSlashStateBase : KatanaBaseState
    {

        private Vector2 slashDirection;
        private KatanaAttackIntent bufferedNextAttack;
        private bool hasBufferedNextAttack;

        protected KatanaSlashStateBase(
            KatanaStateMachine currentContext,
            KatanaStateFactory katanaStateFactory)
            : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }

        protected void EnterSlash(KatanaAttackStrength attackStrength)
        {
            bufferedNextAttack = default;
            hasBufferedNextAttack = false;

            if (Ctx.TryConsumeAttackIntent(out KatanaAttackIntent intent, out bool isChained))
                slashDirection = intent.ReleaseInput;
            else
            {
                slashDirection = InputManager.Instance.LookInput;
                Debug.LogWarning(
                    "[Katana] Slash state entered without a recognized " +
                    "attack intent. Using current look input as a fallback.");
            }

            KatanaSlashDirection classifiedDirection =
                KatanaAttackIntent.ClassifyDirection(slashDirection);

            Ctx.CloseDamageWindow();
            Ctx.SetAttackStrength(attackStrength);
            Ctx.SetSlashDirection(classifiedDirection);
            Ctx.katanaDriver.BeginSwing(
                slashDirection,
                $"{attackStrength} Slash");

            Debug.Log(
                $"[Katana] {attackStrength} slash started: " +
                $"{classifiedDirection} ({slashDirection.x:F2}, " +
                $"{slashDirection.y:F2}).");

            Ctx.camStateMachine.SlowCam(true);

            // Each direction has its own state, preserving the outgoing
            // clip while blending into the next slash past its preparation.
            Ctx.PlayKatanaAnimation(KatanaAnimationType.Slash, isChained: isChained);
            Ctx.isSlashing = true;
            Ctx.PlaySlashFeedback(attackStrength);
        }

        protected void UpdateSlash()
        {
            if (InputManager.Instance.attackIsPressed)
                Ctx.katanaDriver.FeedInput(InputManager.Instance.LookInput);

            if (!hasBufferedNextAttack
                && Ctx.TryRecognizeAttack(
                    out KatanaAttackIntent nextIntent))
            {
                bufferedNextAttack = nextIntent;
                hasBufferedNextAttack = true;
            }
        }

        protected void ExitSlash()
        {
            Ctx.CloseDamageWindow();
            Ctx.katanaDriver.EndSwing();
            Ctx.camStateMachine.SlowCam(false);
            Ctx.ableToSlashInMiddleOfAnimation = false;
            Ctx.isSlashing = false;
        }

        protected void CheckSlashTransitions()
        {
            if (!Ctx.isSlashing)
            {
                if (InputManager.Instance.aimIsPressed)
                {
                    SwitchState(Factory.Parry());
                }
                else if (hasBufferedNextAttack)
                {
                    Ctx.QueueAttackIntent(bufferedNextAttack, isChained: true);
                    SwitchState(bufferedNextAttack.Strength == KatanaAttackStrength.Heavy
                        ? Factory.HeavySlash() : Factory.LightSlash());
                }
                else if (Ctx.IsInitialSlashQualified)
                {
                    SwitchState(Factory.HeavyCharge());
                }
                else if (InputManager.Instance.attackIsPressed)
                {
                    SwitchState(Factory.Ready());
                }
                else
                {
                    SwitchState(Factory.Idle());
                }
            }
            else if (Ctx.ableToSlashInMiddleOfAnimation)
            {
                if (hasBufferedNextAttack)
                {
                    Ctx.QueueAttackIntent(bufferedNextAttack, isChained: true);

                    if (bufferedNextAttack.Strength
                        == KatanaAttackStrength.Heavy)
                    {
                        SwitchState(Factory.HeavySlash());
                    }
                    else
                    {
                        SwitchState(Factory.LightSlash());
                    }
                }
                // Qualification alone is not the next attack: release still
                // means light, and a held reversal still means heavy.
                // Keep sampling in UpdateSlash while this recovery plays.
                // Entering HeavyCharge here flashes its ready pose between
                // otherwise continuous chained slashes. If recovery finishes
                // while the gesture is still held, the branch above enters
                // HeavyCharge normally.
                else if (InputPrioritySorter.Instance.AimIsPriority())
                {
                    SwitchState(Factory.Parry());
                }
            }
        }

    }
}
