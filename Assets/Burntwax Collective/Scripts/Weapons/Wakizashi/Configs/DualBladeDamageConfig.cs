using UnityEngine;

namespace Burntwax
{

    [CreateAssetMenu(fileName = "DualBlade Damage Config", menuName = "DualBlade/Damage Config", order = 1)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaDamageConfig")]
    public class DualBladeDamageConfig : ScriptableObject
    {
        [Header("Attack Damage")]
        [Min(0f)] public float lightAttackDamage = 20f;
        [Min(0f)] public float heavyAttackDamage = 40f;

        public float GetDamage(DualBladeAttackStrength attackStrength)
        {
            return attackStrength == DualBladeAttackStrength.Heavy
                ? heavyAttackDamage
                : lightAttackDamage;
        }
    }
}
