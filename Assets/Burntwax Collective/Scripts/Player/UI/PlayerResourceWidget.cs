using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Presentation-only base for HUD elements that display one player
    /// resource. Widgets observe values and never mutate gameplay state.
    /// </summary>
    public abstract class PlayerResourceWidget : MonoBehaviour
    {
        public IPlayerResourceSource BoundSource => boundSource;

        private IPlayerResourceSource boundSource;

        public void Bind(IPlayerResourceSource source)
        {
            if (ReferenceEquals(boundSource, source))
            {
                RefreshVisual(true);
                return;
            }

            Unbind();
            boundSource = source;

            if (boundSource == null)
                return;

            boundSource.ResourceChanged += OnResourceChanged;
            RefreshVisual(true);
        }

        public void Unbind()
        {
            if (boundSource == null)
                return;

            boundSource.ResourceChanged -= OnResourceChanged;

            boundSource = null;
            OnSourceUnbound();
        }

        protected abstract void ApplyResourceValue(
            float current,
            float maximum,
            bool isAvailable,
            bool immediate);

        protected virtual void OnSourceUnbound() { }

        protected static float GetNormalizedValue(
            float current,
            float maximum)
        {
            return maximum > Mathf.Epsilon
                ? Mathf.Clamp01(current / maximum)
                : 0f;
        }

        protected virtual void OnDestroy()
        {
            Unbind();
        }

        private void OnResourceChanged(IPlayerResourceSource source)
        {
            if (ReferenceEquals(source, boundSource))
                RefreshVisual(false);
        }

        private void RefreshVisual(bool immediate)
        {
            if (boundSource == null)
                return;

            ApplyResourceValue(
                boundSource.CurrentValue,
                boundSource.MaximumValue,
                boundSource.IsResourceAvailable,
                immediate);
        }
    }
}
