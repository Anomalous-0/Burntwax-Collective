using UnityEngine;

namespace Burntwax
{
    [CreateAssetMenu(
        fileName = "DualBlade Impact Config",
        menuName = "DualBlade/Impact Config",
        order = 2)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaImpactConfig")]
    public sealed class DualBladeImpactConfig : ScriptableObject
    {
        [Header("Light Attack Impulse")]
        [Min(0f)] public float lightAwayImpulse = 4f;
        [Min(0f)] public float lightDirectionalImpulse = 3f;

        [Header("Heavy Attack Impulse")]
        [Min(0f)] public float heavyAwayImpulse = 8f;
        [Min(0f)] public float heavyDirectionalImpulse = 6f;

        public Vector3 GetImpulse(
            DualBladeAttackStrength attackStrength,
            DualBladeSlashDirection slashDirection,
            Transform attacker,
            Vector3 hitPoint)
        {
            if (attacker == null)
                return Vector3.zero;

            bool isHeavy = attackStrength == DualBladeAttackStrength.Heavy;
            float awayImpulse = isHeavy
                ? heavyAwayImpulse
                : lightAwayImpulse;
            float directionalImpulse = isHeavy
                ? heavyDirectionalImpulse
                : lightDirectionalImpulse;

            Vector3 awayFromAttacker = Vector3.ProjectOnPlane(
                hitPoint - attacker.position,
                Vector3.up);
            if (awayFromAttacker.sqrMagnitude <= Mathf.Epsilon)
            {
                awayFromAttacker = Vector3.ProjectOnPlane(
                    attacker.forward,
                    Vector3.up);
            }

            awayFromAttacker = awayFromAttacker.sqrMagnitude > Mathf.Epsilon
                ? awayFromAttacker.normalized
                : Vector3.forward;

            Vector3 attackerRight = Vector3.ProjectOnPlane(
                attacker.right,
                Vector3.up);
            attackerRight = attackerRight.sqrMagnitude > Mathf.Epsilon
                ? attackerRight.normalized
                : Vector3.right;

            Vector3 directionalVector = GetDirectionalVector(
                slashDirection,
                attackerRight);

            return awayFromAttacker * awayImpulse
                + directionalVector * directionalImpulse;
        }

        private static Vector3 GetDirectionalVector(
            DualBladeSlashDirection slashDirection,
            Vector3 attackerRight)
        {
            switch (slashDirection)
            {
                case DualBladeSlashDirection.Left:
                    return -attackerRight;
                case DualBladeSlashDirection.Right:
                    return attackerRight;
                case DualBladeSlashDirection.UpLeft:
                    return (-attackerRight + Vector3.up).normalized;
                case DualBladeSlashDirection.UpRight:
                    return (attackerRight + Vector3.up).normalized;
                case DualBladeSlashDirection.DownLeft:
                    return (-attackerRight + Vector3.down).normalized;
                case DualBladeSlashDirection.DownRight:
                    return (attackerRight + Vector3.down).normalized;
                case DualBladeSlashDirection.Down:
                    return Vector3.down;
                default:
                    return Vector3.zero;
            }
        }
    }
}
