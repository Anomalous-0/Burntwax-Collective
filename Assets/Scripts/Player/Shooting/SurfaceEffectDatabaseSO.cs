using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceEffectDatabase", menuName = "Effects/SurfaceEffectDatabase", order = 2)]
public class SurfaceEffectDatabaseSO : ScriptableObject
{
    public List<SurfaceEffectSO> surfaceEffects;

    public SurfaceEffectSO GetEffectForTag(string tag)
    {
        return surfaceEffects.Find(effect => effect.surfaceTag == tag);
    }
}
