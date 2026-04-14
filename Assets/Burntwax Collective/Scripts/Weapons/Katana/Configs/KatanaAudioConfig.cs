using System;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Burntwax
{

    [CreateAssetMenu(fileName = "KatanaAudio Config", menuName = "Katana/Audio Config", order = 5)]
    public class KatanaAudioConfig : ScriptableObject
    {
        [Range(0, 1f)] public float volume = 1f;
        public AudioClip[] slashClips;
        public AudioClip[] parryClips;

        public void PlaySlashClip(AudioSource audioSource)
        {
            audioSource.PlayOneShot(slashClips[Random.Range(0, slashClips.Length)], volume);
        }

        public void PlayParryClip(AudioSource audioSource)
        {
            audioSource.PlayOneShot(parryClips[Random.Range(0, parryClips.Length)], volume);
        }


    }

}
