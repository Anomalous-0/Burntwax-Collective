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
        private const float LeftSlashBlend = 0f;
        private const float RightSlashBlend = 0.16666667f;
        private const float UpLeftSlashBlend = 0.33333334f;
        private const float UpRightSlashBlend = 0.5f;
        private const float DownLeftSlashBlend = 0.6666667f;
        private const float DownRightSlashBlend = 0.8333333f;
        private const float DownSlashBlend = 1f;

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

            if (Ctx.TryConsumeAttackIntent(out KatanaAttackIntent intent))
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

            // The slash Blend Tree uses exact thresholds as a four-way clip
            // selector. Set direction before entering the slash state.
            Ctx.anim.SetFloat(
                Ctx.slashBlendHash,
                GetSlashBlend(classifiedDirection));
            Ctx.anim.SetBool(Ctx.slashHash, true);

            // Restart the Blend Tree because a chained gameplay slash can
            // begin before the Animator has fully exited the previous one.
            Ctx.anim.Play(Ctx.slashAnimationHash, 0, 0f);
            Ctx.isSlashing = true;
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
            Ctx.anim.SetBool(Ctx.slashHash, false);
            Ctx.ableToSlashInMiddleOfAnimation = false;
        }

        protected void CheckSlashTransitions()
        {
            if (!Ctx.isSlashing && !InputManager.Instance.attackIsPressed)
            {
                if (InputManager.Instance.aimIsPressed)
                {
                    SwitchState(Factory.Parry());
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
                    Ctx.QueueAttackIntent(bufferedNextAttack);

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
                else if (Ctx.IsInitialSlashQualified)
                {
                    SwitchState(Factory.HeavyCharge());
                }
                else if (InputPrioritySorter.Instance.AimIsPriority())
                {
                    SwitchState(Factory.Parry());
                }
            }
        }

        private static float GetSlashBlend(
            KatanaSlashDirection direction)
        {
            switch (direction)
            {
                case KatanaSlashDirection.Left:
                    return LeftSlashBlend;
                case KatanaSlashDirection.Right:
                    return RightSlashBlend;
                case KatanaSlashDirection.UpLeft:
                    return UpLeftSlashBlend;
                case KatanaSlashDirection.UpRight:
                    return UpRightSlashBlend;
                case KatanaSlashDirection.DownLeft:
                    return DownLeftSlashBlend;
                case KatanaSlashDirection.DownRight:
                    return DownRightSlashBlend;
                case KatanaSlashDirection.Down:
                    return DownSlashBlend;
                default:
                    return RightSlashBlend;
            }
        }
    }
}
