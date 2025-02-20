using UnityEngine;

[CreateAssetMenu(fileName = "NewSurfaceType", menuName = "Game/Surface Type")]
public class SurfaceType : ScriptableObject {
    
    [Range(0,1f)] public float volume = 1f;
    public AudioClip impactSound; // Impact sound
    
    public ParticleSystem impactVFX;  // Vfx to instantiate
}