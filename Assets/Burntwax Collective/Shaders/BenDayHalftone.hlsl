#ifndef BENDAY_HALFTONE_INCLUDED
#define BENDAY_HALFTONE_INCLUDED

void BenDayHalftone_float(
    float2 UV,
    float3 SceneColor,
    float DotScale,
    float AngleDegrees,
    float Softness,
    float Strength,
    float3 InkColor,
    out float3 Out)
{
    DotScale = max(DotScale, 1.0);
    Softness = max(Softness, 0.0001);

    // Convert scene color to brightness.
    float luminance = saturate(dot(SceneColor, float3(0.2126, 0.7152, 0.0722)));

    // Rotation angle.
    float angle = radians(AngleDegrees);
    float s = sin(angle);
    float c = cos(angle);

    // Screen-space UVs centered around 0.
    float aspect = _ScreenParams.x / _ScreenParams.y;
    float2 p = UV - 0.5;

    // Aspect correction so dots stay circular.
    p.x *= aspect;

    // Rotate dot pattern.
    float2 rotated;
    rotated.x = p.x * c - p.y * s;
    rotated.y = p.x * s + p.y * c;

    // Make repeating grid.
    float2 grid = rotated * DotScale;
    float2 cell = frac(grid) - 0.5;

    // Distance from center of each grid cell.
    float dist = length(cell);

    // Dark parts of the scene get bigger dots.
    // Bright parts get smaller dots.
    float radius = lerp(0.48, 0.03, luminance);

    // Smooth dot edge.
    float dotMask = 1.0 - smoothstep(radius, radius + Softness, dist);

    // Keep original scene color between dots.
    // Only the dots become InkColor.
    float3 halftone = lerp(SceneColor, InkColor, dotMask);

    // Strength controls how much of the effect is visible.
    Out = lerp(SceneColor, halftone, saturate(Strength));
}

#endif