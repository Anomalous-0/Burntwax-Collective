using System.Collections.Generic;
using UnityEngine;

public class BulletImpactEffects : MonoBehaviour
{

    public static BulletImpactEffects Instance;
    public List<SurfaceEffectSO> surfaceEffects;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (Instance != null)
        {
            
            Destroy(gameObject);
        }
        Instance = this;
    }

    public void ImpactVFX(string tag, Vector3 position, Quaternion rotation)
    {
        var effect = surfaceEffects.Find(effect => effect.surfaceTag == tag);
        if (effect != null)
        {
            if (effect.vfxPrefab != null)
                Instantiate(effect.vfxPrefab, position, rotation);


        }
        else
        {
            Debug.LogWarning($"No surface effect found for tag: {tag}");
        }
    }

    public void ImpactAudio(string tag, Vector3 position, Quaternion rotation)
    {
        var effect = surfaceEffects.Find(effect => effect.surfaceTag == tag);
        if (effect != null)
        {
            if (effect.impactSounds != null && effect.impactSounds.Count > 0)
                AudioSource.PlayClipAtPoint(effect.impactSounds[Random.Range(0, effect.impactSounds.Count)], position, effect.volume);
        }
        else
        {
            Debug.LogWarning($"No surface effect found for tag: {tag}");
        }
    }
}
