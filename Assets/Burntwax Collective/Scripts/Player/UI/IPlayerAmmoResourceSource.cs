namespace Burntwax
{
    /// <summary>
    /// Compound resource contract used by an ammo widget. The generic values
    /// represent the active clip; these fields add the reserve display.
    /// </summary>
    public interface IPlayerAmmoResourceSource : IPlayerResourceSource
    {
        int ClipAmmo { get; }
        int ClipCapacity { get; }
        int ReserveAmmo { get; }
        int ReserveCapacity { get; }
    }
}
