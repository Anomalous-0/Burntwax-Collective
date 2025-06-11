using System.Collections.Generic;
using UnityEngine;

public class BulletImpactEffects : MonoBehaviour
{
    public List<SurfaceEffectSO> surfaceEffects;

    public void ImpactVFX(string tag, Vector3 position, Quaternion rotation)
    {
        var effect = surfaceEffects.Find(effect => effect.surfaceTag == tag);
        if (effect != null) {
            if (effect.vfxPrefab != null)
                Instantiate(effect.vfxPrefab, position, rotation);

           
        }
        else
        {
            Debug.LogWarning($"No surface effect found for tag: {tag}");
        }
    }

    public void ImpactAudio(string tag, Vector3 position, Quaternion rotation) {
        var effect = surfaceEffects.Find(effect => effect.surfaceTag == tag);
        if (effect != null) {   
            if (effect.impactSound != null)
                AudioSource.PlayClipAtPoint(effect.impactSound, position, effect.volume);
        }
        else {
            Debug.LogWarning($"No surface effect found for tag: {tag}");
        }
    }
}
