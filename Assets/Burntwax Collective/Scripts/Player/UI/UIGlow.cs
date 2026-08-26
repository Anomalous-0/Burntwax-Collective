using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Burntwax
{
    /// <summary>
    /// Adds an overlay-canvas-safe halo around a UI Graphic. The effect copies
    /// the existing UI mesh in a small radial pattern, so it does not depend on
    /// HDR colors, post-processing, or a camera-rendered Canvas.
    /// </summary>
    [AddComponentMenu("UI/Effects/UI Glow")]
    [RequireComponent(typeof(Graphic))]
    public sealed class UIGlow : BaseMeshEffect
    {
        [Tooltip("When enabled, the glow follows the Graphic's current color. This lets depleted resource cells automatically become dimmer.")]
        [SerializeField] private bool useGraphicColor = true;

        [Tooltip("Glow color when Use Graphic Color is disabled. Its alpha always controls glow strength.")]
        [SerializeField] private Color glowColor =
            new Color(0f, 1f, 1f, 0.24f);

        [Tooltip("Outer distance of the halo in Canvas pixels.")]
        [SerializeField, Range(0f, 24f)] private float radius = 5f;

        [Tooltip("Number of directions sampled around each ring. Higher values look smoother but add more UI vertices.")]
        [SerializeField, Range(4, 24)] private int samples = 12;

        [Tooltip("Number of concentric rings used to soften the halo.")]
        [SerializeField, Range(1, 4)] private int rings = 2;

        [Tooltip("Outer-ring strength relative to the inner glow.")]
        [SerializeField, Range(0f, 1f)] private float outerFalloff = 0.2f;

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive()
                || radius <= Mathf.Epsilon
                || glowColor.a <= Mathf.Epsilon
                || vertexHelper.currentVertCount == 0)
            {
                return;
            }

            List<UIVertex> sourceVertices = new();
            vertexHelper.GetUIVertexStream(sourceVertices);

            int directionCount = Mathf.Max(4, samples);
            int ringCount = Mathf.Max(1, rings);
            List<UIVertex> outputVertices = new(
                sourceVertices.Count * (1 + directionCount * ringCount));

            for (int ringIndex = 1;
                ringIndex <= ringCount;
                ringIndex++)
            {
                float ringProgress = ringIndex / (float)ringCount;
                float ringRadius = radius * ringProgress;
                float ringStrength = Mathf.Lerp(
                    1f,
                    outerFalloff,
                    ringProgress);

                for (int sampleIndex = 0;
                    sampleIndex < directionCount;
                    sampleIndex++)
                {
                    float angle = sampleIndex
                        * Mathf.PI
                        * 2f
                        / directionCount;
                    Vector3 offset = new(
                        Mathf.Cos(angle) * ringRadius,
                        Mathf.Sin(angle) * ringRadius,
                        0f);

                    AppendGlowCopy(
                        sourceVertices,
                        outputVertices,
                        offset,
                        ringStrength);
                }
            }

            // The original graphic is added last so it remains crisp and draws
            // over the surrounding halo copies.
            outputVertices.AddRange(sourceVertices);
            vertexHelper.Clear();
            vertexHelper.AddUIVertexTriangleStream(outputVertices);
        }

        private void AppendGlowCopy(
            List<UIVertex> sourceVertices,
            List<UIVertex> outputVertices,
            Vector3 offset,
            float strength)
        {
            for (int i = 0; i < sourceVertices.Count; i++)
            {
                UIVertex vertex = sourceVertices[i];
                vertex.position += offset;

                Color sourceColor = vertex.color;
                Color haloColor = useGraphicColor
                    ? sourceColor
                    : glowColor;
                haloColor.a = sourceColor.a
                    * glowColor.a
                    * strength;
                vertex.color = haloColor;
                outputVertices.Add(vertex);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            samples = Mathf.Clamp(samples, 4, 24);
            rings = Mathf.Clamp(rings, 1, 4);
            graphic?.SetVerticesDirty();
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            graphic?.SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            graphic?.SetVerticesDirty();
            base.OnDisable();
        }
    }
}
