using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Shared authored settings for explosions. Breakable barrels, grenades, rockets,
    /// and other sources can reuse the same configuration without sharing runtime state.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Explosion Config",
        menuName = "Combat/Explosion Config")]
    public sealed class ExplosionConfig : ScriptableObject
    {
        [Header("Range")]
        [SerializeField, Min(0.01f)] private float radius = 6f;

        [Header("Damage")]
        [SerializeField, Min(0f)] private float maximumDamage = 50f;
        [Tooltip("X is normalized distance from the center; Y multiplies maximum damage.")]
        [SerializeField] private AnimationCurve damageFalloff =
            AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("Physical Impulse")]
        [SerializeField, Min(0f)] private float maximumImpulse = 10f;
        [Tooltip("X is normalized distance from the center; Y multiplies maximum impulse.")]
        [SerializeField] private AnimationCurve impulseFalloff =
            AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [Tooltip("Adds an upward component to the outward impulse direction.")]
        [SerializeField, Min(0f)] private float upwardBias = 0.15f;

        [Header("Filtering")]
        [SerializeField] private LayerMask affectedLayers = ~0;
        [SerializeField] private bool requireLineOfSight = true;
        [Tooltip("Layers capable of blocking an explosion when line of sight is required.")]
        [SerializeField] private LayerMask obstructionLayers = ~0;

        [Header("Presentation")]
        [SerializeField] private GameObject vfxPrefab;
        [SerializeField] private AudioClip[] explosionSounds;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [Tooltip("Seconds before the spawned VFX root is cleaned up. Set to 0 if the prefab cleans itself up.")]
        [SerializeField, Min(0f)] private float vfxLifetime = 5f;

        public float Radius => radius;
        public float MaximumDamage => maximumDamage;
        public float MaximumImpulse => maximumImpulse;
        public float UpwardBias => upwardBias;
        public LayerMask AffectedLayers => affectedLayers;
        public bool RequireLineOfSight => requireLineOfSight;
        public LayerMask ObstructionLayers => obstructionLayers;
        public GameObject VfxPrefab => vfxPrefab;
        public AudioClip[] ExplosionSounds => explosionSounds;
        public float Volume => volume;
        public float VfxLifetime => vfxLifetime;

        public float EvaluateDamage(float normalizedDistance)
        {
            return maximumDamage * EvaluateFalloff(
                damageFalloff,
                normalizedDistance);
        }

        public float EvaluateImpulse(float normalizedDistance)
        {
            return maximumImpulse * EvaluateFalloff(
                impulseFalloff,
                normalizedDistance);
        }

        private static float EvaluateFalloff(
            AnimationCurve curve,
            float normalizedDistance)
        {
            if (curve == null)
                return 0f;

            return Mathf.Max(0f, curve.Evaluate(
                Mathf.Clamp01(normalizedDistance)));
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0.01f, radius);
            maximumDamage = Mathf.Max(0f, maximumDamage);
            maximumImpulse = Mathf.Max(0f, maximumImpulse);
            upwardBias = Mathf.Max(0f, upwardBias);
            volume = Mathf.Clamp01(volume);
            vfxLifetime = Mathf.Max(0f, vfxLifetime);

            damageFalloff ??= AnimationCurve.Linear(0f, 1f, 1f, 0f);
            impulseFalloff ??= AnimationCurve.Linear(0f, 1f, 1f, 0f);
            explosionSounds ??= System.Array.Empty<AudioClip>();
        }
    }
}
