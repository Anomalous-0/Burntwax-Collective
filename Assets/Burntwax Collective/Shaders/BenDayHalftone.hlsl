#ifndef BENDAY_HALFTONE_INCLUDED
#define BENDAY_HALFTONE_INCLUDED

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
    ShadowDotBrightness = max(ShadowDotBrightness, 0.0);

    // Pixel brightness from the rendered scene.
    float luminance = dot(SceneColor, float3(0.2126, 0.7152, 0.0722));

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

    // Dark area mask.
    float shadowMask = 1.0 - smoothstep(
        ShadowThreshold,
        ShadowThreshold + ShadowFeather,
        luminance
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

    // Use the real scene shadow color, but darken it so the dot is visible.
    float3 shadowDotColor = SceneColor * ShadowDotBrightness;
    color = lerp(color, shadowDotColor, shadowDots);

    // Use the real bright/emissive scene color for glow dots.
    color += SceneColor * lightDots;

    Out = saturate(color);
}

#endif