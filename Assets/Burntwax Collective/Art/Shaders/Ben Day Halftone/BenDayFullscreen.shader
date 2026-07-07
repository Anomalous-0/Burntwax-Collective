Shader "Burntwax/Fullscreen/Ben Day Halftone"
{
    Properties
    {
        _Dot_Scale("Dot Scale", Float) = 128.2
        _Angle_Degrees("Angle Degrees", Float) = 16.5
        _Softness("Softness", Range(0.0, 1.0)) = 0.05
        _ReferenceResolution("Reference Resolution", Vector) = (1920, 1080, 0, 0)

        _Shadow_Threshold("Shadow Threshold", Range(0.0, 1.0)) = 0.74
        _Shadow_Feather("Shadow Feather", Range(0.0, 1.0)) = 0.0
        _Shadow_Strength("Shadow Strength", Range(0.0, 1.0)) = 0.7
        _Shadow_Dot_Brightness("Shadow Dot Brightness", Range(0.0, 1.0)) = 0.85

        _Light_Threshold("Light Threshold", Range(0.0, 1.0)) = 0.2
        _Light_Feather("Light Feather", Range(0.0, 1.0)) = 0.43
        _Light_Dot_Strength("Light Dot Strength", Range(0.0, 1.0)) = 0.2

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Dot_Scale;
                float _Angle_Degrees;
                float _Softness;
                float4 _ReferenceResolution;
                float _Shadow_Threshold;
                float _Shadow_Feather;
                float _Shadow_Strength;
                float _Shadow_Dot_Brightness;
                float _Light_Threshold;
                float _Light_Feather;
                float _Light_Dot_Strength;
                float _Fade_Start_Distance;
                float _Fade_End_Distance;
            CBUFFER_END

            float BenDayDirectLight(float2 uv)
            {
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
            }

            float BenDayDirectLightApprox(float2 uv)
            {
                float3 normalWS = SampleSceneNormals(uv);
                float normalLengthSq = dot(normalWS, normalWS);
                if (normalLengthSq <= 0.0001)
                    return 1.0;

                normalWS *= rsqrt(normalLengthSq);

                Light mainLight = GetMainLight();
                float3 mainLightDirWS = normalize(mainLight.direction);
                float ndotl = saturate(dot(normalWS, mainLightDirWS));

                return ndotl;
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

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                float3 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                float dotScale = max(_Dot_Scale, 1.0);
                float softness = max(_Softness, 0.0001);
                float screenScale = max(BenDayScreenScale(), 1e-5);
                float shadowFeather = max(_Shadow_Feather, 0.0001);
                float lightFeather = max(_Light_Feather, 0.0001);
                float shadowDotBrightness = saturate(_Shadow_Dot_Brightness);

                float luminance = dot(sceneColor, float3(0.2126, 0.7152, 0.0722));
                float directLightExact = BenDayDirectLight(uv);
                float directLightApprox = BenDayDirectLightApprox(uv);
                float emissiveBoost = smoothstep(0.65, 0.95, luminance);
                float directLight = max(max(directLightExact, directLightApprox), emissiveBoost);
                float eyeDepth = BenDayEyeDepth(uv);
                float shadowAmount = 1.0 - directLight;

                float angle = radians(_Angle_Degrees);
                float s = sin(angle);
                float c = cos(angle);

                float referenceMinDimension = max(min(_ReferenceResolution.x, _ReferenceResolution.y), 1.0);
                float pitchAtReference = referenceMinDimension / dotScale;
                float pitchPixels = max(pitchAtReference * screenScale, 1.0);
                float softnessPixels = max(softness * pitchPixels, 0.0001);

                float2 centeredPixels = (uv - 0.5) * _ScreenParams.xy;

                float2 rotated;
                rotated.x = centeredPixels.x * c - centeredPixels.y * s;
                rotated.y = centeredPixels.x * s + centeredPixels.y * c;

                float2 grid = rotated / pitchPixels;
                float2 cell = frac(grid) - 0.5;
                float dist = length(cell);
                float distPixels = dist * pitchPixels;
                float radiusPixels = pitchPixels * 0.25;
                float baseDot = 1.0 - smoothstep(radiusPixels, radiusPixels + softnessPixels, distPixels);

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

                float shadowDots = baseDot * shadowMask * saturate(_Shadow_Strength);
                float lightDots = baseDot * lightMask * saturate(_Light_Dot_Strength);

                float fadeRange = max(_Fade_End_Distance - _Fade_Start_Distance, 1e-5);
                float distanceFade = 1.0 - saturate((eyeDepth - _Fade_Start_Distance) / fadeRange);
                shadowDots *= distanceFade;
                lightDots *= distanceFade;

                float3 color = sceneColor;

                float visibilityCompensation = lerp(0.35, 0.85, 1.0 - luminance);
                float shadowDarkening = (1.0 - shadowDotBrightness) * visibilityCompensation;
                float3 shadowDotColor = max(sceneColor - shadowDarkening.xxx, 0.0);
                color = lerp(color, shadowDotColor, shadowDots);

                float lightVisibilityCompensation = lerp(1.35, 0.8, luminance);
                float3 lightDotColor = saturate(sceneColor * lightVisibilityCompensation);
                color += lightDotColor * lightDots;

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }
}
