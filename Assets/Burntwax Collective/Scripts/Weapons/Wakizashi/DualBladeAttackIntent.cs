using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Immutable result of one recognized dualBlade gesture. Input recognition
    /// creates it; attack states consume it without reinterpreting raw input.
    /// </summary>
    public readonly struct DualBladeAttackIntent
    {

        private readonly bool wasCreated;
        public DualBladeAttackStrength Strength { get; }
        public Vector2 InitialDirection { get; }
        public Vector2 ReleaseInput { get; }
        public DualBladeSlashDirection SlashDirection { get; }

        public bool IsValid => wasCreated && (Strength == DualBladeAttackStrength.Light
        || SlashDirection != DualBladeSlashDirection.None);

        public DualBladeAttackIntent(

            DualBladeAttackStrength strength,
            Vector2 releaseInput,
            Vector2 initialDirection = default)
        {
            wasCreated = true;
            Strength = strength;
            InitialDirection = initialDirection.sqrMagnitude > Mathf.Epsilon
                ? initialDirection.normalized
                : Vector2.zero;
            ReleaseInput = releaseInput;
            SlashDirection = ClassifyDirection(releaseInput);
        }

        public static DualBladeSlashDirection ClassifyDirection(
            Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return DualBladeSlashDirection.None;

            float angle = Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg;
            if (angle < 0f)
                angle += 360f;

            // Choose the nearest available animation direction. There is no
            // straight-up clip, so 90 degrees is the boundary between the two
            // upward diagonals. The straight-down clip is centered on 270
            // degrees and owns the 247.5-292.5 degree sector.
            if (angle <= 22.5f || angle >= 337.5f)
                return DualBladeSlashDirection.Right;
            if (angle <= 90f)
                return DualBladeSlashDirection.UpRight;
            if (angle < 157.5f)
                return DualBladeSlashDirection.UpLeft;
            if (angle <= 202.5f)
                return DualBladeSlashDirection.Left;
            if (angle < 247.5f)
                return DualBladeSlashDirection.DownLeft;
            if (angle <= 292.5f)
                return DualBladeSlashDirection.Down;

            return DualBladeSlashDirection.DownRight;
        }
    }
}
