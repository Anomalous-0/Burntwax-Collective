using UnityEngine;
using UnityEngine.UI;

namespace Burntwax
{
    /// <summary>
    /// Two-layer health presentation. Damage updates the front immediately and
    /// trails the back bar; healing updates the back and trails the front bar.
    /// </summary>
    public sealed class PlayerHealthBarWidget : PlayerResourceWidget
    {
        private enum AnimatedLayer
        {
            None,
            Front,
            Back
        }

        [Header("Images")]
        [SerializeField] private Image frontHealthBar;
        [SerializeField] private Image backHealthBar;

        [Header("Chip Animation")]
        [SerializeField, Min(0.01f)] private float chipDuration = 2f;
        [SerializeField] private Color damageChipColor = Color.red;
        [SerializeField] private Color healingChipColor = Color.cyan;

        private AnimatedLayer animatedLayer;
        private float animationStartFill;
        private float animationTargetFill;
        private float animationElapsed;

        private void Update()
        {
            if (animatedLayer == AnimatedLayer.None)
                return;

            animationElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(
                animationElapsed / Mathf.Max(0.01f, chipDuration));
            float fill = Mathf.Lerp(
                animationStartFill,
                animationTargetFill,
                Mathf.SmoothStep(0f, 1f, progress));

            if (animatedLayer == AnimatedLayer.Front
                && frontHealthBar != null)
            {
                frontHealthBar.fillAmount = fill;
            }
            else if (animatedLayer == AnimatedLayer.Back
                && backHealthBar != null)
            {
                backHealthBar.fillAmount = fill;
            }

            if (progress >= 1f)
                animatedLayer = AnimatedLayer.None;
        }

        protected override void ApplyResourceValue(
            float current,
            float maximum,
            bool isAvailable,
            bool immediate)
        {
            if (frontHealthBar == null || backHealthBar == null)
                return;

            float targetFill = GetNormalizedValue(current, maximum);
            if (immediate)
            {
                frontHealthBar.fillAmount = targetFill;
                backHealthBar.fillAmount = targetFill;
                animatedLayer = AnimatedLayer.None;
                return;
            }

            if (targetFill < frontHealthBar.fillAmount)
            {
                frontHealthBar.fillAmount = targetFill;
                backHealthBar.color = damageChipColor;
                BeginAnimation(
                    AnimatedLayer.Back,
                    backHealthBar.fillAmount,
                    targetFill);
            }
            else if (targetFill > frontHealthBar.fillAmount)
            {
                backHealthBar.fillAmount = targetFill;
                backHealthBar.color = healingChipColor;
                BeginAnimation(
                    AnimatedLayer.Front,
                    frontHealthBar.fillAmount,
                    targetFill);
            }
        }

        private void BeginAnimation(
            AnimatedLayer layer,
            float startFill,
            float targetFill)
        {
            animatedLayer = layer;
            animationStartFill = startFill;
            animationTargetFill = targetFill;
            animationElapsed = 0f;
        }

        private void OnValidate()
        {
            chipDuration = Mathf.Max(0.01f, chipDuration);
        }
    }
}
