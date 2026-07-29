using UnityEngine;
using UnityEngine.Rendering;

namespace Burntwax
{
    /// <summary>
    /// Builds the glove's plasma-ball blast: an emissive sphere with a point light and an energy
    /// trail, plus the <see cref="GloveBlastProjectile"/> that carries the payload. One shared builder
    /// so the runtime fallback (when no prefab is wired) and the editor "bake to prefab" menu produce
    /// the exact same object — no drift between them.
    /// </summary>
    public static class GloveBlastVisual
    {
        public static readonly Color PlasmaColor = new Color(0.25f, 0.85f, 1f, 1f);

        public static GloveBlastProjectile Create(Vector3 position, Material sharedMaterial = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "GloveBlast";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.5f;

            // The projectile spawns its own concentric trigger fields; the visible ball must not
            // physically collide, so drop the primitive's collider.
            Collider visibleCollider = go.GetComponent<Collider>();
            if (visibleCollider != null)
                SafeDestroy(visibleCollider);

            Material material = sharedMaterial != null ? sharedMaterial : CreatePlasmaMaterial();

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // Glow, so it reads as live energy against dark geometry.
            Light glow = go.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = PlasmaColor;
            glow.intensity = 4f;
            glow.range = 6f;

            // Energy streak trailing the shot.
            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = 0.45f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.sharedMaterial = material;
            trail.startColor = PlasmaColor;
            trail.endColor = new Color(PlasmaColor.r, PlasmaColor.g, PlasmaColor.b, 0f);

            GloveBlastProjectile projectile = go.AddComponent<GloveBlastProjectile>();

            // RequireComponent added the Rigidbody; make sure the asset/instance starts weightless
            // (the projectile also enforces this in Awake for the runtime path).
            Rigidbody body = go.GetComponent<Rigidbody>();
            if (body != null)
                body.useGravity = false;

            return projectile;
        }

        public static Material CreatePlasmaMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader) { name = "GloveBlast Plasma" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", PlasmaColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", PlasmaColor);

            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", PlasmaColor * 4f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            return material;
        }

        // Works whether called at runtime (play mode) or from the editor baker (edit mode).
        private static void SafeDestroy(Object target)
        {
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
