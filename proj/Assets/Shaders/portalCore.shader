Shader "Custom/portalCore" {
    Properties {
        _MainColor ("Main Color", Color) = (0.5, 0.1, 0.8, 0.8)
        _SecondaryColor ("Secondary Color", Color) = (0.7, 0.3, 1, 0.6)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _Speed ("Animation Speed", Range(0, 10)) = 2
    }
    SubShader {
        Tags {
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
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
                // Sample the video texture
                half4 videoColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Time-based animation
                float t = _Time.y * _Speed;
                float animValue = sin(t) * 0.5 + 0.5;
                
                // Animated color mixing between main and secondary color
                float3 animColor = lerp(_MainColor.rgb, _SecondaryColor.rgb, animValue);
                
                // Combine video texture with animated color
                float3 finalColor = videoColor.rgb * animColor * _Intensity;
                
                // Clamp alpha to prevent it from becoming too transparent
                float finalAlpha = clamp(videoColor.a * _MainColor.a * (sin(t) * 0.075 + 0.9), 0.9, 1.0);

                
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
