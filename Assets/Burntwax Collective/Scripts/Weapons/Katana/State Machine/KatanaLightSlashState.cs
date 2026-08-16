using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Burntwax
{
    public class KatanaLightSlashState : KatanaBaseState
    {
        private const float LeftSlashBlend = 0f;
        private const float RightSlashBlend = 1f / 3f;
        private const float UpSlashBlend = 2f / 3f;
        private const float DownSlashBlend = 1f;
        private Vector2 slashDirection;

        public KatanaLightSlashState(KatanaStateMachine currentContext, KatanaStateFactory katanaStateFactory)
        : base(currentContext, katanaStateFactory)
        {
            IsRootState = true;
        }


        public override void EnterState()
        {
            slashDirection = InputManager.Instance.slashStartDirection;

            InputManager.Instance.slash = false;
            Ctx.CloseDamageWindow();
            Ctx.SetAttackStrength(KatanaAttackStrength.Light);
            Ctx.katanaDriver.BeginSwing(slashDirection, "Slash");

            Ctx.camStateMachine.SlowCam(true);

            // The slash Blend Tree uses exact thresholds as a four-way clip selector.
            // Set the direction before entering the slash state so it starts on the right clip.
            Ctx.anim.SetFloat(Ctx.slashBlendHash, GetSlashBlend(slashDirection));
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
            if (InputManager.Instance.isTrackingSlash)
            {
                Ctx.katanaDriver.FeedInput(InputManager.Instance.LookInput);
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
                if (InputManager.Instance.parry)
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
                if(InputManager.Instance.slash && InputManager.Instance.slashStartDirection != slashDirection)
                {
                    SwitchState(Factory.LightSlash());   
                }
    
                else if(InputPrioritySorter.Instance.AimIsPriority())
                {
                    SwitchState(Factory.Parry());
                }
            } 

                
            
        }

        public override void InitializeSubState() { }

        private static float GetSlashBlend(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                return direction.x < 0f ? LeftSlashBlend : RightSlashBlend;

            return direction.y > 0f ? UpSlashBlend : DownSlashBlend;
        }
    }
}
