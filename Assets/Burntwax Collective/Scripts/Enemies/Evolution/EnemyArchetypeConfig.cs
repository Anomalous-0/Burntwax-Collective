using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Immutable starting point for an enemy family. Humans can use this asset
    /// directly; aliens can combine it with population and individual traits.
    /// Never modify this shared asset at runtime.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Enemy Archetype",
        menuName = "Enemies/Evolution/Enemy Archetype",
        order = 0)]
    public sealed class EnemyArchetypeConfig : ScriptableObject
    {
        [Tooltip("Stable identifier used by future save and spawn systems.")]
        [SerializeField] private string stableId;

        [SerializeField] private EnemyRuntimeStats baseStats =
            EnemyRuntimeStats.Default;

        [SerializeField] private EnemyCapability baseCapabilities =
            EnemyCapability.GroundMovement
            | EnemyCapability.MeleeAttack;

        public string StableId => stableId;
        public EnemyRuntimeStats BaseStats => baseStats.Sanitized();
        public EnemyCapability BaseCapabilities => baseCapabilities;

        private void OnValidate()
        {
            baseStats = baseStats.Sanitized();
        }
    }
}
