using System;
using Burntwax.Guns;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using Random = UnityEngine.Random;

namespace Burntwax
{

    [CreateAssetMenu(fileName = "Katana Damage Config", menuName = "Katana/Damage Config", order = 1)]
    public class KatanaDamageConfig : ScriptableObject
    {
        public MinMaxCurve damageCurve;

        private void Reset()
        {
            damageCurve.mode = ParticleSystemCurveMode.Curve;
        }

        public int GetDamage(float distance = 0)
        {
            return Mathf.CeilToInt(damageCurve.Evaluate(distance, Random.value));
        }

        
    }
}
