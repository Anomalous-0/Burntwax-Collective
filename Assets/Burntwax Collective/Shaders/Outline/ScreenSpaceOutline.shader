Shader "Burntwax/Fullscreen/Screen Space Outline"
{
    Properties
    {
        _Color("Outline Color", Color) = (0, 0, 0, 1)
        _Thickness("Thickness (Pixels)", Range(0.5, 4.0)) = 1.25
        _DepthThreshold("Depth Threshold", Range(0.0001, 0.02)) = 0.002
        _DepthScale("Depth Scale", Range(0.25, 4.0)) = 1.0
        _NormalThreshold("Normal Threshold", Range(0.01, 1.0)) = 0.2
        _NormalStrength("Normal Strength", Range(0.5, 4.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Screen Space Outline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Thickness;
                float _DepthThreshold;
                float _DepthScale;
                float _NormalThreshold;
                float _NormalStrength;
            CBUFFER_END

            float SampleEyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            float DepthEdge(float2 uv, float2 texelSize)
            {
                float centerDepth = SampleEyeDepth(uv);
                float leftDepth = SampleEyeDepth(uv - float2(texelSize.x, 0.0));
                float rightDepth = SampleEyeDepth(uv + float2(texelSize.x, 0.0));
                float upDepth = SampleEyeDepth(uv + float2(0.0, texelSize.y));
                float downDepth = SampleEyeDepth(uv - float2(0.0, texelSize.y));

                float maxDepthDelta = max(
                    max(abs(centerDepth - leftDepth), abs(centerDepth - rightDepth)),
                    max(abs(centerDepth - upDepth), abs(centerDepth - downDepth)));

                // Scale the threshold by eye depth so outlines stay visually consistent at distance.
                float scaledThreshold = max(1e-5, _DepthThreshold * max(centerDepth, 1.0) * _DepthScale);
                return saturate((maxDepthDelta - scaledThreshold) / scaledThreshold);
            }

            float NormalEdge(float2 uv, float2 texelSize)
            {
                float3 centerNormal = normalize(SampleSceneNormals(uv));
                float3 leftNormal = normalize(SampleSceneNormals(uv - float2(texelSize.x, 0.0)));
                float3 rightNormal = normalize(SampleSceneNormals(uv + float2(texelSize.x, 0.0)));
                float3 upNormal = normalize(SampleSceneNormals(uv + float2(0.0, texelSize.y)));
                float3 downNormal = normalize(SampleSceneNormals(uv - float2(0.0, texelSize.y)));

                float maxNormalDelta = max(
                    max(1.0 - dot(centerNormal, leftNormal), 1.0 - dot(centerNormal, rightNormal)),
                    max(1.0 - dot(centerNormal, upNormal), 1.0 - dot(centerNormal, downNormal)));

                float threshold = max(1e-5, _NormalThreshold);
                return saturate(((maxNormalDelta - threshold) / threshold) * _NormalStrength);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                float2 texelSize = (_BlitTexture_TexelSize.xy * _Thickness);

                float depthEdge = DepthEdge(uv, texelSize);
                float normalEdge = NormalEdge(uv, texelSize);
                float edge = saturate(max(depthEdge, normalEdge));

                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                return lerp(sceneColor, _Color, edge);
            }
            ENDHLSL
        }
    }
}
