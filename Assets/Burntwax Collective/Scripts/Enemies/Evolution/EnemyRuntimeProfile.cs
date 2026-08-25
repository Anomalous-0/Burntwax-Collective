using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Builds the final stats and capabilities for one spawned enemy. This is
    /// the only layer that combines an archetype with evolution traits. Health,
    /// movement, attacks, and state machines consume the resolved result and do
    /// not need to know whether a value came from species, difficulty, or player
    /// observations.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyRuntimeProfile : MonoBehaviour
    {
        [Header("Base")]
        [SerializeField] private EnemyArchetypeConfig archetype;

        [Header("Alien Population Evolution")]
        [SerializeField] private bool inheritPopulationEvolution;
        [SerializeField] private AlienEvolutionDirector evolutionDirector;

        [Tooltip("Leave disabled for normal aliens: population changes affect future spawns. Enable only for an enemy designed to evolve while already alive.")]
        [SerializeField] private bool rebuildWhenPopulationChanges;

        [Header("Per-enemy Variation")]
        [Tooltip("Traits unique to this spawn, applied after population traits.")]
        [SerializeField] private AlienEvolutionTrait[] individualTraits;

        [Header("Difficulty Composition")]
        [Tooltip("Neutral by default. A future difficulty service sets these modifiers independently from alien evolution.")]
        [SerializeField] private EnemyStatModifiers difficultyModifiers;

        [Header("Runtime (read-only)")]
        [SerializeField] private EnemyRuntimeStats resolvedStats =
            EnemyRuntimeStats.Default;
        [SerializeField] private EnemyCapability resolvedCapabilities;
        [SerializeField] private List<string> appliedTraitIds = new();

        public event Action ProfileRebuilt;
        public EnemyRuntimeStats Stats => resolvedStats;
        public EnemyCapability Capabilities => resolvedCapabilities;

        private readonly HashSet<AlienEvolutionTrait> appliedTraits = new();
        private EnemyHealth health;
        private bool hasBuiltProfile;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            ResolveEvolutionDirector();
            RebuildRuntimeProfile();
        }

        private void OnEnable()
        {
            ResolveEvolutionDirector();
            if (evolutionDirector != null
                && rebuildWhenPopulationChanges)
            {
                evolutionDirector.PopulationTraitsChanged +=
                    RebuildRuntimeProfile;
            }
        }

        private void OnDisable()
        {
            if (evolutionDirector != null)
            {
                evolutionDirector.PopulationTraitsChanged -=
                    RebuildRuntimeProfile;
            }
        }

        public bool HasCapability(EnemyCapability capability)
        {
            return (resolvedCapabilities & capability) == capability;
        }

        /// <summary>
        /// Safe rebuild point for spawning or a deliberate mid-combat evolution
        /// sequence. Existing health is not refilled after the initial build.
        /// </summary>
        public void RebuildRuntimeProfile()
        {
            resolvedStats = archetype != null
                ? archetype.BaseStats
                : BuildFallbackStats();
            resolvedCapabilities = archetype != null
                ? archetype.BaseCapabilities
                : EnemyCapability.GroundMovement
                    | EnemyCapability.MeleeAttack;

            appliedTraits.Clear();
            appliedTraitIds.Clear();

            if (inheritPopulationEvolution
                && evolutionDirector != null)
            {
                ApplyTraits(evolutionDirector.ActivePopulationTraits);
            }

            ApplyTraits(individualTraits);
            resolvedStats = resolvedStats.Apply(difficultyModifiers);
            resolvedStats = resolvedStats.Sanitized();

            bool refillHealth = !hasBuiltProfile;
            hasBuiltProfile = true;
            if (health != null)
            {
                health.SetMaximumHealth(
                    resolvedStats.MaximumHealth,
                    refillHealth);
            }

            ProfileRebuilt?.Invoke();
        }

        /// <summary>
        /// Difficulty changes numbers but never population knowledge or evolved
        /// capabilities. Keeping this input separate prevents the two systems
        /// from multiplying their responsibilities together.
        /// </summary>
        public void SetDifficultyModifiers(
            EnemyStatModifiers modifiers)
        {
            difficultyModifiers = modifiers;
            RebuildRuntimeProfile();
        }

        private void ApplyTraits(
            IEnumerable<AlienEvolutionTrait> traits)
        {
            if (traits == null)
                return;

            foreach (AlienEvolutionTrait trait in traits)
            {
                if (trait == null
                    || appliedTraits.Contains(trait)
                    || !trait.CanApply(resolvedCapabilities))
                {
                    continue;
                }

                trait.Apply(
                    ref resolvedStats,
                    ref resolvedCapabilities);
                appliedTraits.Add(trait);
                appliedTraitIds.Add(trait.StableId);
            }
        }

        private EnemyRuntimeStats BuildFallbackStats()
        {
            EnemyRuntimeStats defaults = EnemyRuntimeStats.Default;
            return new EnemyRuntimeStats(
                health != null
                    ? health.MaxHealth
                    : defaults.MaximumHealth,
                defaults.GroundMoveSpeed,
                defaults.SprintSpeed,
                defaults.TurnSpeed,
                defaults.MeleeDamageMultiplier,
                defaults.AttackSpeedMultiplier,
                defaults.StaggerDurationMultiplier);
        }

        private void ResolveEvolutionDirector()
        {
            if (!inheritPopulationEvolution
                || evolutionDirector != null)
            {
                return;
            }

            evolutionDirector = FindAnyObjectByType<
                AlienEvolutionDirector>();
        }
    }
}
