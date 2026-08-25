using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Semantic facts alien populations may learn about the player. Recording
    /// observations is deliberately separate from selecting adaptations: later
    /// balancing code can decide how much evidence is required and how quickly
    /// knowledge decays without touching enemy state machines.
    /// </summary>
    public enum AlienCombatObservationType
    {
        PlayerUsedMelee = 0,
        PlayerUsedRanged = 1,
        PlayerPerfectParried = 2,
        PlayerBlocked = 3,
        PlayerThrewObject = 4,
        PlayerUsedEnvironment = 5,
        PlayerFoughtAtLongRange = 6,
        PlayerFoughtAtCloseRange = 7
    }

    /// <summary>
    /// Serializable population knowledge. The array representation is used
    /// instead of a Dictionary so Unity and the future save system can persist
    /// it without custom serialization.
    /// </summary>
    [Serializable]
    public sealed class AlienCombatKnowledge
    {
        [SerializeField] private float[] observationStrengths;

        public void Record(
            AlienCombatObservationType observation,
            float strength,
            float maximumStrength)
        {
            EnsureCapacity();
            int index = (int)observation;
            observationStrengths[index] = Mathf.Clamp(
                observationStrengths[index] + Mathf.Max(0f, strength),
                0f,
                Mathf.Max(0f, maximumStrength));
        }

        public float Get(AlienCombatObservationType observation)
        {
            EnsureCapacity();
            return observationStrengths[(int)observation];
        }

        public void Decay(float amount)
        {
            EnsureCapacity();
            amount = Mathf.Max(0f, amount);
            for (int i = 0; i < observationStrengths.Length; i++)
            {
                observationStrengths[i] = Mathf.Max(
                    0f,
                    observationStrengths[i] - amount);
            }
        }

        private void EnsureCapacity()
        {
            int requiredLength = Enum.GetValues(
                typeof(AlienCombatObservationType)).Length;
            if (observationStrengths != null
                && observationStrengths.Length == requiredLength)
            {
                return;
            }

            float[] resized = new float[requiredLength];
            if (observationStrengths != null)
            {
                Array.Copy(
                    observationStrengths,
                    resized,
                    Mathf.Min(
                        observationStrengths.Length,
                        resized.Length));
            }

            observationStrengths = resized;
        }
    }

    /// <summary>
    /// Population-level bridge between observed player behavior and traits
    /// granted to future alien spawns. Today the active traits are designer
    /// selected. Later an evolution policy will spend a difficulty-controlled
    /// budget based on Knowledge and update this same list.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AlienEvolutionDirector : MonoBehaviour
    {
        [SerializeField] private AlienCombatKnowledge knowledge = new();

        [Tooltip("Traits currently inherited by new aliens in this population.")]
        [SerializeField] private List<AlienEvolutionTrait>
            activePopulationTraits = new();

        [SerializeField, Min(0f)] private float maximumObservationStrength =
            100f;

        public event Action PopulationTraitsChanged;
        public AlienCombatKnowledge Knowledge => knowledge;
        public IReadOnlyList<AlienEvolutionTrait> ActivePopulationTraits =>
            activePopulationTraits;

        public void RecordObservation(
            AlienCombatObservationType observation,
            float strength = 1f)
        {
            knowledge.Record(
                observation,
                strength,
                maximumObservationStrength);
        }

        /// <summary>
        /// Called by a future evolution policy after it chooses a new set. The
        /// director owns population knowledge; individual enemies only receive
        /// the resulting traits through EnemyRuntimeProfile.
        /// </summary>
        public void SetActivePopulationTraits(
            IEnumerable<AlienEvolutionTrait> traits)
        {
            activePopulationTraits.Clear();
            if (traits != null)
            {
                foreach (AlienEvolutionTrait trait in traits)
                {
                    if (trait != null
                        && !activePopulationTraits.Contains(trait))
                    {
                        activePopulationTraits.Add(trait);
                    }
                }
            }

            PopulationTraitsChanged?.Invoke();
        }

        private void OnValidate()
        {
            maximumObservationStrength = Mathf.Max(
                0f,
                maximumObservationStrength);
        }
    }
}
