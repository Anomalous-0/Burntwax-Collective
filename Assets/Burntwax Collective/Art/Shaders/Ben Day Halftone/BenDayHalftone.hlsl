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
// pixel position - no time input), used to jitter the contrast sample kernel so
// it doesn't read as a perfectly uniform ring.
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

// Screen-space Ben Day dot: a fixed grid painted on the lens/screen, like a
// film filter, not attached to any object's world position. Call separately
// for the shadow and light side so each can have its own feather (edge
// softness) while sharing the same grid pitch/rotation/size.
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
    float DotRadius,

    float ContrastBorderWidth,
    float ContrastSensitivity,
    float ContrastCurve,

    float ShadowFeather,
    float ShadowMaxStrength,
    float ShadowBaseVisibility,
    float3 ShadowDotColor,

    float LightFeather,
    float LightMaxStrength,
    float LightBaseVisibility,
    float3 LightDotColor,

    float DistanceFadeStart,
    float DistanceFadeEnd,

    out float3 Out)
{
    float pitchPixels = max(DotScale, 1.0);

    float directLight = BenDayDirectLight_float(UV);
    float eyeDepth = BenDayEyeDepth_float(UV);
    float shadowAmount = 1.0 - directLight;

    // Each side gets its own feather (edge softness), same grid otherwise.
    float shadowSoftnessPixels = max(ShadowFeather, 0.0001) * pitchPixels;
    float lightSoftnessPixels = max(LightFeather, 0.0001) * pitchPixels;
    float baseDotShadow = BenDayDotShape_float(UV, pitchPixels, shadowSoftnessPixels, AngleDegrees, DotRadius);
    float baseDotLight = BenDayDotShape_float(UV, pitchPixels, lightSoftnessPixels, AngleDegrees, DotRadius);

    float fadeRange = max(DistanceFadeEnd - DistanceFadeStart, 1e-5);
    float distanceFade = 1.0 - saturate((eyeDepth - DistanceFadeStart) / fadeRange);

    // The whole effect is driven by local contrast, not absolute brightness: flat
    // bright and flat dark surfaces both read as zero. Unlike fwidth() (a single
    // adjacent-pixel derivative), this samples the lighting at four taps out to a
    // real screen-space radius (ContrastBorderWidth) and uses the local max-min
    // range, so the reactive band is actually as wide as that radius. The kernel
    // is rotated/resized per pixel via a cheap hash so it reads as a fuzzy,
    // irregular edge rather than a clean geometric border.
    float2 texel = 1.0 / _ScreenParams.xy;
    float borderPixels = max(ContrastBorderWidth, 0.0);

    float2 pixelCoord = UV * _ScreenParams.xy;
    float hashAngle = BenDayHash(pixelCoord) * BenDayTwoPi;
    float hashRadius = BenDayHash(pixelCoord + 17.0);
    float jitteredBorder = borderPixels * lerp(0.3, 1.9, hashRadius);

    float sinA = sin(hashAngle);
    float cosA = cos(hashAngle);
    float2 dirA = float2(cosA, sinA);
    float2 dirB = float2(-sinA, cosA);

    float2 offsetA = dirA * texel * jitteredBorder;
    float2 offsetB = dirB * texel * jitteredBorder;

    float lightA1 = BenDayDirectLight_float(UV + offsetA);
    float lightA2 = BenDayDirectLight_float(UV - offsetA);
    float lightB1 = BenDayDirectLight_float(UV + offsetB);
    float lightB2 = BenDayDirectLight_float(UV - offsetB);

    float localMax = max(directLight, max(max(lightA1, lightA2), max(lightB1, lightB2)));
    float localMin = min(directLight, min(min(lightA1, lightA2), min(lightB1, lightB2)));
    float contrastRaw = localMax - localMin;
    float contrastNorm = saturate(contrastRaw * ContrastSensitivity);

    float grain = lerp(0.9, 1.1, BenDayHash(pixelCoord + 91.0));
    float baseNoise = lerp(0.85, 1.15, BenDayHash(pixelCoord + 133.0));

    // Ease the raw contrast value through a smoothstep before curving it -
    // smoothstep has zero slope at both ends, so the response fades in and out
    // gradually rather than snapping the instant contrast crosses into
    // detectable range. Reads as a gradient, not a hard on/off line. Shared
    // between both sides since it's about where the edge is, not how each side
    // should react to it.
    float easedContrast = smoothstep(0.0, 1.0, contrastNorm);
    easedContrast = pow(easedContrast, max(ContrastCurve, 0.0001));

    // Flat lit/shadowed regions aren't exactly zero - a very slight base hum of
    // visibility comes through everywhere, itself grained per-pixel. From there
    // each side ramps continuously (no plateau, no snap) up to its own max
    // strength as local contrast increases.
    float shadowIntensity = lerp(saturate(ShadowBaseVisibility) * baseNoise, saturate(ShadowMaxStrength), easedContrast) * distanceFade * grain;
    float lightIntensity = lerp(saturate(LightBaseVisibility) * baseNoise, saturate(LightMaxStrength), easedContrast) * distanceFade * grain;

    // Within that contrast band, split the response by which side of the
    // transition each pixel sits on: the darker side gets shadow dots, the
    // lighter side gets light dots. Composited with a straight lerp to the
    // dot's own color - fully opaque at full strength, not an additive haze.
    float shadowDots = saturate(baseDotShadow * shadowIntensity * shadowAmount);
    float lightDots = saturate(baseDotLight * lightIntensity * directLight);

    float3 color = SceneColor;
    color = lerp(color, ShadowDotColor, shadowDots);
    color = lerp(color, LightDotColor, lightDots);

    Out = saturate(color);
}

#endif
