using UnityEngine;

namespace Burntwax
{
    // Branch A, step 1: stop-sign pose, pulling object(s) toward the hand. The primary latches into
    // Hold; a tap or (with katana equipped) a slash launches the incoming object early.
    public class GloveAttractState : GloveBaseState
    {
        private Rigidbody attractedBody;
        private float initialDistance;
        private Vector3 curveDirection;

        public GloveAttractState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
        }

        public override void EnterState()
        {
            Ctx.PlayGloveAnimation(Ctx.attractHash);
            Ctx.heldObject = Ctx.detectedObject;
            Ctx.IgnoreHeldObjectPlayerCollisions();
            attractedBody = Ctx.heldObject != null
                ? Ctx.heldObject.GetComponent<Rigidbody>()
                : null;

            if (attractedBody != null && Ctx.restPos != null)
            {
                initialDistance = Mathf.Max(
                    Vector3.Distance(attractedBody.position, Ctx.restPos.transform.position),
                    Ctx.RestDistance);

                Camera playerCamera = Camera.main;
                curveDirection = playerCamera != null ? playerCamera.transform.up : Vector3.up;
            }

        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void FixedUpdateState()
        {
            AttractObject();
        }

        public override void ExitState() { }



        public override void CheckSwitchStates()
        {
            if (attractedBody == null || Ctx.restPos == null)
            {
                Ctx.RestoreHeldObjectPlayerCollisions();
                Ctx.heldObject = null;
                SwitchState(Factory.Idle());
                return;
            }

            if (Vector3.Distance(attractedBody.position, Ctx.restPos.transform.position) <=
                Ctx.RestDistance)
            {
                attractedBody.linearVelocity = Vector3.zero;
                attractedBody.angularVelocity = Vector3.zero;
                SwitchState(Factory.Hold());
            }
        }

        public override void InitializeSubState() { }

        private void AttractObject()
        {
            if (attractedBody == null || Ctx.restPos == null)
                return;

            Vector3 toRestPosition = Ctx.restPos.transform.position - attractedBody.position;
            float distance = toRestPosition.magnitude;
            if (distance <= 0f)
                return;

            float travelProgress = 1f - Mathf.Clamp01(distance / initialDistance);
            float curveOffset = Mathf.Sin(travelProgress * Mathf.PI) * Ctx.AttractionCurveHeight;
            Vector3 curvedTarget = Ctx.restPos.transform.position + curveDirection * curveOffset;
            Vector3 displacement = curvedTarget - attractedBody.position;

            Vector3 acceleration =
                displacement * Ctx.AttractionSpringStrength -
                attractedBody.linearVelocity * Ctx.AttractionDamping;
            acceleration = Vector3.ClampMagnitude(acceleration, Ctx.MaxAttractionAcceleration);

            attractedBody.AddForce(acceleration, ForceMode.Acceleration);

            if (attractedBody.linearVelocity.sqrMagnitude >
                Ctx.MaxAttractionSpeed * Ctx.MaxAttractionSpeed)
            {
                attractedBody.linearVelocity =
                    attractedBody.linearVelocity.normalized * Ctx.MaxAttractionSpeed;
            }
        }
    }
}
