using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Burntwax
{
    /// <summary>
    /// Displays a discrete resource as individually filled cells. The number
    /// of authored Image references controls the visible segment count.
    /// </summary>
    public sealed class SegmentedResourceWidget : PlayerResourceWidget
    {
        [SerializeField] private List<Image> segments = new();
        [SerializeField] private Color filledColor = Color.cyan;
        [SerializeField] private Color emptyColor =
            new Color(0.15f, 0.18f, 0.2f, 0.55f);
        [SerializeField] private bool hideWhenUnavailable = true;

        protected override void ApplyResourceValue(
            float current,
            float maximum,
            bool isAvailable,
            bool immediate)
        {
            if (hideWhenUnavailable && gameObject.activeSelf != isAvailable)
                gameObject.SetActive(isAvailable);

            if (!isAvailable)
                return;

            int filledSegments = Mathf.Clamp(
                Mathf.RoundToInt(current),
                0,
                segments.Count);

            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i] != null)
                    segments[i].color = i < filledSegments
                        ? filledColor
                        : emptyColor;
            }
        }

        protected override void OnSourceUnbound()
        {
            if (hideWhenUnavailable)
                gameObject.SetActive(false);
        }
    }
}
