Shader "Custom/litObsidian"
{
    Properties
    {
        _Color("Base Color", Color) = (0, 0, 0, 1)
        _HeightScale("Height Scale", Range(0, 0.1)) = 0.05
        _Smoothness("Smoothness", Range(0, 1)) = 0.8
        _Metallic("Metallic", Range(0, 1)) = 0.9
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"  // Standard Unity CG includes for lighting

            // Input structure
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;  // World position for lighting
            };

            // Shader properties
            float _HeightScale;
            float _Smoothness;
            float _Metallic;
            fixed4 _Color;
            sampler2D _MainTex;

            // Function to generate procedural heightmap
            float heightmap(float2 uv)
            {
                return sin(uv.x * 30) * cos(uv.y * 30) * _HeightScale;  // Higher frequency for more variation
            }

            // Vertex Shader
            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;  // Pass world position for lighting calculation
                return o;
            }

            // Fragment Shader
            half4 frag(v2f i) : SV_Target
            {
                // Compute heightmap and normals
                float h = heightmap(i.uv);
                float dx = heightmap(i.uv + float2(0.01, 0)) - h;
                float dy = heightmap(i.uv + float2(0, 0.01)) - h;
                float3 normal = normalize(float3(-dx, -dy, 1.0));  // Ensure normal is facing outwards

                // Sample the texture
                half4 textureColor = tex2D(_MainTex, i.uv);
                half3 color = lerp(_Color.rgb, textureColor.rgb, textureColor.a);

                // Lighting calculation using Unity's lighting system
                half3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, normal));  // Ensure normal is transformed to world space
                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);  // Direction of light

                // Calculate the light intensity using the dot product between the normal and the light direction
                half lightIntensity = max(0.3, dot(worldNormal, lightDir));  // Dot product gives us how much light hits the surface

                // Always ensure a small amount of ambient lighting (not fully dark)
                half3 ambientLight = 0.5 * _Color.rgb; // Slight ambient lighting

                // Apply the light intensity to the color, but ensure it doesn't go fully dark
                half3 finalColor = (color * lightIntensity) + ambientLight;

                // Final color
                return half4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}