using System.Collections.Generic;
using UnityEngine;

public class BulletImpactEffects : MonoBehaviour
{
    public SurfaceEffectDatabaseSO effectDatabase;

    public void ProcessImpact(string tag, Vector3 position, Quaternion rotation)
    {
        var effect = effectDatabase.GetEffectForTag(tag);
        if (effect != null)
        {
            if (effect.vfxPrefab != null)
                Instantiate(effect.vfxPrefab, position, rotation);

            if (effect.impactSound != null)
                AudioSource.PlayClipAtPoint(effect.impactSound, position);
        }
        else
        {
            Debug.LogWarning($"No surface effect found for tag: {tag}");
        }
    }
}
