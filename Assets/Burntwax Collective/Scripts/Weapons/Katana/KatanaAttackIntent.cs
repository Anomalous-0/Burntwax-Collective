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

            float angle = Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg;
            if (angle < 0f)
                angle += 360f;

            // Choose the nearest available animation direction. Horizontal
            // clips are centered on 0/180 degrees; diagonal clips are centered
            // on 45/135/225/315 degrees. Because there are no straight up or
            // down clips, those axes divide their neighboring diagonals.
            if (angle <= 22.5f || angle >= 337.5f)
                return KatanaSlashDirection.Right;
            if (angle <= 90f)
                return KatanaSlashDirection.UpRight;
            if (angle < 157.5f)
                return KatanaSlashDirection.UpLeft;
            if (angle <= 202.5f)
                return KatanaSlashDirection.Left;
            if (angle < 270f)
                return KatanaSlashDirection.DownLeft;

            return KatanaSlashDirection.DownRight;
        }
    }
}
