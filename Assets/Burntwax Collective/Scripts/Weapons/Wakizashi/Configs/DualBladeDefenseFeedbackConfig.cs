using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Burntwax
{
    /// <summary>
    /// Presentation data for the player's three dualBlade-defense outcomes.
    /// Gameplay rules remain in DualBladeParryConfig and DualBladeParryDefense.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DualBlade Defense Feedback Config",
        menuName = "DualBlade/Defense Feedback Config",
        order = 6)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaDefenseFeedbackConfig")]
    public sealed class DualBladeDefenseFeedbackConfig : ScriptableObject
    {
        [Serializable]
        private sealed class FeedbackProfile
        {
            [Tooltip("VFX spawned at the incoming hit point.")]
            [SerializeField] private GameObject vfxPrefab;

            [Tooltip("One clip is chosen randomly whenever this outcome occurs.")]
            [SerializeField] private AudioClip[] audioClips;

            [SerializeField, Range(0f, 1f)] private float volume = 1f;

            public void Play(
                Vector3 position,
                Quaternion rotation,
                AudioSource audioSource)
            {
                if (vfxPrefab != null)
                    Instantiate(vfxPrefab, position, rotation);

                PlayAudio(position, audioSource);
            }

            public void PlayAudio(
                Vector3 position,
                AudioSource audioSource)
            {
                if (audioClips == null || audioClips.Length == 0)
                    return;

                AudioClip clip = audioClips[
                    Random.Range(0, audioClips.Length)];
                if (clip == null)
                    return;

                if (audioSource != null)
                    audioSource.PlayOneShot(clip, volume);
                else
                    AudioSource.PlayClipAtPoint(clip, position, volume);
            }
        }

        [Header("Defense Outcomes")]
        [SerializeField] private FeedbackProfile blocked = new();
        [SerializeField] private FeedbackProfile perfectParry = new();
        [SerializeField] private FeedbackProfile guardBroken = new();

        public void Play(
            HitDefenseOutcome outcome,
            DamageInfo incomingHit,
            AudioSource audioSource)
        {
            FeedbackProfile profile = GetProfile(outcome);
            if (profile == null)
                return;

            Vector3 normal = incomingHit.Normal;
            if (normal.sqrMagnitude <= Mathf.Epsilon)
            {
                normal = incomingHit.Direction.sqrMagnitude > Mathf.Epsilon
                    ? -incomingHit.Direction
                    : Vector3.up;
            }

            Vector3 position = incomingHit.Point + normal * 0.01f;
            Quaternion rotation = Quaternion.LookRotation(normal);
            profile.Play(position, rotation, audioSource);
        }

        public void PlayAudio(
            HitDefenseOutcome outcome,
            Vector3 position,
            AudioSource audioSource)
        {
            FeedbackProfile profile = GetProfile(outcome);
            profile?.PlayAudio(position, audioSource);
        }

        private FeedbackProfile GetProfile(HitDefenseOutcome outcome)
        {
            switch (outcome)
            {
                case HitDefenseOutcome.Blocked:
                    return blocked;
                case HitDefenseOutcome.Parried:
                    return perfectParry;
                case HitDefenseOutcome.GuardBroken:
                    return guardBroken;
                default:
                    return null;
            }
        }
    }
}
