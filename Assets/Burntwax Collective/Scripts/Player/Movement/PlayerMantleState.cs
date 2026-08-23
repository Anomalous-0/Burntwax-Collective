using UnityEngine;

namespace Burntwax
{
    public class PlayerMantleState : PlayerBaseState
    {
        private PlayerMantleTarget target;
        private Vector3 startPosition;
        private float elapsedTime;
        private bool hasTarget;
        private bool mantleComplete;
        private bool wasKinematic;

        public PlayerMantleState(
            PlayerStateMachine currentContext,
            PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory)
        {
            IsRootState = true;
            UsesSubState = false;
        }

        public void SetTarget(PlayerMantleTarget mantleTarget)
        {
            target = mantleTarget;
            hasTarget = true;
        }

        public override void EnterState()
        {
            mantleComplete = false;
            elapsedTime = 0f;

            if (!hasTarget || Ctx.MantleConfig == null)
            {
                mantleComplete = true;
                return;
            }

            startPosition = Ctx.rb.position;
            Ctx.DisableMovement = true;
            Ctx.SetMantleActive(true);
            wasKinematic = Ctx.rb.isKinematic;
            Ctx.rb.linearVelocity = Vector3.zero;
            Ctx.rb.useGravity = false;
            Ctx.rb.linearDamping = 0f;
            Ctx.rb.isKinematic = true;
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public void FixedUpdateState()
        {
            if (mantleComplete || !hasTarget || Ctx.MantleConfig == null)
                return;

            elapsedTime += Time.fixedDeltaTime;
            float duration = Mathf.Max(0.01f, Ctx.MantleConfig.MantleDuration);
            float progress = Mathf.Clamp01(elapsedTime / duration);
            float curveProgress = Mathf.SmoothStep(0f, 1f, progress);
            Ctx.SetMantleAnimationProgress(progress);
            Vector3 startControl = startPosition
                + Vector3.up * Ctx.MantleConfig.MantleStartCurveHeight;
            Vector3 endControl = target.RigidbodyPosition
                + Vector3.up * Ctx.MantleConfig.MantleEndCurveHeight;
            Vector3 position = EvaluateCubicBezier(
                startPosition,
                startControl,
                endControl,
                target.RigidbodyPosition,
                curveProgress);

            Ctx.rb.MovePosition(position);

            if (progress >= 1f)
                mantleComplete = true;
        }

        public override void ExitState()
        {
            Ctx.DisableMovement = false;
            Ctx.SetMantleActive(false);
            Ctx.rb.isKinematic = wasKinematic;
            Ctx.rb.useGravity = true;
            Ctx.rb.linearDamping = Ctx.GroundDrag;

            if (!Ctx.rb.isKinematic)
                Ctx.rb.linearVelocity = Vector3.zero;

            hasTarget = false;
        }

        public override void CheckSwitchStates()
        {
            if (!mantleComplete)
                return;

            SwitchState(hasTarget ? Factory.Grounded() : Factory.Fall());
        }

        public override void InitializeSubState()
        {
        }

        private static Vector3 EvaluateCubicBezier(
            Vector3 start,
            Vector3 startControl,
            Vector3 endControl,
            Vector3 end,
            float progress)
        {
            float inverseProgress = 1f - progress;

            return inverseProgress * inverseProgress * inverseProgress * start
                + 3f * inverseProgress * inverseProgress * progress * startControl
                + 3f * inverseProgress * progress * progress * endControl
                + progress * progress * progress * end;
        }
    }
}
