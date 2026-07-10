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

// Rotated-grid wave shape: rows of a sine-wave line replace the original circular
// dot. Same distance-field-and-softness approach as the dots, so each row's stroke
// only covers a slice of its pitch band (never a solid fill).
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

    float ShadowThreshold,
    float ShadowFeather,
    float ShadowStrength,
    float ShadowBrightness,

    float LightThreshold,
    float LightFeather,
    float LightStrength,
    float DistanceFadeStart,
    float DistanceFadeEnd,

    out float3 Out)
{
    WaveScale = max(WaveScale, 1.0);
    Softness = max(Softness, 0.0001);
    ShadowFeather = max(ShadowFeather, 0.0001);
    LightFeather = max(LightFeather, 0.0001);
    ShadowBrightness = saturate(ShadowBrightness);

    // Pixel brightness from the rendered scene.
    float luminance = dot(SceneColor, float3(0.2126, 0.7152, 0.0722));
    float directLight = BenDayDirectLight_float(UV);
    float eyeDepth = BenDayEyeDepth_float(UV);
    float shadowAmount = 1.0 - directLight;

    // Dark area mask based on actual sun lighting, not albedo brightness.
    float shadowMask = smoothstep(
        ShadowThreshold,
        ShadowThreshold + ShadowFeather,
        shadowAmount
    );

    // Bright area mask based on actual direct lighting, not scene color brightness.
    float lightMask = smoothstep(
        LightThreshold,
        LightThreshold + LightFeather,
        directLight
    );

    float pitchPixels = max(WaveScale, 1.0);
    float softnessPixels = max(Softness * pitchPixels, 0.0001);
    float baseWave = BenDayWaveShape_float(UV, pitchPixels, softnessPixels, AngleDegrees, WaveAmplitude, WaveFrequency, WaveThickness);

    float shadowWaves = baseWave * shadowMask * saturate(ShadowStrength);
    float lightWaves = baseWave * lightMask * saturate(LightStrength);

    float fadeRange = max(DistanceFadeEnd - DistanceFadeStart, 1e-5);
    float distanceFade = 1.0 - saturate((eyeDepth - DistanceFadeStart) / fadeRange);
    shadowWaves *= distanceFade;
    lightWaves *= distanceFade;

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
