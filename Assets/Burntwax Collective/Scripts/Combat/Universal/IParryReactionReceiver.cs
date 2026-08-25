using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Optional reaction point for an attack source whose hit was perfectly
    /// parried. The attacker decides what being parried means for that attack.
    /// </summary>
    public interface IParryReactionReceiver
    {
        void OnAttackParried(
            DamageInfo parriedHit,
            Collider defenderCollider);
    }
}
