using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Authored set of actions available to an enemy family or prototype.
    /// Shared assets are immutable at runtime; EnemyRuntimeProfile combines this
    /// base list with evolution-granted actions for each spawned enemy.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Enemy Combat Loadout",
        menuName = "Enemies/Combat/Combat Loadout",
        order = 0)]
    public sealed class EnemyCombatLoadout : ScriptableObject
    {
        [SerializeField] private EnemyCombatActionConfig[] actions;

        public IReadOnlyList<EnemyCombatActionConfig> Actions => actions;
    }
}
