Shader "Custom/Obsidian"
{
    Properties
    {
        _Color("Base Color", Color) = (0, 0, 0, 1)
        _HeightScale("Height Scale", Range(0, 0.1)) = 0.1
        _Smoothness("Smoothness", Range(0, 1)) = 0.8
        _Metallic("Metallic", Range(0, 1)) = 0.9
        _MainTex("Texture", 2D) = "white" {}

        [NoScaleOffset] _NormalMapTex("Normal Map Texture", 2D) = "bump" {}
        _DispTexScale("Displacement Texture Scale", Range(1, 100)) = 10
        [Toggle] _ShowNormalMap("Show Normal Map", Float) = 0

        _AmbientIntensity("Ambient Intensity", Range(0, 1)) = 0.4
        _EmissiveIntensity("Emissive Intensity", Range(0, 1)) = 0.1
        [Toggle] _UseShadows("Use Shadows", Float) = 1

    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 worldTangent : TEXCOORD3;
                float3 worldBinormal : TEXCOORD4;

            };

            float _HeightScale;
            float _Smoothness;
            float _Metallic;
            fixed4 _Color;
            sampler2D _MainTex;
            sampler2D _NormalMapTex;
            float _DispMapBlend;
            float _DispTexScale;
            float _ShowNormalMap;
            float _AmbientIntensity;
            float _EmissiveIntensity;
            float _UseShadows;


            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 worldTangent = normalize(mul((float3x3)unity_ObjectToWorld, v.tangent.xyz));
                float3 worldBinormal = normalize(cross(worldNormal, worldTangent) * v.tangent.w);

                o.worldNormal = worldNormal;
                o.worldTangent = worldTangent;
                o.worldBinormal = worldBinormal;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Check if we should display normal map visualization
                if (_ShowNormalMap > 0.5) {
                    // Sample normal map at scaled UV coordinates
                    float2 scaledUV = i.uv * _DispTexScale;
                    float3 normalMap = UnpackNormal(tex2D(_NormalMapTex, scaledUV));
                    // Convert normal from [ - 1, 1] range to [0, 1] for display
                    return half4(normalMap * 0.5 + 0.5, 1.0);
                }

                // Get mesh normal (the actual surface normal)
                float3 meshNormal = normalize(i.worldNormal);

                //normal from ChunkMeshGenerator
                float2 scaledUV = i.uv * _DispTexScale;
                float3 texNormal = UnpackNormal(tex2D(_NormalMapTex, scaledUV));

                float3 worldNormal = normalize(
                texNormal.x * i.worldTangent +
                texNormal.y * i.worldBinormal +
                texNormal.z * i.worldNormal
                );

                float3 lightDir;
                if (_WorldSpaceLightPos0.w == 0.0) {
                    // Directional light
                    lightDir = normalize(_WorldSpaceLightPos0.xyz);
                } else {
                    // Point light
                    lightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                }

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfDir = normalize(lightDir + viewDir);

                // Sample and blend texture with base color
                half4 textureColor = tex2D(_MainTex, i.uv);
                half3 baseColor = lerp(_Color.rgb, textureColor.rgb, textureColor.a);

                // Diffuse term (Lambert)
                float NdotL = max(0, dot(worldNormal, lightDir));

                //shadows
                float shadowTerm = _UseShadows > 0.5 ? max(0, NdotL) : 1.0;

                half3 diffuse = shadowTerm * baseColor;

                // Specular term (Blinn - Phong)
                //half vector between light and view directions instead of the reflection vector
                float NdotH = max(0, dot(worldNormal, halfDir));
                //shininess / smoothness controlled by specPower
                float specPower = _Smoothness * 8.0;
                //sharpness of highlight (first part) multiplied by second part for flexibility
                half3 specular = (NdotL > 0) ? pow(NdotH, specPower) * _Color.rgb * _Metallic * 10.0 : 0.0;

                specular = 2 * pow(NdotH, specPower) * _Color.rgb * _Metallic;

                // Ambient lighting
                half3 ambient = _AmbientIntensity * baseColor;

                //subtle emissive component for more brightness
                half3 emissive = baseColor * (_EmissiveIntensity * 1.2);

                // Final color
                half3 finalColor = ambient + diffuse + specular + emissive * 2;

                return half4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}