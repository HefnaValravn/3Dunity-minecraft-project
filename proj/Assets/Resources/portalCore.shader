Shader "Custom/PortalCoreShader" {
    Properties {
        _MainColor ("Main Color", Color) = (0.5, 0.1, 0.8, 0.8)
        _SecondaryColor ("Secondary Color", Color) = (0.7, 0.3, 1, 0.6)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _Speed ("Animation Speed", Range(0, 10)) = 2
    }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade vertex:vert

        float4 _MainColor;
        float4 _SecondaryColor;
        float _Intensity;
        float _Speed;

        struct Input {
            float2 uv_MainTex;
            float3 vertexColor;
        };

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Simple vertex animation
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float t = _Time.y * _Speed;
            v.vertex.xyz += sin(worldPos + t) * 0.02;
            
            o.vertexColor = v.color;
        }

        void surf (Input IN, inout SurfaceOutput o) {
            // Animated color mixing
            float t = _Time.y * _Speed;
            float3 animColor = lerp(_MainColor.rgb, _SecondaryColor.rgb, sin(t) * 0.5 + 0.5);
            
            o.Albedo = animColor;
            o.Alpha = _MainColor.a * (sin(t) * 0.2 + 0.8);
            o.Emission = animColor * _Intensity * (sin(t) * 0.5 + 0.5);
        }
        ENDCG
    }
    FallBack "Diffuse"
}