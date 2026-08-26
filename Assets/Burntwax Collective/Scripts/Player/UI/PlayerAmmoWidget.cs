using TMPro;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Presentation-only clip/reserve text for the active gun.
    /// </summary>
    public sealed class PlayerAmmoWidget : PlayerResourceWidget
    {
        [SerializeField] private TextMeshProUGUI ammoText;

        protected override void ApplyResourceValue(
            float current,
            float maximum,
            bool isAvailable,
            bool immediate)
        {
            if (ammoText == null)
                return;

            if (!isAvailable)
            {
                ammoText.SetText(string.Empty);
                return;
            }

            if (BoundSource is IPlayerAmmoResourceSource ammo)
            {
                ammoText.SetText(
                    $"{ammo.ClipAmmo} / {ammo.ReserveAmmo}");
                return;
            }

            ammoText.SetText(
                $"{Mathf.RoundToInt(current)} / "
                    + $"{Mathf.RoundToInt(maximum)}");
        }

        protected override void OnSourceUnbound()
        {
            if (ammoText != null)
                ammoText.SetText(string.Empty);
        }
    }
}
