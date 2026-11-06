Shader "LastTide/Ocean"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.16, 0.82, 0.76, 0.55)
        _DeepColor ("Deep Color", Color) = (0.01, 0.16, 0.36, 1)
        _DepthRange ("Depth Range (m)", Float) = 9
        _FoamColor ("Foam Color", Color) = (0.95, 0.98, 0.97, 1)
        _FoamWidth ("Foam Width (m)", Float) = 0.2
        _RippleScale ("Ripple Scale", Float) = 0.55
        _RippleSpeed ("Ripple Speed", Float) = 0.7
        _RippleStrength ("Ripple Strength", Float) = 0.05
        _SkyColor ("Sky Reflection Color", Color) = (0.55, 0.75, 0.95, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "Ocean"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float _DepthRange;
                half4 _FoamColor;
                float _FoamWidth;
                float _RippleScale;
                float _RippleSpeed;
                float _RippleStrength;
                half4 _SkyColor;
            CBUFFER_END

            float _LastTideNight;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float2 RippleSlope(float2 position, float time)
            {
                float2 slope = 0;
                position += 1.8 * sin(position.yx * float2(0.23, 0.31) + float2(time * 0.13, -time * 0.17));
                slope += float2(0.83, 0.55) * cos(dot(position, float2(0.83, 0.55)) * 1.07 + time * 1.1);
                slope += float2(-0.47, 0.88) * cos(dot(position, float2(-0.47, 0.88)) * 1.73 + time * 1.4) * 0.7;
                slope += float2(0.31, -0.95) * cos(dot(position, float2(0.31, -0.95)) * 3.13 + time * 1.9) * 0.45;
                slope += float2(-0.93, -0.37) * cos(dot(position, float2(-0.93, -0.37)) * 5.37 + time * 2.6) * 0.3;
                return slope;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float waterDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depth = max(0.0, sceneDepth - waterDepth);

                float time = _Time.y * _RippleSpeed;
                float rippleFade = 1.0 - smoothstep(25.0, 140.0, distance(_WorldSpaceCameraPos, input.positionWS));
                float2 slope = RippleSlope(input.positionWS.xz * _RippleScale, time) * _RippleStrength * rippleFade;
                float3 normalWS = normalize(float3(-slope.x, 1.0, -slope.y));
                float3 viewWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                float depthBlend = saturate(depth / _DepthRange);
                depthBlend = 1.0 - (1.0 - depthBlend) * (1.0 - depthBlend);
                half4 water = lerp(_ShallowColor, _DeepColor, depthBlend);

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewWS)), 4.0);
                water.rgb = lerp(water.rgb, _SkyColor.rgb, fresnel * 0.6);
                water.a = saturate(water.a + fresnel * 0.4);

                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + viewWS);
                float specular = pow(saturate(dot(normalWS, halfDir)), 90.0);
                specular *= 0.15 + 0.85 * rippleFade;
                water.rgb += mainLight.color * specular * 0.45;
                water.a = saturate(water.a + specular);

                float wobble = sin(input.positionWS.x * 0.9 + time) * sin(input.positionWS.z * 0.8 - time * 0.8);
                float foamEdge = _FoamWidth * (0.8 + 0.2 * sin(time * 1.3) + 0.5 * wobble);
                float foam = 1.0 - smoothstep(foamEdge * 0.5, foamEdge, depth);
                water.rgb = lerp(water.rgb, _FoamColor.rgb, foam);
                water.rgb *= lerp(1.0, 0.09, _LastTideNight);
                water.a = lerp(water.a, _FoamColor.a, foam);

                return water;
            }
            ENDHLSL
        }
    }
}
