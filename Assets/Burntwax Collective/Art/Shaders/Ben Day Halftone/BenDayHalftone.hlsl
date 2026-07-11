#ifndef BENDAY_HALFTONE_INCLUDED
#define BENDAY_HALFTONE_INCLUDED

#pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

static const float BenDayTwoPi = 6.283185307;

// Cheap per-pixel pseudo-random value, stable across frames (pure function of
// pixel position - no time input), used to dither the shadow/light tone so dot
// growth doesn't read as a perfectly uniform ring.
float BenDayHash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float BenDayDirectLight_float(float2 uv)
{
#if defined(SHADERGRAPH_PREVIEW)
    return 1.0;
#else
    float rawDepth = SampleSceneDepth(uv);

    #if UNITY_REVERSED_Z
        if (rawDepth <= 0.0001)
            return 1.0;

        float deviceDepth = rawDepth;
    #else
        if (rawDepth >= 0.9999)
            return 1.0;

        float deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
    #endif

    float3 normalWS = SampleSceneNormals(uv);
    float normalLengthSq = dot(normalWS, normalWS);
    if (normalLengthSq <= 0.0001)
        return 1.0;

    normalWS *= rsqrt(normalLengthSq);

    float3 positionWS = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    Light mainLight = GetMainLight(shadowCoord, positionWS, half4(1.0, 1.0, 1.0, 1.0));

    float3 mainLightDirWS = normalize(mainLight.direction);
    float ndotl = saturate(dot(normalWS, mainLightDirWS));

    return saturate(ndotl * mainLight.shadowAttenuation * mainLight.distanceAttenuation);
#endif
}

float BenDayEyeDepth_float(float2 uv)
{
#if defined(SHADERGRAPH_PREVIEW)
    return 0.0;
#else
    float rawDepth = SampleSceneDepth(uv);

    #if UNITY_REVERSED_Z
        if (rawDepth <= 0.0001)
            return 1e6;
    #else
        if (rawDepth >= 0.9999)
            return 1e6;
    #endif

    return LinearEyeDepth(rawDepth, _ZBufferParams);
#endif
}

// Classic rotated-grid Ben Day dot: same distance-field-and-softness approach as
// before, just a circle per cell instead of a wave line - matches the
// hand-authored dot-halftone look already used on prop textures.
float BenDayDotShape_float(float2 uv, float pitchPixels, float softnessPixels, float angleDegrees, float radiusRatio)
{
    float angle = radians(angleDegrees);
    float s = sin(angle);
    float c = cos(angle);

    float aspect = _ScreenParams.x / _ScreenParams.y;
    float2 p = (uv - 0.5) * float2(aspect, 1.0);

    float2 rotated;
    rotated.x = p.x * c - p.y * s;
    rotated.y = p.x * s + p.y * c;

    float2 grid = rotated / pitchPixels;
    float2 cell = frac(grid) - 0.5;
    float distPixels = length(cell) * pitchPixels;

    float radiusPixels = pitchPixels * saturate(radiusRatio);

    return 1.0 - smoothstep(radiusPixels, radiusPixels + softnessPixels, distPixels);
}

void BenDayShadowBloom_float(
    float2 UV,
    float3 SceneColor,

    float DotScale,
    float AngleDegrees,
    float Softness,

    float ShadowThreshold,
    float ShadowFeather,
    float ShadowStrength,
    float ShadowDotBrightness,

    float LightThreshold,
    float LightFeather,
    float LightDotStrength,

    float DistanceFadeStart,
    float DistanceFadeEnd,

    out float3 Out)
{
    DotScale = max(DotScale, 1.0);
    Softness = max(Softness, 0.0001);
    ShadowFeather = max(ShadowFeather, 0.0001);
    LightFeather = max(LightFeather, 0.0001);

    float directLight = BenDayDirectLight_float(UV);
    float eyeDepth = BenDayEyeDepth_float(UV);
    float shadowAmount = 1.0 - directLight;

    float pitchPixels = max(DotScale, 1.0);
    float softnessPixels = max(Softness * pitchPixels, 0.0001);

    float fadeRange = max(DistanceFadeEnd - DistanceFadeStart, 1e-5);
    float distanceFade = 1.0 - saturate((eyeDepth - DistanceFadeStart) / fadeRange);

    // Tiny per-pixel dither on the tone that drives each dot's radius, so the
    // boundary where dots start/stop growing reads as an organic, hand-printed
    // edge instead of a laser-straight ring.
    float2 pixelCoord = UV * _ScreenParams.xy;
    float shadowGrain = (BenDayHash(pixelCoord) - 0.5) * 0.06;
    float lightGrain = (BenDayHash(pixelCoord + 91.0) - 0.5) * 0.06;

    // True halftone: tone is encoded as dot RADIUS, not as fractional opacity.
    // Threshold gates where a tone band starts producing dots at all; Feather is
    // the width of that gate - Feather near 0 snaps dots straight to full size
    // (a hard silhouette edge), a wider Feather eases them in/out gradually
    // ("feathering" the hard outline into softer, partial dots). Strength caps
    // how large the dots grow at maximum tone.
    float shadowTone = smoothstep(ShadowThreshold, ShadowThreshold + ShadowFeather, shadowAmount + shadowGrain);
    float lightTone = smoothstep(LightThreshold, LightThreshold + LightFeather, directLight + lightGrain);

    float shadowRadiusRatio = saturate(shadowTone * ShadowStrength) * distanceFade;
    float lightRadiusRatio = saturate(lightTone * LightDotStrength) * distanceFade;

    // BenDayDotShape_float is 1 inside the dot and 0 outside it, with only a thin
    // Softness-wide antialiased ring in between - so every dot is drawn as solid,
    // opaque ink rather than a translucent haze.
    float shadowDot = BenDayDotShape_float(UV, pitchPixels, softnessPixels, AngleDegrees, shadowRadiusRatio);
    float lightDot = BenDayDotShape_float(UV, pitchPixels, softnessPixels, AngleDegrees, lightRadiusRatio);

    float3 shadowInk = SceneColor * saturate(ShadowDotBrightness);
    float3 lightInk = saturate(lerp(SceneColor, float3(1.0, 1.0, 1.0), 0.8));

    float3 color = SceneColor;
    color = lerp(color, shadowInk, shadowDot);
    color = lerp(color, lightInk, lightDot);

    Out = saturate(color);
}

#endif
