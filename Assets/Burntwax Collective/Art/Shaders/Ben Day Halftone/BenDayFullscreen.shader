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

        _Shadow_Threshold("Shadow Threshold", Range(0.0, 1.0)) = 0.74
        _Shadow_Feather("Shadow Feather", Range(0.0, 1.0)) = 0.0
        _Shadow_Strength("Shadow Strength", Range(0.0, 1.0)) = 0.7
        _Shadow_Brightness("Shadow Wave Brightness", Range(0.0, 1.0)) = 0.85

        _Light_Threshold("Light Threshold", Range(0.0, 1.0)) = 0.2
        _Light_Feather("Light Feather", Range(0.0, 1.0)) = 0.43
        _Light_Strength("Light Wave Strength", Range(0.0, 1.0)) = 0.2

        _Edge_Sensitivity("Shadow Edge Sensitivity", Range(0.0, 20.0)) = 6.0
        _Edge_Strength("Shadow Edge Strength", Range(0.0, 1.0)) = 0.5

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
                float _Shadow_Threshold;
                float _Shadow_Feather;
                float _Shadow_Strength;
                float _Shadow_Brightness;
                float _Light_Threshold;
                float _Light_Feather;
                float _Light_Strength;
                float _Edge_Sensitivity;
                float _Edge_Strength;
                float _Fade_Start_Distance;
                float _Fade_End_Distance;
            CBUFFER_END

            static const float BenDayTwoPi = 6.283185307;

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
                float shadowFeather = max(_Shadow_Feather, 0.0001);
                float lightFeather = max(_Light_Feather, 0.0001);
                float shadowBrightness = saturate(_Shadow_Brightness);

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

                float luminance = dot(sceneColor, float3(0.2126, 0.7152, 0.0722));
                float emissiveBoost = smoothstep(0.65, 0.95, luminance);

                // A pixel counts as "lit" for this effect if the sun, any point/spot
                // light, or its own emissive value reaches it - not the sun alone.
                float directLight = max(max(directLightExact, directLightApprox), max(emissiveBoost, additionalLights));
                float eyeDepth = BenDayEyeDepth(uv);
                float shadowAmount = 1.0 - directLight;

                float referenceMinDimension = max(min(_ReferenceResolution.x, _ReferenceResolution.y), 1.0);
                float pitchAtReference = referenceMinDimension / waveScale;
                float pitchPixels = max(pitchAtReference * screenScale, 1.0);
                float softnessPixels = max(softness * pitchPixels, 0.0001);

                float baseWave = BenDayWaveShape(uv, pitchPixels, softnessPixels);

                float shadowMask = smoothstep(
                    _Shadow_Threshold,
                    _Shadow_Threshold + shadowFeather,
                    shadowAmount
                );

                float lightMask = smoothstep(
                    _Light_Threshold,
                    _Light_Threshold + lightFeather,
                    directLight
                );

                // Screen-space derivative of the lighting term spikes wherever light and
                // shadow are actively fighting - a flashlight sweeping a wall, a moving
                // shadow edge - so the waves flare there on top of the normal masking.
                float edgeFactor = saturate(fwidth(directLight) * _Edge_Sensitivity);

                float shadowWaves = baseWave * max(shadowMask * saturate(_Shadow_Strength), edgeFactor * _Edge_Strength);
                float lightWaves = baseWave * max(lightMask * saturate(_Light_Strength), edgeFactor * _Edge_Strength);

                float fadeRange = max(_Fade_End_Distance - _Fade_Start_Distance, 1e-5);
                float distanceFade = 1.0 - saturate((eyeDepth - _Fade_Start_Distance) / fadeRange);
                shadowWaves *= distanceFade;
                lightWaves *= distanceFade;

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
