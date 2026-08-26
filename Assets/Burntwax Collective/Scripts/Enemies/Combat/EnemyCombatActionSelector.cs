using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Immutable facts used to evaluate actions for one decision.
    /// </summary>
    public readonly struct EnemyCombatSelectionContext
    {
        public readonly float TargetDistance;
        public readonly float TargetAngle;
        public readonly float CurrentTime;
        public readonly EnemyCapability Capabilities;
        public readonly EnemyCombatActionCategory SupportedCategories;

        public EnemyCombatSelectionContext(
            float targetDistance,
            float targetAngle,
            float currentTime,
            EnemyCapability capabilities,
            EnemyCombatActionCategory supportedCategories)
        {
            TargetDistance = Mathf.Max(0f, targetDistance);
            TargetAngle = Mathf.Clamp(targetAngle, 0f, 180f);
            CurrentTime = currentTime;
            Capabilities = capabilities;
            SupportedCategories = supportedCategories;
        }
    }

    /// <summary>
    /// Stateless eligibility rules plus per-enemy cooldown history.
    ///
    /// The selector knows nothing about human or alien classes. Archetypes and
    /// evolution determine which configs and capabilities reach it. Unsupported
    /// categories are excluded until their state/executor exists.
    /// </summary>
    public sealed class EnemyCombatActionSelector
    {
        private readonly Dictionary<EnemyCombatActionConfig, float>
            nextAvailableTimes = new();
        private readonly List<EnemyCombatActionConfig> topPriorityActions = new();

        public bool TrySelect(
            IReadOnlyList<EnemyCombatActionConfig> actions,
            EnemyCombatSelectionContext context,
            out EnemyCombatActionConfig selectedAction)
        {
            selectedAction = null;
            topPriorityActions.Clear();
            if (actions == null)
                return false;

            int highestPriority = int.MinValue;
            for (int i = 0; i < actions.Count; i++)
            {
                EnemyCombatActionConfig action = actions[i];
                if (!IsEligible(action, context))
                    continue;

                if (action.SelectionPriority > highestPriority)
                {
                    highestPriority = action.SelectionPriority;
                    topPriorityActions.Clear();
                }

                if (action.SelectionPriority == highestPriority)
                    topPriorityActions.Add(action);
            }

            if (topPriorityActions.Count == 0)
                return false;

            selectedAction = SelectWeighted(topPriorityActions);
            return selectedAction != null;
        }

        public void MarkStarted(
            EnemyCombatActionConfig action,
            float startTime)
        {
            if (action == null)
                return;

            nextAvailableTimes[action] = startTime + action.Cooldown;
        }

        public void ClearCooldowns()
        {
            nextAvailableTimes.Clear();
        }

        private bool IsEligible(
            EnemyCombatActionConfig action,
            EnemyCombatSelectionContext context)
        {
            if (action == null
                || action.SelectionWeight <= 0f
                || (context.SupportedCategories & action.Category) == 0
                || (context.Capabilities & action.RequiredCapabilities)
                    != action.RequiredCapabilities
                || context.TargetDistance < action.MinimumRange
                || context.TargetDistance > action.MaximumRange
                || context.TargetAngle > action.MaximumStartAngle)
            {
                return false;
            }

            return !nextAvailableTimes.TryGetValue(
                    action,
                    out float nextAvailableTime)
                || context.CurrentTime >= nextAvailableTime;
        }

        private static EnemyCombatActionConfig SelectWeighted(
            IReadOnlyList<EnemyCombatActionConfig> actions)
        {
            float totalWeight = 0f;
            for (int i = 0; i < actions.Count; i++)
                totalWeight += actions[i].SelectionWeight;

            if (totalWeight <= Mathf.Epsilon)
                return null;

            float roll = Random.value * totalWeight;
            for (int i = 0; i < actions.Count; i++)
            {
                roll -= actions[i].SelectionWeight;
                if (roll <= 0f)
                    return actions[i];
            }

            return actions[actions.Count - 1];
        }
    }
}
