using UnityEngine;

[CreateAssetMenu(fileName = "NewSurfaceType", menuName = "Game/Surface Type")]
public class SurfaceType : ScriptableObject
{
    public AudioClip impactSound; // The sound to play on impact
    public GameObject impactVFX;  // The particle system or VFX prefab to instantiate
}