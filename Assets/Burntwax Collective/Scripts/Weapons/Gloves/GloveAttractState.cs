using UnityEngine;

namespace Burntwax
{
    // Branch A, step 1: stop-sign pose, pulling object(s) toward the hand. The primary latches into
    // Hold; a tap or (with katana equipped) a slash launches the incoming object early.
    public class GloveAttractState : GloveBaseState
    {
        private Rigidbody attractedBody;

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
            if (toRestPosition.sqrMagnitude > 0f)
                attractedBody.AddForce(
                    toRestPosition.normalized * Ctx.AttractionForce,
                    ForceMode.Force);
        }
    }
}
