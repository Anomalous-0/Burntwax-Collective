using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SurfaceEffectPair
{
    public string tag;
    public GameObject vfxPrefab;
    public AudioClip impactSound;
}

public class BulletImpactEffects : MonoBehaviour
{
    public List<SurfaceEffectPair> surfaceEffects;

    private Dictionary<string, SurfaceEffectPair> effectsDict;

    private void Awake()
    {
        effectsDict = new Dictionary<string, SurfaceEffectPair>();
        foreach (var pair in surfaceEffects)
        {
            if (!effectsDict.ContainsKey(pair.tag))
            {
                effectsDict.Add(pair.tag, pair);
            }
        }
    }

    public void ProcessImpact(string tag, Vector3 position, Quaternion rotation)
    {
        if (effectsDict.TryGetValue(tag, out SurfaceEffectPair effect))
        {
            if (effect.vfxPrefab != null)
            {
                Instantiate(effect.vfxPrefab, position, rotation);
            }

            if (effect.impactSound != null)
            {
                AudioSource.PlayClipAtPoint(effect.impactSound, position);
            }
        }
    }
}
