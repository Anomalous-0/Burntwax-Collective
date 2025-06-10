using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceEffect", menuName = "Effects/SurfaceEffect", order = 1)]
public class SurfaceEffectSO : ScriptableObject
{
    public string surfaceTag;
    public GameObject vfxPrefab;
    public AudioClip impactSound;
}

