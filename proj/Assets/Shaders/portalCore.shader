Shader "Custom/portalCore" {
    Properties {
        _MainColor ("Main Color", Color) = (0.5, 0.1, 0.8, 0.8)
        _SecondaryColor ("Secondary Color", Color) = (0.7, 0.3, 1, 0.6)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _Speed ("Animation Speed", Range(0, 10)) = 2
        _Transparency ("Transparency", Range(0, 1)) = 0.7
        _WaveStrength ("Wave Distortion", Range(0, 0.1)) = 0.05
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 4
    }
    SubShader {
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForward"}

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _MainColor;
            float4 _SecondaryColor;
            float _Intensity;
            float _Speed;
            float _Transparency;
            float _WaveStrength;
            float _WaveSpeed;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct V2F {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            V2F vert(Attributes input) {
                V2F output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(V2F input) : SV_Target {
                float2 uv = input.uv;
                float2 center = float2(0.5, 0.5);
                float2 uvFromCenter = uv - center;
                float dist = length(uvFromCenter);

                // Time values for different wave effects
                float t1 = _Time.y * _WaveSpeed;
                float t2 = _Time.y * _WaveSpeed * 0.8;
                float t3 = _Time.y * _WaveSpeed * 1.2;

                // Create concentric ripples moving outward
                float wave1 = sin(dist * 20.0 - t1 * 2.0) * 0.5 + 0.5;
                float wave2 = sin(dist * 15.0 - t2 * 1.5) * 0.5 + 0.5;
                float wave3 = sin((uv.x + uv.y) * 12.0 - t3) * 0.5 + 0.5;

                // Combine waves and fade effect toward edges
                float combinedWave = (wave1 + wave2 + wave3) * 0.5;
                combinedWave *= smoothstep(0.8, 0.2, dist * 1.2); // Stronger in center

                // Apply wave distortion to UVs
                float2 direction = normalize(uvFromCenter + 0.00001); // Avoid division by zero
                float2 distortion = direction * combinedWave * _WaveStrength;
                float2 distortedUV = uv + distortion;

                // Sample the video texture
                half4 videoColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                // Time - based animation
                float t = _Time.y * _Speed;
                float animValue = sin(t) * 0.5 + 0.5;

                // Animated color mixing between main and secondary color
                float3 animColor = lerp(_MainColor.rgb, _SecondaryColor.rgb, animValue);

                // Combine video texture with animated color
                float3 finalColor = videoColor.rgb * animColor * _Intensity;

                // Calculate alpha - oscillate between opaque (1.0) and your transparency value
                // This makes it pulse between completely opaque and your set transparency level
                float minAlpha = _Transparency; // The most transparent it will get
                float maxAlpha = 0.95; // Completely opaque
                float alphaOscillation = sin(t * 0.7) * 0.5 + 0.5; // Oscillation between 0 - 1
                float finalAlpha = lerp(minAlpha, maxAlpha, alphaOscillation);


                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
