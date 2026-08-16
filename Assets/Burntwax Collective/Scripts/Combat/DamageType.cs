using System;

namespace Burntwax
{
    /// <summary>
    /// Broad categories used by defenses, resistances, reactions, and effects.
    /// Flags allow one hit to have more than one category, such as Energy | Impact.
    /// </summary>
    [Flags]
    public enum DamageType
    {
        None = 0,
        Bullet = 1 << 0,
        Impact = 1 << 1,
        Slash = 1 << 2,
        Explosion = 1 << 3,
        Energy = 1 << 4
    }
}
