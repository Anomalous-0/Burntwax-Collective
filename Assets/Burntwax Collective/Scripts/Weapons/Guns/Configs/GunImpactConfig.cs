using UnityEngine;

namespace Burntwax
{
    [CreateAssetMenu(
        fileName = "Gun Impact Config",
        menuName = "Guns/Impact Config",
        order = 2)]
    public sealed class GunImpactConfig : ScriptableObject
    {
        [Header("Bullet Impulse")]
        [Min(0f)] public float closeRangeImpulse = 5f;
        [Min(0f)] public float longRangeImpulse = 5f;

        [Header("Distance Falloff")]
        [Min(0f)] public float falloffStartDistance;
        [Min(0f)] public float falloffEndDistance = 20f;

        public float GetImpulse(float distance)
        {
            if (falloffEndDistance <= falloffStartDistance)
                return closeRangeImpulse;

            float falloff = Mathf.InverseLerp(
                falloffStartDistance,
                falloffEndDistance,
                Mathf.Max(0f, distance));

            return Mathf.Lerp(
                closeRangeImpulse,
                longRangeImpulse,
                falloff);
        }
    }
}
