using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Burntwax
{
    /// <summary>
    /// Shared execution mechanics for concrete dualBlade slash states. Light and
    /// heavy states own their lifecycle and explicitly choose attack strength.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaSlashStateBase")]
    public abstract class DualBladeSlashStateBase : DualBladeBaseState
    {

        private Vector2 slashDirection;
        private DualBladeAttackIntent bufferedNextAttack;
        private bool hasBufferedNextAttack;
        private float bufferedAttackExpiresAt;

        protected DualBladeSlashStateBase(
            DualBladeStateMachine currentContext,
            DualBladeStateFactory dualBladeStateFactory)
            : base(currentContext, dualBladeStateFactory)
        {
            IsRootState = true;
        }

        protected void EnterSlash(DualBladeAttackStrength attackStrength)
        {
            bufferedNextAttack = default;
            hasBufferedNextAttack = false;
            bufferedAttackExpiresAt = 0f;

            if (Ctx.TryConsumeAttackIntent(out DualBladeAttackIntent intent, out bool isChained))
                slashDirection = intent.ReleaseInput;
            else
            {
                slashDirection = InputManager.Instance.LookInput;
                Debug.LogWarning(
                    "[DualBlade] Slash state entered without a recognized " +
                    "attack intent. Using current look input as a fallback.");
            }

            DualBladeSlashDirection classifiedDirection =
                DualBladeAttackIntent.ClassifyDirection(slashDirection);

            Ctx.BeginBladeAttack(attackStrength, classifiedDirection);
            Ctx.dualBladeDriver.BeginSwing(
                slashDirection,
                $"{attackStrength} Slash");

            Debug.Log(
                $"[DualBlade] {attackStrength} slash started: " +
                $"{classifiedDirection} ({slashDirection.x:F2}, " +
                $"{slashDirection.y:F2}).");

            Ctx.camStateMachine.SlowCam(true);

            // Each direction has its own state, preserving the outgoing
            // clip while blending into the next slash past its preparation.
            Ctx.PlayDualBladeAnimation(DualBladeAnimationType.Slash, isChained: isChained);
            Ctx.isSlashing = true;
            Ctx.PlaySlashFeedback(attackStrength);
        }

        protected void UpdateSlash()
        {
            if (InputManager.Instance.attackIsPressed)
                Ctx.dualBladeDriver.FeedInput(InputManager.Instance.LookInput);

            // Expire before checking transitions, so neither the chain event
            // nor the end of the current attack can consume stale input.
            if (hasBufferedNextAttack
                && Time.unscaledTime > bufferedAttackExpiresAt)
            {
                hasBufferedNextAttack = false;
                bufferedNextAttack = default;
            }

            // Always observe button releases, even while the buffer is full.
            // Extra inputs must not extend the existing request's deadline.
            bool recognized = Ctx.TryRecognizeAttack(
                out DualBladeAttackIntent nextIntent);
            if (!recognized || hasBufferedNextAttack)
                return;

            DualBladeGestureConfig config = Ctx.ActiveDualBladeScriptable != null
                ? Ctx.ActiveDualBladeScriptable.GestureConfig
                : null;
            float duration = config != null ? config.AttackBufferDuration : 0.12f;

            bufferedNextAttack = nextIntent;
            hasBufferedNextAttack = true;
            bufferedAttackExpiresAt = Time.unscaledTime + duration;
        }

        protected void ExitSlash()
        {
            Ctx.CloseDamageWindow();
            Ctx.dualBladeDriver.EndSwing();
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
                    SwitchState(bufferedNextAttack.Strength == DualBladeAttackStrength.Heavy
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
                        == DualBladeAttackStrength.Heavy)
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
