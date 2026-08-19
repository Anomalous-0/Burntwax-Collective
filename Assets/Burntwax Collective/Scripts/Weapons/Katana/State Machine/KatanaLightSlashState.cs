using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Burntwax
{
    public abstract class KatanaSlashState : KatanaBaseState
    {
        private const float LeftSlashBlend = 0f;
        private const float RightSlashBlend = 1f / 3f;
        private const float UpSlashBlend = 2f / 3f;
        private const float DownSlashBlend = 1f;
        private Vector2 slashDirection;
        private KatanaAttackIntent bufferedNextAttack;
        private bool hasBufferedNextAttack;

        protected abstract KatanaAttackStrength AttackStrength { get; }

        protected KatanaSlashState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }


        public override void EnterState()
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
            Ctx.SetAttackStrength(AttackStrength);
            Ctx.SetSlashDirection(classifiedDirection);
            Ctx.katanaDriver.BeginSwing(
                slashDirection,
                $"{AttackStrength} Slash");

            Debug.Log(
                $"[Katana] {AttackStrength} slash started: " +
                $"{classifiedDirection} ({slashDirection.x:F2}, " +
                $"{slashDirection.y:F2}).");

            Ctx.camStateMachine.SlowCam(true);

            // The slash Blend Tree uses exact thresholds as a four-way clip selector.
            // Set the direction before entering the slash state so it starts on the right clip.
            Ctx.anim.SetFloat(
                Ctx.slashBlendHash,
                GetSlashBlend(classifiedDirection));
            Ctx.anim.SetBool(Ctx.slashHash, true);

            // A new gameplay slash can begin before the Animator has fully transitioned out of
            // the previous one. Explicitly restart the Blend Tree so it cannot retain the old
            // clip or normalized playback time.
            Ctx.anim.Play(Ctx.slashAnimationHash, 0, 0f);
            Ctx.isSlashing = true;

            InitializeSubState();
        }

        public override void UpdateState()
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


            CheckSwitchStates();
        }

        public override void ExitState()
        {
            Ctx.CloseDamageWindow();
            Ctx.katanaDriver.EndSwing();
            Ctx.camStateMachine.SlowCam(false);
            Ctx.anim.SetBool(Ctx.slashHash, false);
            Ctx.ableToSlashInMiddleOfAnimation = false;


        }

        public override void CheckSwitchStates()
        {
            if (!Ctx.isSlashing && !InputManager.Instance.attackIsPressed)
            {
                if (InputManager.Instance.aimIsPressed)
                {
                    SwitchState(Factory.Parry());
                }
                else if (InputPrioritySorter.Instance.SprintIsPriority()
                    && Ctx.stateMachine.currentState.CurrentSubstate()
                        != Ctx.stateMachine.states.Idle())
                {
                    SwitchState(Factory.Stow());
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
                        SwitchState(Factory.HeavySlash());
                    else
                        SwitchState(Factory.LightSlash());
                }
                else if (Ctx.IsInitialSlashQualified)
                {
                    SwitchState(Factory.HeavyCharge());
                }
                else if(InputPrioritySorter.Instance.AimIsPriority())
                {
                    SwitchState(Factory.Parry());
                }
            } 

                
            
        }

        public override void InitializeSubState() { }

        private static float GetSlashBlend(KatanaSlashDirection direction)
        {
            switch (direction)
            {
                case KatanaSlashDirection.Left:
                    return LeftSlashBlend;
                case KatanaSlashDirection.Right:
                    return RightSlashBlend;
                case KatanaSlashDirection.Up:
                    return UpSlashBlend;
                case KatanaSlashDirection.Down:
                    return DownSlashBlend;
                default:
                    return RightSlashBlend;
            }
        }
    }

    public sealed class KatanaLightSlashState : KatanaSlashState
    {
        protected override KatanaAttackStrength AttackStrength =>
            KatanaAttackStrength.Light;

        public KatanaLightSlashState(
            KatanaStateMachine currentContext,
            KatanaStateFactory katanaStateFactory)
            : base(currentContext, katanaStateFactory)
        {
        }
    }
}
