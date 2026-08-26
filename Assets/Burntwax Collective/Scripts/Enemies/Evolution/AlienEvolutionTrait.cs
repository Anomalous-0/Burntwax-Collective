using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// One modular alien adaptation. Traits change data and capabilities; they
    /// do not contain AI branches. A trait that grants RangedAttack makes that
    /// action available to the action-selection system built later.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Alien Evolution Trait",
        menuName = "Enemies/Evolution/Alien Trait",
        order = 1)]
    public sealed class AlienEvolutionTrait : ScriptableObject
    {
        [Tooltip("Stable identifier saved instead of a direct asset reference.")]
        [SerializeField] private string stableId;

        [TextArea]
        [SerializeField] private string designerDescription;

        [Tooltip("Budget cost used by the future evolution selector.")]
        [SerializeField, Min(0)] private int evolutionCost = 1;

        [Header("Eligibility")]
        [Tooltip("Capabilities that must already exist before this trait can be applied.")]
        [SerializeField] private EnemyCapability requiredCapabilities;

        [Tooltip("If any of these capabilities exist, the trait is incompatible.")]
        [SerializeField] private EnemyCapability incompatibleCapabilities;

        [Header("Effects")]
        [SerializeField] private EnemyStatModifiers statModifiers;
        [SerializeField] private EnemyCapability grantedCapabilities;
        [SerializeField] private EnemyCapability removedCapabilities;

        [Tooltip("Actions learned when this trait is applied. Their own capability requirements must also be satisfied before selection.")]
        [SerializeField] private EnemyCombatActionConfig[] grantedCombatActions;

        public string StableId => stableId;
        public int EvolutionCost => evolutionCost;
        public EnemyCombatActionConfig[] GrantedCombatActions =>
            grantedCombatActions;

        public bool CanApply(EnemyCapability currentCapabilities)
        {
            bool hasRequirements = (currentCapabilities
                & requiredCapabilities) == requiredCapabilities;
            bool hasIncompatibility = (currentCapabilities
                & incompatibleCapabilities) != 0;
            return hasRequirements && !hasIncompatibility;
        }

        public void Apply(
            ref EnemyRuntimeStats stats,
            ref EnemyCapability capabilities)
        {
            stats = stats.Apply(statModifiers);
            capabilities |= grantedCapabilities;
            capabilities &= ~removedCapabilities;
        }

        private void OnValidate()
        {
            evolutionCost = Mathf.Max(0, evolutionCost);
        }
    }
}
