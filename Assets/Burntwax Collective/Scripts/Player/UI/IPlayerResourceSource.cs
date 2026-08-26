using System;

namespace Burntwax
{
    /// <summary>
    /// Read-only HUD boundary for any player-owned resource. Implementations
    /// remain responsible for their own gameplay, recharge, and persistence.
    /// </summary>
    public interface IPlayerResourceSource
    {
        PlayerResourceDefinition ResourceDefinition { get; }
        float CurrentValue { get; }
        float MaximumValue { get; }
        bool IsResourceAvailable { get; }

        event Action<IPlayerResourceSource> ResourceChanged;
    }
}
