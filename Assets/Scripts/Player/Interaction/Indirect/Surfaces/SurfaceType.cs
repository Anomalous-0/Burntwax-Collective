using UnityEngine;

[CreateAssetMenu(fileName = "NewSurfaceType", menuName = "Game/Surface Type")]
public class SurfaceType : ScriptableObject
{
    public AudioClip impactSound; // Impact sound
    public GameObject impactVFX;  // Vfx to instantiate
}