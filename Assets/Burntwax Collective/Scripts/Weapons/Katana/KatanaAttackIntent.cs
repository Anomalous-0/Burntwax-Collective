using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Immutable result of one recognized katana gesture. Input recognition
    /// creates it; attack states consume it without reinterpreting raw input.
    /// </summary>
    public readonly struct KatanaAttackIntent
    {
        public KatanaAttackStrength Strength { get; }
        public Vector2 InitialDirection { get; }
        public Vector2 ReleaseInput { get; }
        public KatanaSlashDirection SlashDirection { get; }

        public bool IsValid => SlashDirection != KatanaSlashDirection.None;

        public KatanaAttackIntent(
            KatanaAttackStrength strength,
            Vector2 releaseInput,
            Vector2 initialDirection = default)
        {
            Strength = strength;
            InitialDirection = initialDirection.sqrMagnitude > Mathf.Epsilon
                ? initialDirection.normalized
                : Vector2.zero;
            ReleaseInput = releaseInput;
            SlashDirection = ClassifyDirection(releaseInput);
        }

        public static KatanaSlashDirection ClassifyDirection(
            Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return KatanaSlashDirection.None;

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x < 0f
                    ? KatanaSlashDirection.Left
                    : KatanaSlashDirection.Right;
            }

            return direction.y > 0f
                ? KatanaSlashDirection.Up
                : KatanaSlashDirection.Down;
        }
    }
}
