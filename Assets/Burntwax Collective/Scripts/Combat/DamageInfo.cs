using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Runtime description of one attempted hit. It contains facts about the hit,
    /// but does not decide how a target reacts to those facts.
    /// </summary>
    public readonly struct DamageInfo
    {
        public float Amount { get; }
        public DamageType Type { get; }

        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public Vector3 Direction { get; }

        public GameObject Source { get; }
        public GameObject Instigator { get; }

        public DamageInfo(
            float amount,
            DamageType type,
            Vector3 point,
            Vector3 normal,
            Vector3 direction,
            GameObject source,
            GameObject instigator)
        {
            Amount = Mathf.Max(0f, amount);
            Type = type;
            Point = point;
            Normal = normal.sqrMagnitude > 0f ? normal.normalized : Vector3.zero;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
            Source = source;
            Instigator = instigator;
        }
    }
}
