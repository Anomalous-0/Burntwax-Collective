Shader "Burntwax/Fullscreen/Ben Day Halftone"
{
    Properties
    {
        _Wave_Scale("Wave Scale", Float) = 128.2
        _Angle_Degrees("Angle Degrees", Float) = 16.5
        _Softness("Softness", Range(0.0, 1.0)) = 0.05
        _Wave_Amplitude("Wave Amplitude", Range(0.0, 1.0)) = 0.6
        _Wave_Frequency("Wave Frequency", Range(0.1, 4.0)) = 1.0
        _Wave_Thickness("Wave Thickness", Range(0.05, 1.0)) = 0.35
        _ReferenceResolution("Reference Resolution", Vector) = (1920, 1080, 0, 0)

        _Contrast_Border_Width("Contrast Border Width (px)", Range(1.0, 32.0)) = 6.0
        _Contrast_Sensitivity("Contrast Sensitivity", Range(0.5, 40.0)) = 12.0
        _Contrast_Curve("Contrast Curve", Range(0.25, 4.0)) = 1.0
        _Contrast_Strength("Contrast Strength", Range(0.0, 1.0)) = 0.85
        _Base_Visibility("Base Visibility", Range(0.0, 0.3)) = 0.06
        _Shadow_Brightness("Shadow Wave Brightness", Range(0.0, 1.0)) = 0.6

        _Fade_Start_Distance("Fade Start Distance", Float) = 0.0
        _Fade_End_Distance("Fade End Distance", Float) = 150.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Ben Day Halftone"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Wave_Scale;
                float _Angle_Degrees;
                float _Softness;
                float _Wave_Amplitude;
                float _Wave_Frequency;
                float _Wave_Thickness;
                float4 _ReferenceResolution;
                float _Contrast_Border_Width;
                float _Contrast_Sensitivity;
                float _Contrast_Curve;
                float _Contrast_Strength;
                float _Base_Visibility;
                float _Shadow_Brightness;
                float _Fade_Start_Distance;
                float _Fade_End_Distance;
            CBUFFER_END

            static const float BenDayTwoPi = 6.283185307;

            // Cheap per-pixel pseudo-random value, stable across frames (pure function
            // of pixel position - no time input), used to jitter the contrast sample
            // kernel so it doesn't read as a perfectly uniform ring.
            float BenDayHash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Minimal stand-in for URP's InputData, only used to satisfy the
            // LIGHT_LOOP_BEGIN macro's Forward+ cluster lookup (needs positionWS +
            // normalizedScreenSpaceUV on a variable literally named "inputData").
            struct BenDayLightLoopInput
            {
                float3 positionWS;
                float2 normalizedScreenSpaceUV;
            };

            // Reconstructs world position + normal for this pixel from depth/normal
            // buffers. Returns false for sky/background pixels.
            bool BenDaySampleSurface(float2 uv, out float3 positionWS, out float3 normalWS)
            {
                positionWS = float3(0, 0, 0);
                normalWS = float3(0, 0, 0);

                float rawDepth = SampleSceneDepth(uv);

                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.0001)
                        return false;

                    float deviceDepth = rawDepth;
                #else
                    if (rawDepth >= 0.9999)
                        return false;

                    float deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif

                float3 n = SampleSceneNormals(uv);
                float normalLengthSq = dot(n, n);
                if (normalLengthSq <= 0.0001)
                    return false;

                normalWS = n * rsqrt(normalLengthSq);
                positionWS = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
                return true;
            }

            // Strongest light hitting this surface across every point/spot light in
            // range (flashlights, muzzle flashes, flickering fixtures), each already
            // accounting for its own shadow map and falloff.
            float BenDayAdditionalLights(float2 screenUV, float3 positionWS, float3 normalWS)
            {
                float strongest = 0.0;

            #if defined(_ADDITIONAL_LIGHTS)
                BenDayLightLoopInput inputData;
                inputData.positionWS = positionWS;
                inputData.normalizedScreenSpaceUV = screenUV;

                uint pixelLightCount = GetAdditionalLightsCount();

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, positionWS, half4(1.0, 1.0, 1.0, 1.0));
                    float3 lightDirWS = normalize(light.direction);
                    float ndotl = saturate(dot(normalWS, lightDirWS));
                    float contribution = saturate(ndotl * light.distanceAttenuation * light.shadowAttenuation);
                    strongest = max(strongest, contribution);
                LIGHT_LOOP_END
            #endif

                return strongest;
            }

            // Same "is this pixel lit" value used for the color compositing below
            // (sun w/ shadow, unshadowed sun fallback, emissive, every additional
            // light), but callable at any uv - used both at the pixel center and at
            // offset taps so contrast can be measured across a real screen-space
            // radius instead of a single-pixel derivative.
            float BenDayComputeDirectLight(float2 uv)
            {
                float3 positionWS, normalWS;
                bool hasSurface = BenDaySampleSurface(uv, positionWS, normalWS);

                float directLightExact = 1.0;
                float directLightApprox = 1.0;
                float additionalLights = 0.0;

                if (hasSurface)
                {
                    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                    Light mainLight = GetMainLight(shadowCoord, positionWS, half4(1.0, 1.0, 1.0, 1.0));
                    float3 mainLightDirWS = normalize(mainLight.direction);
                    float ndotl = saturate(dot(normalWS, mainLightDirWS));
                    directLightExact = saturate(ndotl * mainLight.shadowAttenuation * mainLight.distanceAttenuation);

                    Light mainLightNoShadow = GetMainLight();
                    directLightApprox = saturate(dot(normalWS, normalize(mainLightNoShadow.direction)));

                    additionalLights = BenDayAdditionalLights(uv, positionWS, normalWS);
                }

                float3 tapColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                float tapLuminance = dot(tapColor, float3(0.2126, 0.7152, 0.0722));
                float emissiveBoost = smoothstep(0.65, 0.95, tapLuminance);

                return max(max(directLightExact, directLightApprox), max(emissiveBoost, additionalLights));
            }

            float BenDayEyeDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);

                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.0001)
                        return 1e6;
                #else
                    if (rawDepth >= 0.9999)
                        return 1e6;
                #endif

                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            float BenDayScreenScale()
            {
                float2 referenceResolution = max(_ReferenceResolution.xy, float2(1.0, 1.0));
                float referenceMinDimension = min(referenceResolution.x, referenceResolution.y);
                float currentMinDimension = min(_ScreenParams.x, _ScreenParams.y);
                return currentMinDimension / referenceMinDimension;
            }

            // Rotated-grid wave shape: rows of a sine-wave line replace the original
            // circular dot. Same distance-field-and-softness approach as the dots, so
            // each row's stroke only covers a slice of its pitch band (never a solid
            // fill) and softens the same way at its edge.
            float BenDayWaveShape(float2 uv, float pitchPixels, float softnessPixels)
            {
                float angle = radians(_Angle_Degrees);
                float s = sin(angle);
                float c = cos(angle);

                float2 centeredPixels = (uv - 0.5) * _ScreenParams.xy;

                float2 rotated;
                rotated.x = centeredPixels.x * c - centeredPixels.y * s;
                rotated.y = centeredPixels.x * s + centeredPixels.y * c;

                float rowIndex = floor(rotated.y / pitchPixels + 0.5);
                float rowCenterY = rowIndex * pitchPixels;

                float amplitude = pitchPixels * saturate(_Wave_Amplitude) * 0.5;
                float frequency = max(_Wave_Frequency, 0.01);
                float waveY = amplitude * sin(rotated.x / pitchPixels * BenDayTwoPi * frequency);

                float distFromWave = abs(rotated.y - rowCenterY - waveY);
                float lineHalfWidth = pitchPixels * saturate(_Wave_Thickness) * 0.5;

                return 1.0 - smoothstep(lineHalfWidth, lineHalfWidth + softnessPixels, distFromWave);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                float3 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                float waveScale = max(_Wave_Scale, 1.0);
                float softness = max(_Softness, 0.0001);
                float screenScale = max(BenDayScreenScale(), 1e-5);
                float shadowBrightness = saturate(_Shadow_Brightness);

                float luminance = dot(sceneColor, float3(0.2126, 0.7152, 0.0722));

                // A pixel counts as "lit" for this effect if the sun, any point/spot
                // light, or its own emissive value reaches it - not the sun alone.
                float directLight = BenDayComputeDirectLight(uv);
                float eyeDepth = BenDayEyeDepth(uv);
                float shadowAmount = 1.0 - directLight;

                float referenceMinDimension = max(min(_ReferenceResolution.x, _ReferenceResolution.y), 1.0);
                float pitchAtReference = referenceMinDimension / waveScale;
                float pitchPixels = max(pitchAtReference * screenScale, 1.0);
                float softnessPixels = max(softness * pitchPixels, 0.0001);

                float baseWave = BenDayWaveShape(uv, pitchPixels, softnessPixels);

                float fadeRange = max(_Fade_End_Distance - _Fade_Start_Distance, 1e-5);
                float distanceFade = 1.0 - saturate((eyeDepth - _Fade_Start_Distance) / fadeRange);

                // The whole effect is driven by local contrast, not absolute
                // brightness: flat bright surfaces and flat dark surfaces both read as
                // zero here. Unlike fwidth() (a single adjacent-pixel derivative, so
                // only ever 1-2 pixels wide no matter how it's scaled), this samples
                // the lighting at four taps out to a real screen-space radius
                // (_Contrast_Border_Width) and uses the local max-min range, so the
                // reactive band around a shadow boundary is actually as wide as that
                // radius. The curve/sensitivity control how sharply the response
                // scales with how hard the transition is - a grazing shadow edge stays
                // faint, a hard-edged spotlight cutoff or flashlight sweep goes strong.
                float2 texel = 1.0 / _ScreenParams.xy;
                float borderPixels = max(_Contrast_Border_Width, 0.0);

                // Rotate and resize the sample kernel per pixel using a cheap hash, so
                // neighboring pixels probe slightly different directions/radii instead
                // of a perfectly uniform ring - reads as a fuzzy, irregular edge rather
                // than a clean geometric border.
                float2 pixelCoord = uv * _ScreenParams.xy;
                float hashAngle = BenDayHash(pixelCoord) * BenDayTwoPi;
                float hashRadius = BenDayHash(pixelCoord + 17.0);
                float jitteredBorder = borderPixels * lerp(0.3, 1.9, hashRadius);

                float sinA = sin(hashAngle);
                float cosA = cos(hashAngle);
                float2 dirA = float2(cosA, sinA);
                float2 dirB = float2(-sinA, cosA);

                float2 offsetA = dirA * texel * jitteredBorder;
                float2 offsetB = dirB * texel * jitteredBorder;

                float lightA1 = BenDayComputeDirectLight(uv + offsetA);
                float lightA2 = BenDayComputeDirectLight(uv - offsetA);
                float lightB1 = BenDayComputeDirectLight(uv + offsetB);
                float lightB2 = BenDayComputeDirectLight(uv - offsetB);

                float localMax = max(directLight, max(max(lightA1, lightA2), max(lightB1, lightB2)));
                float localMin = min(directLight, min(min(lightA1, lightA2), min(lightB1, lightB2)));
                float contrastRaw = localMax - localMin;
                float contrastNorm = saturate(contrastRaw * _Contrast_Sensitivity);

                // A small extra grain on the final intensity itself so the band's
                // strength breathes unevenly pixel-to-pixel, instead of a smooth
                // analytic falloff.
                float grain = lerp(0.6, 1.4, BenDayHash(pixelCoord + 91.0));

                // Flat lit/shadowed regions are no longer exactly zero - a very slight
                // base hum of visibility (_Base_Visibility) comes through everywhere,
                // itself grained per-pixel rather than a perfectly flat wash. From
                // there the response ramps continuously (no plateau, no snap) up to
                // full _Contrast_Strength as local contrast increases, so edges still
                // dominate but the whole thing reads as one smooth gradient rather
                // than "quiet base + a hard line".
                float baseNoise = lerp(0.4, 1.6, BenDayHash(pixelCoord + 133.0));
                float baseVisibility = saturate(_Base_Visibility) * baseNoise;
                float contrastIntensity = lerp(baseVisibility, saturate(_Contrast_Strength), pow(contrastNorm, max(_Contrast_Curve, 0.0001))) * distanceFade * grain;

                // Within that contrast band, split the response by which side of the
                // transition each pixel sits on: the darker side darkens further, the
                // lighter side brightens further, tracing a reactive rim around every
                // moving shadow boundary.
                float shadowWaves = baseWave * contrastIntensity * shadowAmount;
                float lightWaves = baseWave * contrastIntensity * directLight;

                float3 color = sceneColor;

                float visibilityCompensation = lerp(0.35, 0.85, 1.0 - luminance);
                float shadowDarkening = (1.0 - shadowBrightness) * visibilityCompensation;
                float3 shadowWaveColor = max(sceneColor - shadowDarkening.xxx, 0.0);
                color = lerp(color, shadowWaveColor, shadowWaves);

                float lightVisibilityCompensation = lerp(1.35, 0.8, luminance);
                float3 lightWaveColor = saturate(sceneColor * lightVisibilityCompensation);
                color += lightWaveColor * lightWaves;

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }
}
