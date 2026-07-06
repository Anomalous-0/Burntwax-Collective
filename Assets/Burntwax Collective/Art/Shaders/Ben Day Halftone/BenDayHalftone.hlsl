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

    out float3 Out)
{
    DotScale = max(DotScale, 1.0);
    Softness = max(Softness, 0.0001);
    ShadowFeather = max(ShadowFeather, 0.0001);
    LightFeather = max(LightFeather, 0.0001);
    ShadowDotBrightness = saturate(ShadowDotBrightness);

    // Pixel brightness from the rendered scene.
    float luminance = dot(SceneColor, float3(0.2126, 0.7152, 0.0722));
    float directLight = BenDayDirectLight_float(UV);
    float shadowAmount = 1.0 - directLight;

    // Screen-space Ben Day dot pattern.
    float angle = radians(AngleDegrees);
    float s = sin(angle);
    float c = cos(angle);

    float aspect = _ScreenParams.x / _ScreenParams.y;
    float2 p = UV - 0.5;

    // Aspect correction so dots stay circular.
    p.x *= aspect;

    // Rotate the dot grid.
    float2 rotated;
    rotated.x = p.x * c - p.y * s;
    rotated.y = p.x * s + p.y * c;

    // Repeating grid.
    float2 grid = rotated * DotScale;
    float2 cell = frac(grid) - 0.5;

    float dist = length(cell);

    // Dot shape.
    float baseDot = 1.0 - smoothstep(0.25, 0.25 + Softness, dist);

    // Dark area mask based on actual sun lighting, not albedo brightness.
    float shadowMask = smoothstep(
        ShadowThreshold,
        ShadowThreshold + ShadowFeather,
        shadowAmount
    );

    // Bright/emissive area mask.
    float lightMask = smoothstep(
        LightThreshold,
        LightThreshold + LightFeather,
        luminance
    );

    float shadowDots = baseDot * shadowMask * saturate(ShadowStrength);
    float lightDots  = baseDot * lightMask  * saturate(LightDotStrength);

    float3 color = SceneColor;

    // Normalize dot visibility across bright and dark local colors.
    // A pure multiply makes dots overpower white surfaces and disappear on darker hues.
    float visibilityCompensation = lerp(0.35, 0.85, 1.0 - luminance);
    float shadowDarkening = (1.0 - ShadowDotBrightness) * visibilityCompensation;
    float3 shadowDotColor = max(SceneColor - shadowDarkening.xxx, 0.0);
    color = lerp(color, shadowDotColor, shadowDots);

    // Use the real bright/emissive scene color for glow dots.
    color += SceneColor * lightDots;

    Out = saturate(color);
}

#endif
