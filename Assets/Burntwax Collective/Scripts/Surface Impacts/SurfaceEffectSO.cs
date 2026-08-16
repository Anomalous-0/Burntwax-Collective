using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{

    [CreateAssetMenu(fileName = "SurfaceEffect", menuName = "Effects/SurfaceEffect", order = 1)]
    public class SurfaceEffectSO : ScriptableObject
    {
        [Header("Impact Match")]
        public string surfaceTag;

        [Tooltip("Damage categories that can use this surface effect.")]
        public DamageType damageTypes = DamageType.Bullet;

        [Header("Effects")]
        public GameObject vfxPrefab;
        public List<AudioClip> impactSounds;

        [Header("Optional Directional VFX")]
        [Tooltip(
            "Aligns the VFX's local Y axis with the hit direction projected "
                + "onto the surface. Enable this for directional slash decals.")]
        public bool alignVfxToHitDirection;

        [Tooltip(
            "Additional roll around the surface normal. Use 90 degrees when "
                + "the decal artwork's long axis is local X instead of local Y.")]
        [Range(-180f, 180f)]
        public float hitDirectionRotationOffset;

        [Header("Audio Settings")]
        [Tooltip("Impact sound volume")]
        [Range(0f, 1f)] public float volume = 1f;
    }


}
