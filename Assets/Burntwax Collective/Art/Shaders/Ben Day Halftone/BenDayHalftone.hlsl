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

// Rotated-grid wave shape: rows of a sine-wave line replace the circular dot.
// Same distance-field-and-softness approach, so each row's stroke only covers a
// slice of its pitch band (never a solid fill).
float BenDayWaveShape_float(float2 uv, float pitchPixels, float softnessPixels, float angleDegrees, float amplitudeRatio, float frequency, float thicknessRatio)
{
    float angle = radians(angleDegrees);
    float s = sin(angle);
    float c = cos(angle);

    float aspect = _ScreenParams.x / _ScreenParams.y;
    float2 p = (uv - 0.5) * float2(aspect, 1.0);

    float2 rotated;
    rotated.x = p.x * c - p.y * s;
    rotated.y = p.x * s + p.y * c;

    float rowIndex = floor(rotated.y / pitchPixels + 0.5);
    float rowCenterY = rowIndex * pitchPixels;

    float amplitude = pitchPixels * saturate(amplitudeRatio) * 0.5;
    float freq = max(frequency, 0.01);
    float waveY = amplitude * sin(rotated.x / pitchPixels * BenDayTwoPi * freq);

    float distFromWave = abs(rotated.y - rowCenterY - waveY);
    float lineHalfWidth = pitchPixels * saturate(thicknessRatio) * 0.5;

    return 1.0 - smoothstep(lineHalfWidth, lineHalfWidth + softnessPixels, distFromWave);
}

void BenDayShadowBloom_float(
    float2 UV,
    float3 SceneColor,

    float WaveScale,
    float AngleDegrees,
    float Softness,
    float WaveAmplitude,
    float WaveFrequency,
    float WaveThickness,

    float ContrastBorderWidth,
    float ContrastSensitivity,
    float ContrastCurve,
    float ContrastStrength,
    float BaseVisibility,
    float ShadowBrightness,

    float DistanceFadeStart,
    float DistanceFadeEnd,

    out float3 Out)
{
    WaveScale = max(WaveScale, 1.0);
    Softness = max(Softness, 0.0001);
    ShadowBrightness = saturate(ShadowBrightness);

    // Pixel brightness from the rendered scene.
    float luminance = dot(SceneColor, float3(0.2126, 0.7152, 0.0722));
    float directLight = BenDayDirectLight_float(UV);
    float eyeDepth = BenDayEyeDepth_float(UV);
    float shadowAmount = 1.0 - directLight;

    float pitchPixels = max(WaveScale, 1.0);
    float softnessPixels = max(Softness * pitchPixels, 0.0001);
    float baseWave = BenDayWaveShape_float(UV, pitchPixels, softnessPixels, AngleDegrees, WaveAmplitude, WaveFrequency, WaveThickness);

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

    float grain = lerp(0.6, 1.4, BenDayHash(pixelCoord + 91.0));

    float baseNoise = lerp(0.4, 1.6, BenDayHash(pixelCoord + 133.0));
    float baseVisibility = saturate(BaseVisibility) * baseNoise;

    // Ease the raw contrast value through a smoothstep before curving it -
    // smoothstep has zero slope at both ends, so the response fades in and out
    // gradually rather than snapping the instant contrast crosses into
    // detectable range. Reads as a gradient, not a hard on/off line.
    float easedContrast = smoothstep(0.0, 1.0, contrastNorm);
    easedContrast = pow(easedContrast, max(ContrastCurve, 0.0001));
    float contrastIntensity = lerp(baseVisibility, saturate(ContrastStrength), easedContrast) * distanceFade * grain;

    float shadowWaves = baseWave * contrastIntensity * shadowAmount;
    float lightWaves = baseWave * contrastIntensity * directLight;

    float3 color = SceneColor;

    // Normalize wave visibility across bright and dark local colors.
    // A pure multiply makes waves overpower white surfaces and disappear on darker hues.
    float visibilityCompensation = lerp(0.35, 0.85, 1.0 - luminance);
    float shadowDarkening = (1.0 - ShadowBrightness) * visibilityCompensation;
    float3 shadowWaveColor = max(SceneColor - shadowDarkening.xxx, 0.0);
    color = lerp(color, shadowWaveColor, shadowWaves);

    // Normalize light-wave visibility across local colors.
    // A pure additive scene-color term makes white surfaces blow out while darker hues barely show.
    float lightVisibilityCompensation = lerp(1.35, 0.8, luminance);
    float3 lightWaveColor = saturate(SceneColor * lightVisibilityCompensation);
    color += lightWaveColor * lightWaves;

    Out = saturate(color);
}

#endif
