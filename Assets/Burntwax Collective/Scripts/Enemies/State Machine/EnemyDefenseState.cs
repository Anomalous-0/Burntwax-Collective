using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Action-state wrapper around EnemyDefense. It starts and ends defensive
    /// ownership, while animation events or fallback timing control the short
    /// perfect-parry window inside the longer normal guard.
    /// </summary>
    public sealed class EnemyDefenseState : EnemyBaseState
    {
        private EnemyDefenseActionConfig queuedAction;
        private EnemyDefenseActionConfig selectedAction;
        private bool defenseStarted;
        private bool actionComplete;
        private bool fallbackParryOpened;
        private bool fallbackParryClosed;
        private float defenseStartTime;

        public override string DebugName => "Defense";

        public EnemyDefenseState(
            EnemyStateMachine context,
            EnemyStateFactory factory)
            : base(context, factory, EnemyStateRegion.Action)
        {
        }

        public void QueueDefense(EnemyDefenseActionConfig action)
        {
            queuedAction = action;
        }

        public override void EnterState()
        {
            // Factory states are cached, so every per-use field must reset.
            selectedAction = queuedAction;
            queuedAction = null;
            actionComplete = false;
            fallbackParryOpened = false;
            fallbackParryClosed = false;
            defenseStartTime = Time.time;

            EnemyDefense defense = Ctx.Defense;
            defenseStarted = defense != null
                && selectedAction != null
                && defense.BeginDefense(selectedAction.DefenseConfig);

            if (!defenseStarted)
                return;

            Ctx.MarkCombatActionStarted(selectedAction);
            Ctx.PlayCombatActionAnimation(selectedAction);
        }

        public override void UpdateState()
        {
            if (!defenseStarted || Ctx.Defense == null)
            {
                SwitchState(Factory.FaceTarget());
                return;
            }

            if (selectedAction.FaceTargetWhileDefending)
                Ctx.RequestFacingTarget();

            if (selectedAction.UseFallbackTiming)
                TickFallbackTiming();

            CheckSwitchStates();
        }

        public override void FixedUpdateState() { }

        public override void ExitState()
        {
            Ctx.Defense?.EndDefense();
            selectedAction = null;
            defenseStarted = false;
            actionComplete = false;
            fallbackParryOpened = false;
            fallbackParryClosed = false;
        }

        public override void CheckSwitchStates()
        {
            EnemyDefense defense = Ctx.Defense;
            if (actionComplete
                || defense == null
                || !defense.IsDefending)
            {
                SwitchState(Factory.FaceTarget());
            }
        }

        public override void InitializeSubState() { }

        public void OpenParryWindow()
        {
            if (defenseStarted)
                Ctx.Defense?.OpenParryWindow();
        }

        public void CloseParryWindow()
        {
            if (defenseStarted)
                Ctx.Defense?.CloseParryWindow();
        }

        public void FinishDefense()
        {
            if (!defenseStarted)
                return;

            Ctx.Defense?.EndDefense();
            actionComplete = true;
        }

        private void TickFallbackTiming()
        {
            float elapsed = Time.time - defenseStartTime;
            float parryStart = selectedAction.ParryWindowStart;
            float parryEnd = parryStart
                + selectedAction.ParryWindowDuration;

            if (!fallbackParryOpened
                && selectedAction.ParryWindowDuration > 0f
                && elapsed >= parryStart)
            {
                fallbackParryOpened = true;
                OpenParryWindow();
            }

            if (fallbackParryOpened
                && !fallbackParryClosed
                && elapsed >= parryEnd)
            {
                fallbackParryClosed = true;
                CloseParryWindow();
            }

            if (elapsed >= selectedAction.DefenseDuration)
                FinishDefense();
        }
    }
}
