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
        Energy = 1 << 4,

        // Presentation-only category for small Rigidbody bumps. It is never sent to
        // IDamageable targets and exists so minor audio can use its own surface profiles.
        MinorImpact = 1 << 5
    }
}
