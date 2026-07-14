namespace Burntwax
{
    // Root state: the glove is put away (left hand free for the weapon). A single tap of F equips it.
    public class GloveUnequippedState : GloveBaseState
    {
        public GloveUnequippedState(GloveStateMachine currentContext, GloveStateFactory gloveStateFactory)
        : base(currentContext, gloveStateFactory)
        {
            IsRootState = true;
        }

        public override void EnterState()
        {
            Ctx.SetEquipped(false);
            InitializeSubState();
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (InputManager.Instance == null)
                return;

            if (InputManager.Instance.gloveTap || !Ctx.CanAttractObject && InputManager.Instance.gloveDoubleTap)
            {
                SwitchState(Factory.Equipped());
            }
            else if(InputManager.Instance.gloveDoubleTap && Ctx.CanAttractObject)
            {
                Ctx.EnterAttract = true;
                SwitchState(Factory.Equipped());

            }
            else if(InputManager.Instance.gloveCharging)
            {
                Ctx.EnterCharge = true;
                SwitchState(Factory.Equipped());
            }

        }

        public override void InitializeSubState() { }
    }
}
