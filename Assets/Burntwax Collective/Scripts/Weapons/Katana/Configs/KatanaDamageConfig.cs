using UnityEngine;

namespace Burntwax
{

    [CreateAssetMenu(fileName = "Katana Damage Config", menuName = "Katana/Damage Config", order = 1)]
    public class KatanaDamageConfig : ScriptableObject
    {
        [Header("Attack Damage")]
        [Min(0f)] public float lightAttackDamage = 20f;
        [Min(0f)] public float heavyAttackDamage = 40f;

        public float GetDamage(KatanaAttackStrength attackStrength)
        {
            return attackStrength == KatanaAttackStrength.Heavy
                ? heavyAttackDamage
                : lightAttackDamage;
        }
    }
}
