using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Burntwax
{
    [CreateAssetMenu(
        fileName = "Katana Combat Feedback Config",
        menuName = "Katana/Combat Feedback Config",
        order = 5)]
    public sealed class KatanaCombatFeedbackConfig : ScriptableObject
    {
        [Serializable]
        private sealed class FeedbackProfile
        {
            [Tooltip("One clip is chosen randomly when this cue plays.")]
            [SerializeField] private AudioClip[] audioClips;

            [SerializeField, Range(0f, 1f)] private float volume = 1f;

            [Tooltip("Optional one-shot effect spawned at the supplied cue origin.")]
            [SerializeField] private GameObject vfxPrefab;

            [Tooltip("Keep the spawned effect attached to the moving sword origin.")]
            [SerializeField] private bool attachVfxToOrigin;

            [Tooltip("Seconds before the spawned VFX root is destroyed. Set to 0 when the prefab cleans itself up.")]
            [SerializeField, Min(0f)] private float vfxLifetime = 2f;

            public bool HasPresentation => vfxPrefab != null
                || audioClips != null && audioClips.Length > 0;

            public void Play(
                Vector3 position,
                Quaternion rotation,
                Transform origin,
                AudioSource audioSource)
            {
                if (vfxPrefab != null)
                {
                    Transform parent = attachVfxToOrigin
                        ? origin
                        : null;
                    GameObject vfx = Instantiate(
                        vfxPrefab,
                        position,
                        rotation,
                        parent);

                    if (vfxLifetime > 0f)
                        Destroy(vfx, vfxLifetime);
                }

                AudioClip clip = GetRandomClip(audioClips);
                if (clip == null)
                    return;

                if (audioSource != null)
                    audioSource.PlayOneShot(clip, volume);
                else
                    AudioSource.PlayClipAtPoint(clip, position, volume);
            }

            private static AudioClip GetRandomClip(AudioClip[] clips)
            {
                if (clips == null || clips.Length == 0)
                    return null;

                int startIndex = Random.Range(0, clips.Length);
                for (int offset = 0; offset < clips.Length; offset++)
                {
                    AudioClip clip = clips[
                        (startIndex + offset) % clips.Length];
                    if (clip != null)
                        return clip;
                }

                return null;
            }
        }

        [Header("Slash Swing")]
        [SerializeField] private FeedbackProfile lightSwing = new();
        [SerializeField] private FeedbackProfile heavySwing = new();

        [Header("Attacker Reaction")]
        [Tooltip("Local cue for this katana when its active slash is perfectly parried by another defender.")]
        [SerializeField] private FeedbackProfile attackParried = new();

        // Preserve existing assets. These legacy fields are fallback-only when
        // their corresponding new profile has no assigned presentation.
        [SerializeField, HideInInspector] private float volume = 1f;
        [SerializeField, HideInInspector] private AudioClip[] slashClips;
        [SerializeField, HideInInspector] private AudioClip[] parryClips;

        public void PlaySwing(
            KatanaAttackStrength strength,
            Transform origin,
            AudioSource audioSource)
        {
            FeedbackProfile profile = strength == KatanaAttackStrength.Heavy
                ? heavySwing
                : lightSwing;
            Vector3 position = origin != null
                ? origin.position
                : Vector3.zero;
            Quaternion rotation = origin != null
                ? origin.rotation
                : Quaternion.identity;

            if (profile != null && profile.HasPresentation)
            {
                profile.Play(position, rotation, origin, audioSource);
                return;
            }

            PlayLegacyClip(slashClips, position, audioSource);
        }

        public void PlayAttackParried(
            DamageInfo parriedHit,
            AudioSource audioSource)
        {
            Vector3 normal = parriedHit.Normal.sqrMagnitude > Mathf.Epsilon
                ? parriedHit.Normal.normalized
                : parriedHit.Direction.sqrMagnitude > Mathf.Epsilon
                    ? -parriedHit.Direction.normalized
                    : Vector3.up;
            Vector3 position = parriedHit.Point + normal * 0.01f;
            Quaternion rotation = Quaternion.LookRotation(normal);

            if (attackParried != null && attackParried.HasPresentation)
            {
                attackParried.Play(
                    position,
                    rotation,
                    null,
                    audioSource);
                return;
            }

            PlayLegacyClip(parryClips, position, audioSource);
        }

        private void PlayLegacyClip(
            AudioClip[] clips,
            Vector3 position,
            AudioSource audioSource)
        {
            if (clips == null || clips.Length == 0)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null)
                return;

            if (audioSource != null)
                audioSource.PlayOneShot(clip, volume);
            else
                AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
            slashClips ??= Array.Empty<AudioClip>();
            parryClips ??= Array.Empty<AudioClip>();
        }
    }
}
